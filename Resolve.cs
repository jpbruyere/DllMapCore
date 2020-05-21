// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

namespace DllMapCore {
	/// <summary>
	/// DllMapCore Resolver.
	/// </summary>
	public static class Resolve {
		static Dictionary<string, Dictionary<string, List<string>>> resolves = new Dictionary<string, Dictionary<string, List<string>>> ();
		static bool global = false;
		/// <summary>
		/// Enable parsing of `.config` files of loaded assemblies in the search of `dllmap` entries for native dll resolution.
		/// </summary>
		/// <remarks>
		/// use the new net core DllImportResolver to resolve imports.
		/// </remarks>
		/// <param name="global">If set to <c>true</c>, all found `.config` files are combined and used for all
		/// dll resolutions in all assemblies</param>
		public static void Enable (bool global = false) {
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyLoad += assemblyLoadHandler;
			Resolve.global = global;
			resolves.Add ("global", new Dictionary<string, List<string>> ());

			//process already loaded assemblies
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) 
				processAssembly (assembly);			
		}
		//process each newly loaded assembly in the current appdomain.
		static void assemblyLoadHandler (object sender, AssemblyLoadEventArgs args) =>
			processAssembly (args.LoadedAssembly);
		//- search for a matching .config file in the same directory as the assembly
		//- parse `dllmap` elements child of the `configuration` tag.
		static void processAssembly (Assembly loadedAssembly) {
			string f = loadedAssembly.Location;
			string config = loadedAssembly.Location + ".config";

			if (File.Exists (config)) {
				Dictionary<string, List<string>> maps = global ?
					resolves["global"] : new Dictionary<string, List<string>> ();

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

				if (!global) {
					if (maps.Count > 0)
						resolves.Add (loadedAssembly.FullName, maps);
					NativeLibrary.SetDllImportResolver (loadedAssembly, (libraryName, assembly, searchPath) => {
						if (resolves.TryGetValue (assembly.FullName, out Dictionary<string, List<string>> ms)) {
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
					return;
				}
			}
			NativeLibrary.SetDllImportResolver (loadedAssembly, (libraryName, assembly, searchPath) => {
				if (resolves["global"].ContainsKey (libraryName)) {
					foreach (string map in resolves["global"][libraryName]) {
						try {
							return NativeLibrary.Load (map, assembly, searchPath);
						} catch { }
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
