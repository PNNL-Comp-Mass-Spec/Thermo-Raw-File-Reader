@echo off
echo Copy the DLL, PDF, and XML to Lib folders
@echo on
xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\RefLib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\DataMining\MASIC\Lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\Lib\" /Y
xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\DataMining\MSGF_Runner\Lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\BrianLaMarche\MultiAlign\lib\InformedProteomics" /Y
xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\BrianLaMarche\MultiAlign\src\MultiAlignRogue\bin\Debug\" /Y


xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\DLLLibrary" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\SangtaeKim\InformedProteomics\lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\Documents\Projects\KevinCrowell\IMSDemultiplexer\IMSDemultiplexer\lib" /Y

@echo off
echo Copy the DLL to bin folders
echo Note that the DLL is compiled as AnyCPU
@echo on

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Decon2ls_PlugIn_Decon2LSV2\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Extraction_PlugIn\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_GlyQIQ_Plugin\bin\Debug\ThermoRawFileReader.dll
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_GlyQIQ_Plugin\GlyQResultsSummarizer\bin\Debug\ThermoRawFileReader.dll
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ICR2LS_PlugIn\bin\Debug\ThermoRawFileReader.dll
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\Debug\ThermoRawFileReader.dll
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\MSGFPlusIndexFileCopier\bin\Debug\ThermoRawFileReader.dll
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Mz_Refinery_Plugin\bin\Debug\ThermoRawFileReader.dll

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug_NoDartFTP\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\DeployedFiles" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\CodeTestCS\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\CodeTestCS\lib" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MASIC\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MASIC\bin\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MASIC\MASICTest\bin\Debug\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MASICResultsMerger\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MASICResultsMerger\Lib" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MS_File_Info_Scanner\bin\DLL\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\MSGF_Runner\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\lib\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\DLL\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\Exe\Debug" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\ProteowizardWrapper\UnitTests\lib\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\DataMining\ThermoRawFileReader\Test_ThermoRawFileReader\bin\Debug\" /Y

rem xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\" /Y


xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x64\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x64\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Testing.ProblemCases\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.ProblemTesting\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x64\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x86\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsoleDMS\bin\x64\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x64\Release\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\Library" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Release" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\DimethylLabelingIq\Library" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\GordonSlysz\IqTargetCreator\Library" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\IDM_Console\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Release" /Y

rem xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\SangtaeKim\InformedProteomics\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\KevinCrowell\LipidTools\lib" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\KevinCrowell\LipidTools\LipidTools\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\Documents\Projects\KevinCrowell\LipidTools\LipidToolsTest\bin\x86\Debug" /Y

@echo off
pause
