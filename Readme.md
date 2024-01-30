# Thermo Raw File Reader

The Thermo Raw File Reader is a .NET DLL for reading
Thermo .raw files, as acquired on Thermo mass spectrometers
* The Raw File Reader is a wrapper for the ThermoFisher.CommonCore C# DLLs

## Supported C# Versions

* .NET Framework 4.7.2
* .NET Framework 4.8
* .NET Framework 4.8.1
* On Linux, Mono 6.12, or newer

### Incompatible C# Versions

The Thermo Raw File Reader does not work with .NET Core 2.x, or newer
* Unsupported: .NET Core 2.x, .NET Core 3.x, .NET 5.x, .NET 6.x, .NET 7.x, etc.

## Features

The Thermo Raw File Reader DLL provides several methods for parsing the information extracted from Thermo .raw files, including:
* Determining the parent ion m/z and fragmentation mode in a given scan filter
* Determining the Ionization mode from a given scan filter
* Extracting MRM masses listed in a given scan filter
* Reporting the number of spectra in the .Raw file
* Returning details on a specific spectrum
* Obtaining the raw m/z and intensity values for a given spectrum

## Usage Instructions

Download the newest ThermoRawFileReader .zip file from the [Releases page](https://github.com/PNNL-Comp-Mass-Spec/Thermo-Raw-File-Reader/releases)
* Extract the DLL files and place in a `Lib` directory inside your project directory
* In Solution Explorer, right click references and add references to the following DLLs (using `Browse` to select each DLL file)
  * ThermoRawFileReader.dll
  * ThermoFisher.CommonCore.RawFileReader.dll
  * ThermoFisher.CommonCore.BackgroundSubtraction.dll
  * ThermoFisher.CommonCore.Data.dll
  * ThermoFisher.CommonCore.MassPrecisionEstimator.dll
* A reference to `PRISM.dll` will get auto-added since the ThermoRawFileReader references the [PRISM Library NuGet package](https://www.nuget.org/packages/PRISM-Library/)
  * See also https://github.com/PNNL-Comp-Mass-Spec/PRISM-Class-Library

## Software Example 1

The Test_ThermoRawFileReader directory in the Thermo Raw File Reader source code contains a .NET command-line application
that illustrates how to interface with ThermoRawFileReader.dll

## Software Example 2

The Thermo Peak Data Exporter application is an example console application that uses ThermoRawFileReader.dll
* Clone the source code from https://github.com/PNNL-Comp-Mass-Spec/Thermo-Peak-Data-Exporter

When running on Linux with Mono, use command line option `-LoadMethod:false` to disable loading method information
* This is required because the Thermo CommonCore DLLs cannot read method information when using Mono
* Example command line:\
`mono ThermoPeakDataExporter.exe DatasetFile.raw -LoadMethod:false`

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)\
Copyright 2019, Battelle Memorial Institute. All Rights Reserved.\
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://www.pnnl.gov/integrative-omics

## License

The Thermo Raw File Reader is licensed under the 2-Clause BSD License; you may not use this program
except in compliance with the License. You may obtain a copy of the License at
https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.
