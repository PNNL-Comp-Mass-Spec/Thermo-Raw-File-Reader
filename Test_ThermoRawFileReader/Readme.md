# Overview

The Thermo Raw File Reader is a .NET DLL that can be used to 
extract data from Thermo .Raw files.  Prior to October 2018, 
in order to use ThermoRawFileReader.dll you needed to either 
download and install MSFileReader, or use batch file registerFiles.bat 
in the lib directory to register MSFileReader.XRawfile2.dll

Newer versions of ThermoRawFileReader.dll use NuGet package
ThermoFisher.CommonCore.RawFileReader

## Details

The Thermo Raw File Reader DLL provides several methods for parsing the information 
returned by MSFileReader or ThermoFisher.CommonCore.RawFileReader, including:
* Determining the parent ion m/z and fragmentation mode in a given scan filter
* Determining the Ionization mode from a given scan filter
* Extracting MRM masses listed in a given scan filter
* Reporting the number of spectra in the .Raw file
* Returning details on a specific spectrum
* Obtaining the raw m/z and intensity values for a given spectrum

The Test_ThermoRawFileReader directory contains a .NET command-line application 
that illustrates how to interface with ThermoRawFileReader.dll

## Console Switches

Test_ThermoRawFileReader is a command line application.  Syntax:

```
Test_ThermoRawFileReader.exe
  InputFilePath.raw [/GetFilters] [/Centroid] [/Sum] [/Start:Scan] [/End:Scan]
  [/ScanInfo:IntervalScans] [/NoScanData] [/NoScanEvents] [/NoCE] [/MSLevelOnly]
  [/Trace]
```

Running this program without any parameters it will process file
`..\..\..\UnitTests\Docs\Angiotensin_AllScans.raw`

The first parameter specifies the file to read

Use /GetFilters to compile and display a list of scan filters in any MASIC
_ScanStatsEx.txt files in the working directory

Without /GetFilters, data is read from the file, either from all scans, or a scan range

Use /Start and /End to limit the scan range to process\

If /Start and /End are not provided, will read every 21 scans

Use /Centroid to centroid the data when reading

Use /Sum to test summing the data across 15 scans (each spectrum will 
be shown twice; once with summing and once without)

While reading data, the scan number and elution time is displayed for each scan.
To show this info every 5 scans, use /ScanInfo:5

Use /NoScanData to skip loading any scan data

Use /NoScanEvents to skip loading any scan events

Use /NoCE to skip trying to determine collision energies

Use /MSLevelOnly to only load MS levels using GetMSLevel

Use /TestFilters to test the parsing of a set of standard scan filters

Use /Trace to display additional debug messages

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

Licensed under the 2-Clause BSD License; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute
