using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;

[assembly: AssemblyTitle("Feed Center")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyCompany("Chris Kaczor")]
[assembly: AssemblyProduct("Feed Center")]
[assembly: AssemblyCopyright("Copyright © Chris Kaczor 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: TargetPlatform("Windows10.0")]
[assembly: SupportedOSPlatform("Windows10.0")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug build")]
#else
[assembly: AssemblyConfiguration("Release build")]
#endif

[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]