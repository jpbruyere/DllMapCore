<p align="center">
  <a href="https://www.nuget.org/packages/DllMapCore">
    <img src="https://buildstats.info/nuget/DllMapCore">
  </a>
  <!--<a href="https://travis-ci.org/jpbruyere/DllMapCore">
    <img src="https://travis-ci.org/jpbruyere/DllMapCore.svg?branch=master">
  </a>
  <a href="https://ci.appveyor.com/project/jpbruyere/DllMapCore">
    <img src="https://ci.appveyor.com/api/projects/status/fdwb4e3ru7y8v3sp/branch/master?svg=true">
  </a>-->  
  <img src="https://img.shields.io/github/license/jpbruyere/DllMapCore.svg?style=flat-square">
  <a href="https://www.paypal.me/GrandTetraSoftware">
    <img src="https://img.shields.io/badge/Donate-PayPal-blue.svg?style=flat-square">
  </a>
</p>

Enable [Mono dllmap](https://www.mono-project.com/docs/advanced/pinvoke/dllmap/) in NETCore applications. 

Once enabled, **.config** files will be scanned to [register](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.setdllimportresolver?view=netcore-3.1) alternative pathes for native dll's resolution.

## Quick start
#### App.config

Add an `App.config` file to your project or use the existing one:
```xml
<configuration>
  ...  
  <dllmap dll="cairo" target="libcairo.so.2" os="!windows"/>
  <dllmap dll="glfw3" target="libglfw.so" os="linux,unix"/>
  <dllmap dll="rsvg-2" target="rsvg-2.40.dll" os="windows"/>
</configuration>
```
Each ```<dllmap>``` entries must have the following attributes:

- **dll**: the original dll path string of the pinvoke.
- **target**: the alternative path for the dll
- **os**: A colon separated list of the platforms on which to use this alternative path. It may be one of the following: `'linux'`, `'unix'`, `'windows'`, `'osx'`, and may be prefixed with `!` as a negation.

The `App.config` will be used to generate a `yourassemblyname.exe.config` file in the output directory. If not, add this to your `csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AppConfig>App.config</AppConfig>	
```

#### DllMapCore activation

Call the `Enable` static method before any call to methods requiring a native library path resolution.

```csharp
static void Main() {
  DllMapCore.Resolve.Enable ();
  ...
```

Once enabled, DllMapCore will search for each loaded assemblies a corresponding `.config` file next to it containing `<dllmap>` entries.

If you pass ```true``` to the Enable method, all the `.config` files will be merged and used for each loaded assemblies. This allow you to have a single App.config for you main program that could resolve every native library access, even from nuget packages.
