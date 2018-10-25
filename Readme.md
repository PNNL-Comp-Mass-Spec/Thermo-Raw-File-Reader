The Thermo Raw File Reader is a .NET DLL wrapper for Thermo's MS File Reader, 
which is accessible by creating an account at 
https://thermo.flexnetoperations.com/control/thmo/login then logging in and 
choosing "Utility Software".

The Thermo Raw File Reader DLL provides several methods for parsing the information returned by MSFileReader, including:
- Determining the parent ion m/z and fragmentation mode in a given scan filter
- Determining the Ionization mode from a given scan filter
- Extracting MRM masses listed in a given scan filter
- Reporting the number of spectra in the .Raw file
- Returning details on a specific spectrum
- Obtaining the raw m/z and intensity values for a given spectrum

The Test_ThermoRawFileReader folder contains a .NET command-line application 
that illustrates how to interface with ThermoRawFileReaderDLL.dll

Prior to using ThermoRawFileReaderDLL.dll you must either download and install the MSFileReader,
or use batch file registerFiles.bat in the lib folder to register MSFileReader.XRawfile2.dll

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)

E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
Website: http://ncrr.pnl.gov/ or http://www.sysbio.org/resources/staff/
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
