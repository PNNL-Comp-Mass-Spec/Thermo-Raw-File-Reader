@echo off
echo.
echo.
echo.
echo ================================================================================================
echo This batch file is obsolete since we now reference the ThermoRawFileReader using a NuGet package
echo See \\proto-2\CI_Publish\ThermoRawFileReader
echo.
echo Copy the .nupkg file from \\proto-2\CI_Publish\NuGet\
echo to                        C:\NuPkg
echo ================================================================================================
echo.
echo.

Goto Done

echo Copy the DLL, PDB, and XML to Lib directories
@echo on
xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\ /Y /D

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\RefLib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\DataMining\MASIC\Lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\DataMining\MS_File_Info_Scanner\Lib\ /Y /D
xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\DataMining\MSGF_Runner\Lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\Josh_Aldrich\InterferenceDetection\InterDetect\DLLLibrary\ /Y /D

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\Kevin_Crowell\IMSDemultiplexer\IMSDemultiplexer\lib\ /Y /D

pause

@echo off
echo Copy the DLL to bin directories
echo Note that the DLL is compiled as AnyCPU
@echo on

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Decon2ls_PlugIn_Decon2LSV2\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Extraction_PlugIn\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_GlyQIQ_Plugin\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_GlyQIQ_Plugin\GlyQResultsSummarizer\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ICR2LS_PlugIn\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\MSGFPlusIndexFileCopier\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Mz_Refinery_Plugin\bin\Debug\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug_NoDartFTP\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\DeployedFiles\ /Y /D
pause

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\CodeTestCS\bin\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\CodeTestCS\lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MASIC\bin\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MASIC\bin\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MASIC\MASICTest\bin\Debug\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\DLL\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\MSGF_Runner\bin\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\PEKtoCSVConverter\lib\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\DLL\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\Exe\Debug\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\ProteowizardWrapper\UnitTests\lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.*   F:\Documents\Projects\DataMining\SpectrumLook_v2\lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\ThermoFAIMStoMzML\lib\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\ThermoPeakDataExporter\lib /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\DataMining\ThermoRawFileReader\Test_ThermoRawFileReader\bin\Debug\ /Y /D

rem xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\ /Y /D
pause

xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\Gordon_Slysz\DeconEngineV2\C#_Version\lib /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconConsole\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconConsole\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconConsole\bin\x64\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Backend\bin\x64\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Testing.ProblemCases\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.ProblemTesting\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x64\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x64\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\IQ\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\IQ\bin\Release\ /Y /D
xcopy Release\net472\ThermoRawFileReader.* F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\Library\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\TestConsole1\bin\x86\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DeconTools_IQ\TestConsole1\bin\x86\Release\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\SIPPER\Library /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\DimethylLabelingIq\Library\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Gordon_Slysz\IqTargetCreator\Library\ /Y /D
pause

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Josh_Aldrich\InterferenceDetection\IDM_Console\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Josh_Aldrich\InterferenceDetection\InterDetect\bin\Debug\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Josh_Aldrich\InterferenceDetection\InterDetect\bin\Release\ /Y /D

rem xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Kevin_Crowell\LipidTools\lib\ /Y /D
rem xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Kevin_Crowell\LipidTools\LipidTools\bin\x86\Debug\ /Y /D
rem xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Kevin_Crowell\LipidTools\LipidToolsTest\bin\x86\Debug\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Bryson_Gibbons\Thermo-Raw-Metadata-Plotter\Lib\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Bryson_Gibbons\Thermo-Raw-Metadata-Plotter\ThermoRawMetadataPlotter\bin\Debug\ /Y /D

xcopy Release\net472\ThermoRawFileReader.dll F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Lib\ /Y /D
xcopy Release\net472\ThermoRawFileReader.dll "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\FAIMS MzXML Generator\FAIMS MzXML Generator\bin\Debug\" /Y /D
xcopy Release\net472\ThermoRawFileReader.dll "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\WriteFaimsXMLFromRawFile\WriteFaimsXMLFromRawFile\bin\Debug\" /Y /D

xcopy Release\net472\ThermoRawFileReader.XML F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Lib\ /Y /D
xcopy Release\net472\ThermoRawFileReader.XML "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\FAIMS MzXML Generator\FAIMS MzXML Generator\bin\Debug\" /Y /D
xcopy Release\net472\ThermoRawFileReader.XML "F:\Documents\Projects\Instrument-Software\FAIMS_MzXML_Generator\Source Code\WriteFaimsXMLFromRawFile\WriteFaimsXMLFromRawFile\bin\Debug\" /Y /D

:Done

@echo off
pause
