@echo off
echo Copy the DLL, PDF, and XML to Lib folders
@echo on
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\PHRP\" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\RefLib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\MASIC\Lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\MS_File_Info_Scanner\Lib\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\MSGF_Runner\Lib\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\PHRPReader\Lib\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\PeptideListToXML\Lib\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\Lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\MTDB_Creator\trunk\libs\" /Y
xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\DataMining\SMAQC\SMAQC\dll" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\BrianLaMarche\MultiAlign\lib\" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\DLLLibrary" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\SangtaeKim\InformedProteomics\lib" /Y

xcopy Release\ThermoRawFileReader.* "F:\My Documents\Projects\KevinCrowell\IMSDemultiplexer\IMSDemultiplexer\lib" /Y

@echo off
echo Copy the DLL to bin folders
echo Note that the DLL is compiled as AnyCPU
@echo on

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\AM_Shared\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Program\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Ape_PlugIn\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_AScore_PlugIn\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Cyclops_PlugIn\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_DataImport_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Decon2ls_PlugIn_Decon2LSV2\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_DTA_Import_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_DTA_Split_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_DtaRefinery_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_DTASpectraFileGen_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Extraction_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ICR2LS_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_IDM_Plugin\AM_IDM_Plugin\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_IDPicker_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InSpecT_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_InspectResultsAssembly_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_LCMSFeatureFinder_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_LipidMapSearch_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Mage_PlugIn\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Masic_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSAlign_Histone_Plugin\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSAlign_Plugin\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSAlign_Quant_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSDeconv_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGF_PlugIn\Bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGF_PlugIn\MSGF_Results_Summarizer\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_IMS_Plugin\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSGFDB_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSMSSpectrumFilter_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSXML_Bruker_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MSXML_Gen_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MultiAlign_Aggregator_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_MultiAlign_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_OMSSA_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Phospho_FDR_Aggregator_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_PRIDE_Converter_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_PRIDE_MzXML_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ProSightPC_Quant_Plugin\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ResultsCleanup_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_ResultsXfer_PlugIn\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_Sequest_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_SMAQC_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Plugins\AM_XTandem_PlugIn\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Test_Plugins\TestApePlugIn\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Test_Plugins\TestAScorePlugIn\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\Test_Plugins\TestMagePlugIn\bin\Debug\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\CaptureTaskManager\CaptureTaskManager\bin\Debug_NoDartFTP\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\DMS_Managers\Capture_Task_Manager\DeployedFiles" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\CodeTestCS\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\CodeTestCS\lib" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MASIC\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MASICResultsMerger\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MASICResultsMerger\Lib" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MS_File_Info_Scanner\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MS_File_Info_Scanner\bin\DLL\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\MSGF_Runner\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PEKtoCSVConverter\lib\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\DLL\Debug\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PEKtoCSVConverter\PEKtoCSVConverter\bin\Exe\Debug" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\CreateMSGFDBResultsFileFromPHRP\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\PeptideHitResultsProcessor\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\PHRPReader\bin\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideHitResultsProcessor\Test_PHRPReader\bin" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\PeptideListToXML\bin\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x86\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\bin\x64\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\x86\" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\Protein_Coverage_Summarizer\PeptideToProteinMapper\PeptideToProteinMapEngine\bin\x64\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\SMAQC\SMAQC\SMAQC\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\SMAQC\SMAQC\SMAQC\bin\release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\DataMining\ThermoRawFileReader\Test_ThermoRawFileReader\bin\Debug\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconConsole\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Backend\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Testing.ProblemCases\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.UnitTesting2\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.ProblemTesting\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows.UnitTesting\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\DeconTools.Workflows\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowConsole\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconTools.Workflows\TargetedWorkflowManagerConsole\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\DeconToolsAutoProcessV1\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\IQ\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\Library" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DeconTools_IQ\TestConsole1\bin\x86\Release" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\DimethylLabelingIq\Library" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\GordonSlysz\IqTargetCreator\Library" /Y

if exist "F:\My Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Debug"   xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Debug" /Y
if exist "F:\My Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Release" xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\AScore\AScore_Console\bin\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\AScore\AScore_DLL\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\AScore\AScore_DLL\bin\x86\Release" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\AScore\AScore_DLL\lib" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\InterferenceDetection\IDM_Console\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\JoshAldrich\InterferenceDetection\InterDetect\bin\Release" /Y

rem xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\SangtaeKim\InformedProteomics\" /Y

xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\KevinCrowell\LipidTools\lib" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\KevinCrowell\LipidTools\LipidTools\bin\x86\Debug" /Y
xcopy Release\ThermoRawFileReader.dll "F:\My Documents\Projects\KevinCrowell\LipidTools\LipidToolsTest\bin\x86\Debug" /Y

@echo off
pause
