using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PRISM;
using PRISM.Logging;
using ThermoFisher.CommonCore.Data.Business;
using ThermoRawFileReader;

namespace Test_ThermoRawFileReader
{
    static class Program
    {
        private const string PROGRAM_DATE = "February 26, 2020";

        private const string DEFAULT_FILE_PATH = @"..\..\..\UnitTests\Docs\Angiotensin_AllScans.raw";

        private static string mSourceFilePath;
        private static int mStartScan;
        private static int mEndScan;

        private static bool mExtractScanFilters;
        private static bool mCentroid;
        private static bool mTestSumming;

        private static int mScanInfoInterval;

        private static bool mLoadChromatograms;
        private static bool mLoadMethods;
        private static bool mLoadScanData;
        private static bool mGetScanEvents;
        private static bool mLoadCollisionEnergies;
        private static bool mOnlyLoadMSLevelInfo;
        private static bool mTestScanFilters;
        private static bool mTraceMode;

        public static void Main()
        {
            mScanInfoInterval = 1;

            mLoadChromatograms = false;
            mLoadMethods = true;
            mLoadScanData = true;
            mGetScanEvents = true;
            mLoadCollisionEnergies = true;
            mOnlyLoadMSLevelInfo = false;

            var commandLineParser = new clsParseCommandLine();
            commandLineParser.ParseCommandLine();

            if (commandLineParser.NeedToShowHelp)
            {
                ShowProgramHelp();
                return;
            }

            if (Path.DirectorySeparatorChar == '/')
                mSourceFilePath = DEFAULT_FILE_PATH.Replace('\\', '/');
            else
                mSourceFilePath = DEFAULT_FILE_PATH;

            ParseCommandLineParameters(commandLineParser);

            if (mExtractScanFilters)
            {
                var workingDirectory = ".";

                if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    workingDirectory = commandLineParser.RetrieveNonSwitchParameter(0);
                }
                ExtractScanFilters(workingDirectory);
                System.Threading.Thread.Sleep(1500);
                return;
            }

            var sourceFile = new FileInfo(mSourceFilePath);

            if (!sourceFile.Exists)
            {
                Console.WriteLine("File not found: " + sourceFile.FullName);
                System.Threading.Thread.Sleep(1500);
                return;
            }

            Console.WriteLine("Opening " + sourceFile.FullName);

            if (mTestScanFilters)
            {
                TestScanFilterParsing();
            }

            if (mLoadChromatograms)
            {
                TestLoadChromatograms(sourceFile.FullName);
            }

            TestReader(sourceFile.FullName, mCentroid, mTestSumming, mStartScan, mEndScan);

            if (mCentroid)
            {
                // Also process the file with centroiding off
                TestReader(sourceFile.FullName, false, mTestSumming, mStartScan, mEndScan);
            }

            if (mGetScanEvents)
            {
                TestGetAllScanEvents(sourceFile.FullName);
            }

            Console.WriteLine("Done");

            System.Threading.Thread.Sleep(150);

        }

