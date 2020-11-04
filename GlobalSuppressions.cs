﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.CloseRawFile")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.GetScanTypeNameFromThermoScanFilterText(System.String)~System.String")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.ExtractMRMMasses(System.String,ThermoRawFileReader.MRMScanTypeConstants,ThermoRawFileReader.MRMInfo@)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors here", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.MakeGenericThermoScanFilter(System.String)~System.String")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses are correct", Scope = "member", Target = "~M:ThermoRawFileReader.XRawFileIO.ExtractMRMMasses(System.String,ThermoRawFileReader.MRMScanTypeConstants,ThermoRawFileReader.MRMInfo@)")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allow for backwards compatibility", Scope = "type", Target = "~T:ThermoRawFileReader.clsScanInfo")]
