# Thermo Raw File Reader

The Thermo Raw File Reader is a .NET DLL wrapper for reading 
Thermo .raw files.  It originally used the Thermo MS File Reader, 
but in January 2019 we switched to using the ThermoFisher.CommonCore C# DLLs

## Features

The Thermo Raw File Reader DLL provides several methods for parsing the information extracted from Thermo .raw files, including:
* Determining the parent ion m/z and fragmentation mode in a given scan filter
* Determining the Ionization mode from a given scan filter
* Extracting MRM masses listed in a given scan filter
* Reporting the number of spectra in the .Raw file
* Returning details on a specific spectrum
* Obtaining the raw m/z and intensity values for a given spectrum

## Software Demo

The Test_ThermoRawFileReader directory contains a .NET command-line application 
that illustrates how to interface with ThermoRawFileReaderDLL.dll

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)\
Copyright 2019, Battelle Memorial Institute.  All Rights Reserved.\
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics/

## License

The Thermo Raw File Reader is licensed under the 2-Clause BSD License; you may not use this program 
except in compliance with the License. You may obtain a copy of the License at 
https://opensource.org/licenses/BSD-2-Clause

Copyright 2018 Battelle Memorial Institute

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved.
