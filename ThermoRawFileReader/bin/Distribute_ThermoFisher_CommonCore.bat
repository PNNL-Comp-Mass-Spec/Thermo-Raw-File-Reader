@echo off
echo Copy the DLLs to Lib directories
echo Note that several projects download ThermoRawFileReader from NuGet and also reference a local NuGet package to obtain the Thermo CommonCore DLLs
echo Still, the DLLs are included in some key projects
@echo on
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\RefLib\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\BrianLaMarche\MultiAlign\lib\InformedProteomics\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\BrianLaMarche\MultiAlign\src\MultiAlignRogue\bin\Debug\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\SangtaeKim\InformedProteomics\lib\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\KevinCrowell\IMSDemultiplexer\IMSDemultiplexer\lib\" /Y /D
if not "%1"=="NoPause" pause

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\RefLib\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\Lib\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\BrianLaMarche\MultiAlign\lib\InformedProteomics\" /Y /D
xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\BrianLaMarche\MultiAlign\src\MultiAlignRogue\bin\Debug\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\SangtaeKim\InformedProteomics\lib\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\KevinCrowell\IMSDemultiplexer\IMSDemultiplexer\lib\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\DataMining\ThermoFAIMStoMzML\lib\" /Y /D

xcopy ..\..\RawFileReaderLicense.doc "F:\Documents\Projects\DataMining\ThermoPeakDataExporter\lib\" /Y /D

if not "%1"=="NoPause" pause

@echo off
echo Copy the DLL to bin directories
echo Note that the DLL is compiled as AnyCPU
@echo on

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\CodeTestCS\bin\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\CodeTestCS\lib\" /Y /D

rem Skip these since the DLLs are obtained via a NuGet package at C:\NuPkg
rem xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\" /Y /D
rem xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\DLL\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\lib\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\DLL\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\Exe\Debug\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\ThermoFAIMStoMzML\lib\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\ThermoPeakDataExporter\lib\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\DataMining\ThermoRawFileReader\Test_ThermoRawFileReader\bin\Debug\" /Y /D

rem xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\" /Y /D
if not "%1"=="NoPause" pause

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x64\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x64\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Testing.ProblemCases\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.ProblemTesting\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x64\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x64\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Release\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\Library\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Release\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\DimethylLabelingIq\Library\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\GordonSlysz\IqTargetCreator\Library\" /Y /D
if not "%1"=="NoPause" pause

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\IDM_Console\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Release\" /Y /D

rem xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\SangtaeKim\InformedProteomics\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\KevinCrowell\LipidTools\lib\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\KevinCrowell\LipidTools\LipidTools\bin\x86\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\KevinCrowell\LipidTools\LipidToolsTest\bin\x86\Debug\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\Bryson_Gibbons\Thermo-Raw-Metadata-Plotter\Lib\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\Bryson_Gibbons\Thermo-Raw-Metadata-Plotter\ThermoRawMetadataPlotter\bin\Debug\" /Y /D

xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Lib\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\FAIMS MzXML Generator\FAIMS MzXML Generator\bin\Debug\" /Y /D
xcopy Release\net472\ThermoFisher.CommonCore*.dll "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\WriteFaimsXMLFromRawFile\WriteFaimsXMLFromRawFile\bin\Debug" /Y /D


@echo off
if not "%1"=="NoPause" pause
