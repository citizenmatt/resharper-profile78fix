using System.Reflection;
using JetBrains.ActionManagement;
using JetBrains.Application.PluginSupport;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("resharper-profile78")]
[assembly: AssemblyDescription("Tactical fix to stop ReSharper complaining about projects that reference a profile78 PCL assembly from requiring System.Runtime.dll to be referenced")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Matt Ellis")]
[assembly: AssemblyProduct("resharper-profile78")]
[assembly: AssemblyCopyright("Copyright © Matt Ellis, 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: ActionsXml("resharper-profile78.Actions.xml")]

// The following information is displayed by ReSharper in the Plugins dialog
[assembly: PluginTitle("Profile78 tactical fix")]
[assembly: PluginDescription("Tactical fix to stop ReSharper complaining about projects that reference a profile78 PCL assembly from requiring System.Runtime.dll to be referenced")]
[assembly: PluginVendor("Matt Ellis")]