        /// <summary>
        /// Parse scan filters in one or more _ScanStatsEx.txt files
        /// </summary>
        /// <param name="directoryToScan"></param>
        private static void ExtractScanFilters(string directoryToScan)
        {
            var parentIonMZMatcher = new Regex("[0-9.]+@", RegexOptions.Compiled);

            var parentIonMZnoAtMatcher = new Regex("(ms[2-9]|cnl|pr) [0-9.]+ ", RegexOptions.Compiled);

            var massRangeMatcher = new Regex(@"[0-9.]+-[0-9.]+", RegexOptions.Compiled);

            var collisionModeMatcher = new Regex("(?<CollisionMode>cid|etd|hcd|pqd)[0-9.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var sidMatcher = new Regex("sid=[0-9.]+", RegexOptions.Compiled);

            var workingDirectory = new DirectoryInfo(directoryToScan);
            if (!workingDirectory.Exists)
            {
                Console.WriteLine("Directory not found: " + workingDirectory.FullName);
                return;
            }

            // Keys in this dictionary are generic scan filters
            // Values are a tuple of <ScanFilter, Observation Count, First Dataset>
            var scanFilters = new Dictionary<string, Tuple<string, int, string>>();

            // Find the MASIC _ScanStatsEx.txt files in the source directory
            var scanStatsFiles = workingDirectory.GetFiles("*_ScanStatsEx.txt").ToList();

            if (scanStatsFiles.Count > 0)
            {
                Console.WriteLine("Parsing _ScanStatsEx.txt files in directory " + PathUtils.CompactPathString(workingDirectory.FullName, 75));
            }
            else
            {
                // Look instead in the directory that has DEFAULT_FILE_PATH
                var defaultFile = new FileInfo(DEFAULT_FILE_PATH);
                if (defaultFile.Directory != null && defaultFile.Directory.Exists)
                {
                    var alternateScanStatsFiles = defaultFile.Directory.GetFiles("*_ScanStatsEx.txt").ToList();
                    if (alternateScanStatsFiles.Count > 0)
                    {
                        scanStatsFiles.AddRange(alternateScanStatsFiles);
                        Console.WriteLine("Parsing _ScanStatsEx.txt files in directory " + PathUtils.CompactPathString(defaultFile.Directory.FullName, 75));
                    }
                }
            }

            if (scanStatsFiles.Count == 0)
            {
                Console.WriteLine("No _ScanStatsEx.txt files were found in directory " + workingDirectory.FullName);
                return;
            }

            var lastProgress = DateTime.UtcNow;

            var filesProcessed = 0;
            foreach (var dataFile in scanStatsFiles)
            {
                using (var reader = new StreamReader(new FileStream(dataFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    if (reader.EndOfStream)
                        continue;

                    var headerLine = reader.ReadLine();
                    if (headerLine == null)
                    {
                        continue;
                    }

                    var headers = headerLine.Split('\t');
                    var scanFilterIndex = -1;

                    for (var headerIndex = 0; headerIndex < headers.Length; headerIndex++)
                    {
                        if (headers[headerIndex] == "Scan Filter Text")
                        {
                            scanFilterIndex = headerIndex;
                            break;
                        }
                    }

                    if (scanFilterIndex < 0)
                    {
                        Console.WriteLine("Scan Filter Text not found in: " + dataFile.Name);
                        continue;
                    }

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(dataLine))
                            continue;

                        var dataColumns = dataLine.Split('\t');
                        if (dataColumns.Length < scanFilterIndex)
                            continue;

                        var scanFilter = dataColumns[scanFilterIndex];

                        // Scan filter will be of the form
                        //    FTMS + p NSI d Full ms2 343.4@etd20.00
                        // or ITMS + p NSI d Z ms [163.00-173.00]
                        // or + c NSI d Full ms2 150.100 [10.000-155.100]
                        // etc.

                        // Change 343.4@ to 0@
                        // Also change [163.00-173.00] to [0.00-0.00]
                        //  and change [163.00-173.00,403.00-413.00] to [0.00-0.00,0.00-0.00]
                        // Also change etd20.0 to etd00.0
                        // Also change sid=30.00 to sid=00.00

                        var scanFilterGeneric = parentIonMZMatcher.Replace(scanFilter, "0@");
                        scanFilterGeneric = massRangeMatcher.Replace(scanFilterGeneric, "0.00-0.00");
                        scanFilterGeneric = sidMatcher.Replace(scanFilterGeneric, "sid=0.00");

                        var matchesParentNoAt = parentIonMZnoAtMatcher.Matches(scanFilterGeneric);
                        foreach (Match match in matchesParentNoAt)
                        {
                            scanFilterGeneric = scanFilterGeneric.Replace(match.Value, match.Groups[1].Value + " 0.000");
                        }

                        var matchesCollision = collisionModeMatcher.Matches(scanFilterGeneric);
                        foreach (Match match in matchesCollision)
                        {
                            scanFilterGeneric = scanFilterGeneric.Replace(match.Value, match.Groups[1].Value + "00.00");
                        }

                        if (scanFilters.TryGetValue(scanFilterGeneric, out var filterStats))
                        {
                            scanFilters[scanFilterGeneric] = new Tuple<string, int, string>(
                                filterStats.Item1,
                                filterStats.Item2 + 1,
                                filterStats.Item3);
                        }
                        else
                        {
                            scanFilters.Add(scanFilterGeneric, new Tuple<string, int, string>(scanFilter, 1, dataFile.Name));
                        }

                    }
                }

                filesProcessed++;

                if (DateTime.UtcNow.Subtract(lastProgress).TotalSeconds >= 2)
                {
                    lastProgress = DateTime.UtcNow;
                    var percentComplete = filesProcessed / (float)scanStatsFiles.Count * 100.0;
                    Console.WriteLine(percentComplete.ToString("0.0") + "% complete");
                }

            }

            // Write the cached scan filters
            var outputFilePath = Path.Combine(workingDirectory.FullName, "ScanFiltersFound.txt");

            using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("{0}\t{1}\t{2}\t{3}", "Generic_Filter", "Example_Filter", "Count", "First_Dataset");

                foreach (var filter in scanFilters)
                {
                    writer.WriteLine("{0}\t{1}\t{2}\t{3}", filter.Key, filter.Value.Item1, filter.Value.Item2, filter.Value.Item3);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Scan filters written to file " + PathUtils.CompactPathString(outputFilePath, 95));
        }

        private static string GetAppVersion()
        {
            return PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static void ParseCommandLineParameters(clsParseCommandLine commandLineParser)
        {

            mExtractScanFilters = commandLineParser.IsParameterPresent("GetFilters");

            if (commandLineParser.NonSwitchParameterCount > 0)
            {
                mSourceFilePath = commandLineParser.RetrieveNonSwitchParameter(0);
            }

            mCentroid = commandLineParser.IsParameterPresent("centroid");
            mTestSumming = commandLineParser.IsParameterPresent("sum");

            mStartScan = 0;
            mEndScan = 0;

            if (commandLineParser.RetrieveValueForParameter("Start", out var startScan))
            {
                if (int.TryParse(startScan, out var value))
                {
                    mStartScan = value;
                }
            }

            if (commandLineParser.RetrieveValueForParameter("End", out var endScan))
            {
                if (int.TryParse(endScan, out var value))
                {
                    mEndScan = value;
                }
            }

            if (commandLineParser.RetrieveValueForParameter("ScanInfo", out var scanInfoInterval))
            {
                if (int.TryParse(scanInfoInterval, out var value))
                {
                    mScanInfoInterval = value;
                }
            }

            if (commandLineParser.IsParameterPresent("NoMethods"))
            {
                mLoadMethods = false;
            }

            if (commandLineParser.IsParameterPresent("NoScanData"))
            {
                mLoadScanData = false;
            }

            if (commandLineParser.IsParameterPresent("NoScanEvents"))
            {
                mGetScanEvents = false;
            }

            if (commandLineParser.IsParameterPresent("NoCE"))
            {
                mLoadCollisionEnergies = false;
            }

            mLoadChromatograms = commandLineParser.IsParameterPresent("Chrom") || commandLineParser.IsParameterPresent("Chromatogram");

            mOnlyLoadMSLevelInfo = commandLineParser.IsParameterPresent("MSLevelOnly");

            mTestScanFilters = commandLineParser.IsParameterPresent("TestFilters");

            mTraceMode = commandLineParser.IsParameterPresent("Trace");

        }

        private static void ShowError(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowWarning(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        private static void ShowProgramHelp()
        {
            var exePath = PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppPath();

            Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(exePath));
            Console.WriteLine("  InputFilePath.raw [/GetFilters] [/Centroid] [/Sum] [/Start:Scan] [/End:Scan]");
            Console.WriteLine("  [/ScanInfo:IntervalScans] [/NoScanData] [/NoScanEvents] [/NoCE]");
            Console.WriteLine("  [/NoMethods] [/MSLevelOnly] [/Trace]");

            Console.WriteLine();
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                  "Running this program without any parameters it will process file " + DEFAULT_FILE_PATH));
            Console.WriteLine();
            Console.WriteLine("The first parameter specifies the file to read");
            Console.WriteLine();
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                  "Use /GetFilters to compile and display a list of scan filters in any MASIC _ScanStatsEx.txt files in the working directory"));
            Console.WriteLine();
            Console.WriteLine("Without /GetFilters, data is read from the file, either from all scans, or a scan range");
            Console.WriteLine("Use /Start and /End to limit the scan range to process");
            Console.WriteLine("If /Start and /End are not provided, will read every 21 scans");
            Console.WriteLine();
            Console.WriteLine("Use /Centroid to centroid the data when reading");
            Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                  "Use /Sum to test summing the data across 15 scans " +
                                  "(each spectrum will be shown twice; once with summing and once without)"));
            Console.WriteLine();
            Console.WriteLine("While reading data, the scan number and elution time is displayed for each scan.");
            Console.WriteLine("To show this info every 5 scans, use /ScanInfo:5");
            Console.WriteLine();
            Console.WriteLine("Use /NoScanData to skip loading any scan data");
            Console.WriteLine("Use /NoScanEvents to skip loading any scan events");
            Console.WriteLine("Use /NoCE to skip trying to determine collision energies");
            Console.WriteLine("Use /NoMethods to skip loading instrument methods");
            Console.WriteLine();
            Console.WriteLine("Use /MSLevelOnly to only load MS levels using GetMSLevel");
            Console.WriteLine("Use /TestFilters to test the parsing of a set of standard scan filters");
            Console.WriteLine();
            Console.WriteLine("Use /Trace to display additional debug messages");
            Console.WriteLine();
            Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2012");
            Console.WriteLine("Version: " + GetAppVersion());
            Console.WriteLine();

            Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
            Console.WriteLine("Website: https://omics.pnl.gov or https://panomics.pnnl.gov/");
            Console.WriteLine();


            // Delay for 1.5 seconds in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
            System.Threading.Thread.Sleep(1500);

        }

        private static FileInfo ResolveDataFile(string rawFilePath)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                if (rawFilePath.StartsWith(@"\\"))
                {
                    // Remove the server name from the path and change to forward slashes
                    // For example, switch
                    // from: \\proto-2\UnitTest_Files\ThermoRawFileReader\QC_Mam_16_01_1
                    // to    /UnitTest_Files/ThermoRawFileReader/QC_Mam_16_01_1
                    var slashIndex = rawFilePath.IndexOf('\\', 2);
                    if (slashIndex > 2)
                    {
                        rawFilePath = rawFilePath.Substring(slashIndex).Replace('\\', '/');
                    }
                    else
                    {
                        rawFilePath = rawFilePath.Replace('\\', '/');
                    }

                }
                else
                {
                    rawFilePath = rawFilePath.Replace('\\', '/');
                }
            }

            var rawFile = new FileInfo(rawFilePath);

            if (rawFile.Exists)
                return rawFile;

            // File not found via the full path; check in the local directory
            var alternateFile = new FileInfo(rawFile.Name);

            if (alternateFile.Exists)
            {
                return alternateFile;
            }

            Console.WriteLine("File not found: " + rawFilePath);
            Console.WriteLine("Also considered " + alternateFile.FullName);

            return null;
        }

        private static void TestReader(string rawFilePath, bool centroid = false, bool testSumming = false, int scanStart = 0, int scanEnd = 0)
        {
            try
            {

                var rawFile = ResolveDataFile(rawFilePath);
                if (rawFile == null)
                    return;

                var options = new ThermoReaderOptions
                {
                    LoadMSMethodInfo = mLoadMethods
                };

                using (var reader = new XRawFileIO(rawFile.FullName, options, mTraceMode))
                {
                    RegisterEvents(reader);

                    var numScans = reader.GetNumScans();

                    var collisionEnergyList = string.Empty;

                    ShowMethod(reader);

                    var scanStep = 1;

                    if (scanStart < 1)
                        scanStart = 1;

                    if (scanEnd < 1)
                    {
                        scanEnd = numScans;
                        scanStep = 21;
                    }
                    else
                    {
                        if (scanEnd < scanStart)
                        {
                            scanEnd = scanStart;
                        }
                    }

                    if (scanEnd > numScans)
                        scanEnd = numScans;

                    var msLevelStats = new Dictionary<int, int>();

                    Console.WriteLine();
                    Console.WriteLine("Reading data for scans {0} to {1}, step {2}", scanStart, scanEnd, scanStep);

                    for (var scanNum = scanStart; scanNum <= scanEnd; scanNum += scanStep)
                    {
                        if (scanNum > reader.ScanEnd)
                        {
                            ConsoleMsgUtils.ShowWarning("Exiting for loop since scan number {0} is greater than the max scan number, {1}", scanNum, reader.ScanEnd);
                            break;
                        }

                        if (mOnlyLoadMSLevelInfo)
                        {
                            var msLevel = reader.GetMSLevel(scanNum);

                            if (msLevelStats.TryGetValue(msLevel, out var msLevelObsCount))
                            {
                                msLevelStats[msLevel] = msLevelObsCount + 1;
                            }
                            else
                            {
                                msLevelStats.Add(msLevel, 1);
                            }
                            if (mScanInfoInterval <= 0 || scanNum % mScanInfoInterval == 0)
                                Console.WriteLine("Scan " + scanNum);

                            continue;
                        }

                        var success = reader.GetScanInfo(scanNum, out clsScanInfo scanInfo);

                        if (!success)
                            continue;

                        if (mScanInfoInterval <= 0 || scanNum % mScanInfoInterval == 0)
                            Console.WriteLine("Scan " + scanNum + " at " + scanInfo.RetentionTime.ToString("0.00") + " minutes: " + scanInfo.FilterText);

                        if (mLoadCollisionEnergies)
                        {
                            var collisionEnergies = reader.GetCollisionEnergy(scanNum);

                            if (collisionEnergies.Count == 0)
                            {
                                collisionEnergyList = string.Empty;
                            }
                            else if (collisionEnergies.Count >= 1)
                            {
                                collisionEnergyList = collisionEnergies[0].ToString("0.0");

                                if (collisionEnergies.Count > 1)
                                {
                                    for (var index = 1; index <= collisionEnergies.Count - 1; index++)
                                    {
                                        collisionEnergyList += ", " + collisionEnergies[index].ToString("0.0");
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(collisionEnergyList))
                            {
                                Console.WriteLine();
                            }
                            else
                            {
                                Console.WriteLine("; CE " + collisionEnergyList);
                            }
                        }

                        if (mGetScanEvents)
                        {
                            if (scanInfo.TryGetScanEvent("Monoisotopic M/Z:", out var monoMZ))
                            {
                                Console.WriteLine("Monoisotopic M/Z: " + monoMZ);
                            }

                            if (scanInfo.TryGetScanEvent("Charge State", out var chargeState, true))
                            {
                                Console.WriteLine("Charge State: " + chargeState);
                            }

                            if (scanInfo.TryGetScanEvent("MS2 Isolation Width", out var isolationWidth, true))
                            {
                                Console.WriteLine("MS2 Isolation Width: " + isolationWidth);
                            }

                        }

                        if (!mLoadScanData || (scanNum % 50 != 0 && scanEnd - scanStart > 50))
                            continue;

                        // Get the data for scan scanNum
                        Console.WriteLine();
                        Console.WriteLine("Spectrum for scan " + scanNum);

                        reader.GetScanData(scanNum, out var mzList, out var intensityList, 0, centroid);

                        var mzDisplayStepSize = 50;
                        if (centroid)
                        {
                            mzDisplayStepSize = 1;
                        }

                        for (var i = 0; i <= mzList.Length - 1; i += mzDisplayStepSize)
                        {
                            Console.WriteLine("  " + mzList[i].ToString("0.000") + " mz   " + intensityList[i].ToString("0"));
                        }
                        Console.WriteLine();

                        const int scansToSum = 15;

                        if (scanNum + scansToSum < numScans && testSumming)
                        {
                            // Get the data for scan scanNum through scanNum + 15

#pragma warning disable 618
                            reader.GetScanDataSumScans(scanNum, scanNum + scansToSum, out var massIntensityPairs, 0, centroid);
#pragma warning restore 618

                            Console.WriteLine("Summed spectrum, scans " + scanNum + " through " + (scanNum + scansToSum));

                            for (var i = 0; i <= massIntensityPairs.GetLength(1) - 1; i += 50)
                            {
                                Console.WriteLine("  " + massIntensityPairs[0, i].ToString("0.000") + " mz   " +
                                                  massIntensityPairs[1, i].ToString("0"));
                            }

                            Console.WriteLine();
                        }

                        if (!scanInfo.IsFTMS)
                            continue;

                        var dataCount = reader.GetScanLabelData(scanNum, out var ftLabelData);

                        Console.WriteLine();
                        Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,12}", "Mass", "Intensity", "Resolution", "Baseline", "Noise", "Charge");


                        for (var i = 0; i <= dataCount - 1; i += 50)
                        {
                            Console.WriteLine("{0,12:F3}{1,12:0}{2,12:0}{3,12:F1}{4,12:0}{5,12:0}",
                                              ftLabelData[i].Mass,
                                              ftLabelData[i].Intensity,
                                              ftLabelData[i].Resolution,
                                              ftLabelData[i].Baseline,
                                              ftLabelData[i].Noise,
                                              ftLabelData[i].Charge);
                        }

                        dataCount = reader.GetScanPrecisionData(scanNum, out var ftPrecisionData);

                        Console.WriteLine();
                        Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}", "Mass", "Intensity", "AccuracyMMU", "AccuracyPPM", "Resolution");


                        for (var i = 0; i <= dataCount - 1; i += 50)
                        {
                            Console.WriteLine("{0,12:F3}{1,12:0}{2,12:F3}{3,12:F3}{4,12:0}",
                                              ftPrecisionData[i].Mass,
                                              ftPrecisionData[i].Intensity, ftPrecisionData[i].AccuracyMMU,
                                              ftPrecisionData[i].AccuracyPPM,
                                              ftPrecisionData[i].Resolution);
                        }
                    }

                    if (mOnlyLoadMSLevelInfo)
                    {
                        Console.WriteLine();
                        Console.WriteLine("{0,-10} {1}", "MSLevel", "Scans");
                        foreach (var item in msLevelStats)
                        {
                            Console.WriteLine("{0, -10} {1}", item.Key, item.Value);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                ShowError("Error in TestReader: " + ex.Message, ex);
            }
        }

        private static void TestLoadChromatograms(string rawFilePath)
        {

            try
            {

                var rawFile = ResolveDataFile(rawFilePath);
                if (rawFile == null)
                    return;

                var options = new ThermoReaderOptions
                {
                    LoadMSMethodInfo = mLoadMethods
                };

                Console.WriteLine("Opening " + rawFile.FullName);

                using (var reader = new XRawFileIO(rawFile.FullName, options, mTraceMode))
                {
                    RegisterEvents(reader);

                    var deviceList = reader.FileInfo.Devices;
                    var nonMassSpecDevicesInFile = new Dictionary<Device, int>();

                    Console.WriteLine("{0,-15} {1}", "Device Type", "Count in .Raw file");
                    foreach (var item in deviceList)
                    {
                        Console.WriteLine("{0,-15} {1}", item.Key, item.Value);

                        if (item.Value == 0)
                            continue;

                        if (item.Key != Device.MS && item.Key != Device.MSAnalog)
                        {
                            nonMassSpecDevicesInFile.Add(item.Key, item.Value);
                        }
                    }

                    foreach (var device in nonMassSpecDevicesInFile)
                    {
                        for (var deviceNumber = 1; deviceNumber <= device.Value; deviceNumber++)
                        {
                            var deviceInfo = reader.GetDeviceInfo(device.Key, deviceNumber);

                            var chromData = reader.GetChromatogramData(device.Key, deviceNumber);

                            Console.WriteLine();

                            Console.WriteLine(deviceInfo.DeviceDescription);

                            Console.WriteLine("  Name:       {0}", deviceInfo.InstrumentName);
                            Console.WriteLine("  Model:      {0}", deviceInfo.Model);
                            Console.WriteLine("  Serial:     {0}", deviceInfo.SerialNumber);
                            Console.WriteLine("  SW Version: {0}", deviceInfo.SoftwareVersion);
                            Console.WriteLine("  YAxis: {0}, units {1}", deviceInfo.AxisLabelY, deviceInfo.Units);
                            Console.WriteLine();
                            Console.WriteLine("Data for {0}", deviceInfo.DeviceDescription);
                            Console.WriteLine("{0,-9} {1}", "Scan", "Intensity");

                            var i = 0;
                            foreach (var chromPoint in chromData)
                            {
                                i++;
                                if (i % 100 != 0) continue;

                                Console.WriteLine("{0,-9:N0} {1:F2}", chromPoint.Key, chromPoint.Value);
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                ShowError("Error in TestReader: " + ex.Message, ex);
            }
        }

        private static void TestGetAllScanEvents(string rawFilePath)
        {
            try
            {
                var rawFile = ResolveDataFile(rawFilePath);
                if (rawFile == null)
                    return;

                Console.WriteLine();
                Console.WriteLine("Opening " + rawFile.FullName);

                // Keys in this dictionary are event names
                // Values are dictionaries tracking all of the values for each event (key is value, value is occurrence of that value)
                var scanEventStats = new Dictionary<string, Dictionary<string, int>>();

                var lastProgress = DateTime.UtcNow;
                var scansRead = 0;
                var scansReadSinceLastProgress = 0;

                var options = new ThermoReaderOptions
                {
                    LoadMSMethodInfo = mLoadMethods
                };

                using (var reader = new XRawFileIO(rawFile.FullName, options))
                {
                    var scanCount = reader.GetNumScans();

                    for (var scanNum = 1; scanNum <= scanCount; scanNum++)
                    {

                        var success = reader.GetScanInfo(scanNum, out clsScanInfo scanInfo);

                        if (!success)
                        {
                            ShowWarning("GetScanInfo returned false for scan " + scanNum);
                            continue;
                        }

                        foreach (var eventItem in scanInfo.ScanEvents)
                        {
                            if (!scanEventStats.TryGetValue(eventItem.Key, out var valueStats))
                            {
                                valueStats = new Dictionary<string, int>();
                                scanEventStats.Add(eventItem.Key, valueStats);
                            }

                            if (valueStats.TryGetValue(eventItem.Value, out var valueCount))
                            {
                                valueStats[eventItem.Value] = valueCount + 1;
                            }
                            else
                            {
                                valueStats.Add(eventItem.Value, 1);
                            }
                        }

                        scansRead++;
                        scansReadSinceLastProgress++;

                        var elapsedSeconds = DateTime.UtcNow.Subtract(lastProgress).TotalSeconds;
                        if (!(elapsedSeconds > 3) || scansRead % 100 != 0)
                            continue;

                        lastProgress = DateTime.UtcNow;
                        var percentComplete = scansRead / (double)scanCount * 100;

                        var scansPerSecond = scansReadSinceLastProgress / elapsedSeconds;

                        Console.WriteLine("Reading scan events; {0:F1}% complete ({1} / {2} scans); {3:F0} scans/second",
                                          percentComplete, scansRead, scanCount, scansPerSecond);

                        scansReadSinceLastProgress = 0;
                    }

                }

                Console.WriteLine();
                Console.WriteLine("{0,-38} {1,-10} {2,-20} {3}", "Event name", "Values", "Most Common Value", "Occurrence count");
                foreach (var eventInfo in scanEventStats)
                {
                    var eventName = eventInfo.Key;

                    var valueCount = eventInfo.Value.Count;

                    var occurrenceCount = 0;
                    var mostCommonValue = "";

                    foreach (var item in eventInfo.Value)
                    {
                        if (item.Value <= occurrenceCount) continue;
                        occurrenceCount = item.Value;
                        mostCommonValue = item.Key;
                    }

                    if (string.IsNullOrWhiteSpace(mostCommonValue))
                        Console.WriteLine("{0,-38} {1,-10} {2,-20} {3:N0}", eventName, valueCount, "whitespace", occurrenceCount);
                    else
                        Console.WriteLine("{0,-38} {1,-10} {2,-20} {3:N0}", eventName, valueCount, mostCommonValue, occurrenceCount);
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                ShowError("Error in TestGetAllScanEvents: " + ex.Message, ex);
            }
        }

        private static void TestScanFilterParsing()
        {
            // Note: See also the unit test classes in the ThermoRawFileReaderUnitTests project

            var filterList = new List<string>
            {
                "ITMS + c ESI Full ms [300.00-2000.00]",
                "FTMS + p NSI Full ms [400.00-2000.00]",
                "ITMS + p ESI d Z ms [579.00-589.00]",
                "ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]",
                "ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]",
                "FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]",
                "ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]",
                "+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]",
                "+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]",
                "ITMS + c NSI d Full ms10 421.76@35.00",
                "+ p ms2 777.00@cid30.00 [210.00-1200.00]",
                "+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]",
                "+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]",
                "+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]",
                "+ p NSI Q3MS [150.070-1500.000]",
                "c NSI Full cnl 162.053 [300.000-1200.000]",
                "- p NSI Full ms2 168.070 [300.000-1500.00]",
                "+ c NSI Full ms2 1083.000 [300.000-1500.00]",
                "- p NSI Full ms2 247.060 [300.000-1500.00]",
                "- c NSI d Full ms2 921.597 [300.000-1500.00]",
                "+ c NSI SRM ms2 965.958 [300.000-1500.00]",
                "+ p NSI SRM ms2 1025.250 [300.000-1500.00]",
                "+ c EI SRM ms2 247.000 [300.000-1500.00]",
                "+ p NSI Full ms2 589.840 [300.070-1200.000]",
                "+ p NSI ms [0.316-316.000]",
                "ITMS + p NSI SIM ms",
                "ITMS + c NSI d SIM ms",
                "FTMS + p NSI SIM ms",
                "FTMS + p NSI d SIM ms"
            };

            foreach (var filterItem in filterList)
            {
                var genericFilter = XRawFileIO.MakeGenericThermoScanFilter(filterItem);
                var scanType = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(filterItem);

                Console.WriteLine(filterItem);
                Console.WriteLine("  {0,-12} {1}", scanType, genericFilter);

                var validMS1Scan = XRawFileIO.ValidateMSScan(filterItem, out var msLevel, out var simScan, out var mrmScanType, out var zoomScan);

                if (validMS1Scan)
                {
                    if (mrmScanType == MRMScanTypeConstants.NotMRM)
                    {
                        Console.Write("  MSLevel={0}", msLevel);
                    }
                    else
                    {
                        Console.Write("  MSLevel={0}, MRMScanType={1}", msLevel, mrmScanType);
                    }

                    if (simScan)
                    {
                        Console.Write(", SIM Scan");
                    }

                    if (zoomScan)
                    {
                        Console.Write(", Zoom Scan");
                    }

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("  Not an MS1, SRM, MRM, or SIM scan");
                }

                Console.WriteLine();
            }

            Console.WriteLine();

        }

        private static void ShowMethod(XRawFileIO reader)
        {

            try
            {

                foreach (var method in reader.FileInfo.InstMethods)
                {
                    Console.WriteLine();
                    Console.WriteLine("Instrument model: " + reader.FileInfo.InstModel);
                    Console.WriteLine("Instrument name: " + reader.FileInfo.InstName);
                    Console.WriteLine("Instrument description: " + reader.FileInfo.InstrumentDescription);
                    Console.WriteLine("Instrument serial number: " + reader.FileInfo.InstSerialNumber);
                    Console.WriteLine();

                    if (string.IsNullOrWhiteSpace(method))
                        continue;

                    if (method.Length > 500)
                        Console.WriteLine(method.Substring(0, 500) + " ...");
                    else
                        Console.WriteLine(method);

                }

            }
            catch (Exception ex)
            {
                ShowError("Error loading the MS Method: " + ex.Message, ex);
            }

        }

        #region "EventNotifier events"

        private static void RegisterEvents(IEventNotifier processingClass)
        {
            processingClass.DebugEvent += DebugEventHandler;
            processingClass.StatusEvent += StatusEventHandler;
            processingClass.ErrorEvent += ErrorEventHandler;
            processingClass.WarningEvent += WarningEventHandler;
        }

        private static void DebugEventHandler(string message)
        {
            ConsoleMsgUtils.ShowDebug(message);
        }

        private static void StatusEventHandler(string message)
        {
            Console.WriteLine(message);
        }

        private static void ErrorEventHandler(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void WarningEventHandler(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        #endregion

    }
}
