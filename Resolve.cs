// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace DllMapCore {
	/// <summary>
	/// DllMapCore Resolver.
	/// </summary>
	public static class Resolve {	
		static Dictionary<string, Dictionary<string, List<string>>> resolves = new Dictionary<string, Dictionary<string, List<string>>> ();

		/// <summary>
		/// Ename dllmap entries parsing in App.config files on assembly load in current domain. Call this method
		/// on the first statement of your main entry point.
		/// </summary>
		/// <remarks>
		/// On assembly load, search for a corresponding '.config' xml file and parse dllmap entries.
		/// use the new net core DllImportResolver to resolve imports.
		/// Ensure no resolve sensitive type is used in the Main method, or there will trigger
		/// an attempt of resolve before DllMapCore had time to register its callbacks.
		/// </remarks>
		public static void Enable () {
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyLoad += assemblyLoadHandler;
		}

		static void assemblyLoadHandler (object sender, AssemblyLoadEventArgs args) {
			string f = args.LoadedAssembly.Location;
			string config = args.LoadedAssembly.Location + ".config";

			if (!File.Exists (config))
				return;

			Dictionary<string, List<string>> maps = new Dictionary<string, List<string>> ();

			using (XmlReader xml = XmlReader.Create (config)) {
				if (!xml.ReadToFollowing ("configuration"))
					return;
				if (!xml.ReadToDescendant ("dllmap"))
					return;
				do {
					if (processMapEntry (xml, out string dll, out string target)) {
						if (maps.ContainsKey (dll))
							maps[dll].Add (target);
						else
							maps.Add (dll, new List<string> () { target });
					}
				} while (xml.ReadToNextSibling ("dllmap"));
			}

			if (maps.Count == 0)
				return;

			resolves[args.LoadedAssembly.FullName] = maps;

			NativeLibrary.SetDllImportResolver (args.LoadedAssembly,(libraryName, assembly, searchPath) => {
				if (resolves.TryGetValue(assembly.FullName, out Dictionary<string, List<string>> ms)) {
					if (ms.ContainsKey (libraryName)) {
						foreach (string map in ms[libraryName]) {
							try {
								return NativeLibrary.Load (map, assembly, searchPath);
							} catch { }
						}
					}
				}
				return NativeLibrary.Load (libraryName, assembly, searchPath);
			});			
		}


		/// <summary>
		/// linux, osx, solaris, freebsd, openbsd, netbsd, windows, aix, hpux.
		/// </summary>
		static bool osMatch (string os) {
			bool negative = false;
			if (string.IsNullOrEmpty (os))
				return true;
			if (os.StartsWith ('!')) {
				negative = true;
				os = os.Substring (1);
			}
			switch (Environment.OSVersion.Platform) {
			case PlatformID.Win32S:
			case PlatformID.Win32Windows:
			case PlatformID.Win32NT:
			case PlatformID.WinCE:
				return negative ^ string.Equals (os, "windows", StringComparison.OrdinalIgnoreCase);
			case PlatformID.Unix:
				return negative ^ (
						string.Equals (os, "unix", StringComparison.OrdinalIgnoreCase) |
						string.Equals (os, "linux", StringComparison.OrdinalIgnoreCase)
					);
			case PlatformID.MacOSX:
				return negative ^ string.Equals (os, "osx", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		static bool processMapEntry (XmlReader xml, out string dll, out String target) {
			dll = xml.GetAttribute ("dll");
			target = xml.GetAttribute ("target");
			if (string.IsNullOrEmpty (xml.GetAttribute ("os")))
				return false;
			string[] oss = xml.GetAttribute ("os").Split (',', StringSplitOptions.RemoveEmptyEntries);

			foreach (string os in oss) {
				if (osMatch (os.Trim ()))
					return true;
			}
			return false;
		}
	}
}
