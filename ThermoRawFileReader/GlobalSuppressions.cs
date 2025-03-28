// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.FilterTextUtilities.ExtractMRMMasses(System.String,ThermoRawFileReader.MRMScanTypeConstants,ThermoRawFileReader.MRMInfo@)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.CloseRawFile")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.ExtractMRMMasses(System.String,ThermoRawFileReader.MRMScanTypeConstants,ThermoRawFileReader.MRMInfo@)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.MakeGenericThermoScanFilter(System.String,System.Boolean,System.Boolean)~System.String")]
[assembly: SuppressMessage("Performance", "CA1853:Unnecessary call to 'Dictionary.ContainsKey(key)'", Justification = "Leave as-is for clarity and to allow for adding a breakpoint", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.CacheScanInfo(System.Int32,ThermoRawFileReader.clsScanInfo)")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses are correct", Scope = "member", Target = "~M:ThermoRawFileReader.FilterTextUtilities.ExtractMRMMasses(System.String,ThermoRawFileReader.MRMScanTypeConstants,ThermoRawFileReader.MRMInfo@)")]
[assembly: SuppressMessage("Roslynator", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.GetScanInfo(System.Int32,ThermoRawFileReader.clsScanInfo@,System.Boolean,System.Boolean)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Leave as-is for readability", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.GetChromatogramData2D(ThermoFisher.CommonCore.Data.Business.Device,System.Int32,System.Int32,System.Int32)~System.Collections.Generic.Dictionary{System.Int32,System.Collections.Generic.List{System.Double}}")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allow for backwards compatibility", Scope = "type", Target = "~T:ThermoRawFileReader.clsScanInfo")]
[assembly: SuppressMessage("Usage", "RCS1246:Use element access.", Justification = "Use Last for readability", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.GetTuneData")]
