<p align="center">
  <a href="https://www.nuget.org/packages/DllMapCore">
    <img src="https://buildstats.info/nuget/DllMapCore">
  </a>
  <a href="https://travis-ci.org/jpbruyere/DllMapCore">
    <img src="https://travis-ci.org/jpbruyere/DllMapCore.svg?branch=master">
  </a>
  <a href="https://ci.appveyor.com/project/jpbruyere/DllMapCore">
    <img src="https://ci.appveyor.com/api/projects/status/fdwb4e3ru7y8v3sp/branch/master?svg=true">
  </a>  
  <img src="https://img.shields.io/github/license/jpbruyere/DllMapCore.svg?style=flat-square">
  <a href="https://www.paypal.me/GrandTetraSoftware">
    <img src="https://img.shields.io/badge/Donate-PayPal-blue.svg?style=flat-square">
  </a>
</p>

Enable [mono dllmap](https://www.mono-project.com/docs/advanced/pinvoke/dllmap/) on net core with its new [native library resolver](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.setdllimportresolver?view=netcore-3.1)

Call the `Enable` static method in your main method.
```csharp
void Main() {
	DllMapCore.Resolve.Enable ();
```

This will trigger on each assebly load events in current domain a search for a corresponding `.config` file in the same directory.
This config will be automatically created from your `App.config`, if not, add this entry to your project properties:
```xml
<Project Sdk="Microsoft.NET.Sdk">
	...	
  <PropertyGroup>
  	...
		<AppConfig>App.config</AppConfig>	
```
In your project `App.config`, you may now be able to add `dllmap` entries to resolve native dll's.

```xml
<configuration>
    <dllmap dll="glfw3" target="libglfw.so" os="!windows"/>
    <dllmap dll="glfw3.dll" target="libglfw.so" os="!windows"/>
    <dllmap dll="rsvg-2.40" target="librsvg-2.so" os="!windows"/>
</configuration>
```

Where `dll` is the original string of the pinvoke, `target` is the replacement string, and `os` may be one of the following: (`linux`, `unix`, `windows`, `osx`);
