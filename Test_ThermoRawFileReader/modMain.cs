using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using FileProcessor;
using ThermoRawFileReader;

namespace Test_ThermoRawFileReader
{
    static class modMain
    {

        private const string DEFAULT_FILE_PATH = @"..\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW";

        public static void Main()
        {
            var commandLineParser = new clsParseCommandLine();
            commandLineParser.ParseCommandLine();

            if (commandLineParser.NeedToShowHelp)
            {
                ShowProgramHelp();
                return;
            }

            var extractScanFilters = commandLineParser.IsParameterPresent("GetFilters");
            if (extractScanFilters)
            {
                var workingDirectory = ".";

                if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    workingDirectory = commandLineParser.RetrieveNonSwitchParameter(0);
                }
                ExtractScanFilters(workingDirectory);
                return;
            }

            var sourceFilePath = DEFAULT_FILE_PATH;

            if (commandLineParser.NonSwitchParameterCount > 0)
            {
                sourceFilePath = commandLineParser.RetrieveNonSwitchParameter(0);
            }

            var fiSourceFile = new FileInfo(sourceFilePath);

            if (!fiSourceFile.Exists)
            {
                Console.WriteLine("File not found: " + fiSourceFile.FullName);
                return;
            }

            var centroid = commandLineParser.IsParameterPresent("centroid");
            var testSumming = commandLineParser.IsParameterPresent("sum");

            var startScan = 0;
            var endScan = 0;

            int intValue;

            if (commandLineParser.RetrieveValueForParameter("Start", out var strValue))
            {
                if (int.TryParse(strValue, out intValue))
                {
                    startScan = intValue;
                }
            }

            if (commandLineParser.RetrieveValueForParameter("End", out strValue))
            {
                if (int.TryParse(strValue, out intValue))
                {
                    endScan = intValue;
                }
            }

            TestScanFilterParsing();

            TestReader(fiSourceFile.FullName, centroid, testSumming, startScan, endScan);

            if (centroid)
            {
                // Also process the file with centroiding off
                TestReader(fiSourceFile.FullName, false, testSumming, startScan, endScan);
            }

            // Uncomment the following to test the GetCollisionEnergy() function
            // TestReader(@"..\EDRN_ERG_Spop_ETV1_50fmolHeavy_0p5ugB53A_Frac48_3Oct12_Gandalf_W33A1_16a.raw");

            TestGetAllScanEvents(@"\\proto-2\UnitTest_Files\ThermoRawFileReader\QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw");

            Console.WriteLine("Done");

        }

        private static void ExtractScanFilters(string directoryToScan)
        {
            var reParentIonMZ = new Regex("[0-9.]+@", RegexOptions.Compiled);

            var reParentIonMZnoAt = new Regex("(ms[2-9]|cnl|pr) [0-9.]+ ", RegexOptions.Compiled);

            var reMassRange = new Regex(@"[0-9.]+-[0-9.]+", RegexOptions.Compiled);

            var reCollisionMode = new Regex("(?<CollisionMode>cid|etd|hcd|pqd)[0-9.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var reSID = new Regex("sid=[0-9.]+", RegexOptions.Compiled);


            var diWorkingDirectory = new DirectoryInfo(directoryToScan);
            if (!diWorkingDirectory.Exists)
            {
                Console.WriteLine("Folder not found: " + diWorkingDirectory.FullName);
                return;
            }

            // Keys in this dictionary are generic scan filters
            // Values are a tuple of <ScanFilter, Observation Count, First Dataset>
            var lstFilters = new Dictionary<string, Tuple<string, int, string>>();

            // Find the Masic _ScanStatsEx.txt files in the source folder
            var scanStatsFiles = diWorkingDirectory.GetFiles("*_ScanStatsEx.txt");

            if (scanStatsFiles.Length == 0)
            {
                Console.WriteLine("No _ScanStatsEx.txt files were found in folder " + diWorkingDirectory.FullName);
                return;
            }

            Console.WriteLine("Parsing _ScanStatsEx.txt files in folder " + diWorkingDirectory.Name);
            var dtLastProgress = DateTime.UtcNow;

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

                        var scanFilterGeneric = reParentIonMZ.Replace(scanFilter, "0@");
                        scanFilterGeneric = reMassRange.Replace(scanFilterGeneric, "0.00-0.00");
                        scanFilterGeneric = reSID.Replace(scanFilterGeneric, "sid=0.00");

                        var matchesParentNoAt = reParentIonMZnoAt.Matches(scanFilterGeneric);
                        foreach (Match match in matchesParentNoAt)
                        {
                            scanFilterGeneric = scanFilterGeneric.Replace(match.Value, match.Groups[1].Value + " 0.000");
                        }

                        var matchesCollision = reCollisionMode.Matches(scanFilterGeneric);
                        foreach (Match match in matchesCollision)
                        {
                            scanFilterGeneric = scanFilterGeneric.Replace(match.Value, match.Groups[1].Value + "00.00");
                        }

                        if (lstFilters.TryGetValue(scanFilterGeneric, out var filterStats))
                        {
                            lstFilters[scanFilterGeneric] = new Tuple<string, int, string>(
                                filterStats.Item1,
                                filterStats.Item2 + 1,
                                filterStats.Item3);
                        }
                        else
                        {
                            lstFilters.Add(scanFilterGeneric, new Tuple<string, int, string>(scanFilter, 1, dataFile.Name));
                        }

                    }
                }

                filesProcessed++;

                if (DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 2)
                {
                    dtLastProgress = DateTime.UtcNow;
                    var percentComplete = filesProcessed / (float)scanStatsFiles.Length * 100.0;
                    Console.WriteLine(percentComplete.ToString("0.0") + "% complete");
                }

            }

            // Write the cached scan filters
            var outputFilePath = Path.Combine(diWorkingDirectory.FullName, "ScanFiltersFound.txt");

            using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine("{0}\t{1}\t{2}\t{3}", "Generic_Filter", "Example_Filter", "Count", "First_Dataset");

                foreach (var filter in lstFilters)
                {
                    writer.WriteLine("{0}\t{1}\t{2}\t{3}", filter.Key, filter.Value.Item1, filter.Value.Item2, filter.Value.Item3);
                }
            }
        }

        private static void ShowProgramHelp()
        {
            var assemblyNameLocation = Assembly.GetExecutingAssembly().Location;

            Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(assemblyNameLocation));
            Console.WriteLine(" InputFilePath.raw [/Centroid] [/Sum] [/Start:Scan] [/End:Scan]");

            Console.WriteLine("Running this program without any parameters it will process file " + DEFAULT_FILE_PATH);
            Console.WriteLine();
            Console.WriteLine("The first parameter specifies the file to read");
            Console.WriteLine();
            Console.WriteLine("Use /Centroid to centroid the data when reading");
            Console.WriteLine("Use /Sum to test summing the data across 15 scans (each spectrum will be shown twice; once with summing and once without)");
            Console.WriteLine();
            Console.WriteLine("Use /Start and /End to limit the scan range to process");
            Console.WriteLine("If /Start and /End are not provided, then will read every 21 scans");

        }


        private static void TestReader(string rawFilePath, bool centroid = false, bool testSumming = false, int scanStart = 0, int scanEnd = 0)
        {
            try
            {
                if (!File.Exists(rawFilePath))
                {
                    Console.WriteLine("File not found, skipping: " + rawFilePath);
                    return;
                }

                using (var oReader = new XRawFileIO(rawFilePath))
                {

                    foreach (var method in oReader.FileInfo.InstMethods)
                    {
                        Console.WriteLine(method);
                    }

                    var iNumScans = oReader.GetNumScans();

                    var strCollisionEnergies = string.Empty;

                    ShowMethod(oReader);

                    var scanStep = 1;

                    if (scanStart < 1)
                        scanStart = 1;
                    if (scanEnd < 1)
                    {
                        scanEnd = iNumScans;
                        scanStep = 21;
                    }
                    else
                    {
                        if (scanEnd < scanStart)
                        {
                            scanEnd = scanStart;
                        }
                    }

                    for (var scanNum = scanStart; scanNum <= scanEnd; scanNum += scanStep)
                    {
                        var success = oReader.GetScanInfo(scanNum, out clsScanInfo oScanInfo);
                        if (!success)
                            continue;

                        Console.Write("Scan " + scanNum + " at " + oScanInfo.RetentionTime.ToString("0.00") + " minutes: " + oScanInfo.FilterText);
                        var lstCollisionEnergies = oReader.GetCollisionEnergy(scanNum);

                        if (lstCollisionEnergies.Count == 0)
                        {
                            strCollisionEnergies = string.Empty;
                        }
                        else if (lstCollisionEnergies.Count >= 1)
                        {
                            strCollisionEnergies = lstCollisionEnergies[0].ToString("0.0");

                            if (lstCollisionEnergies.Count > 1)
                            {
                                for (var intIndex = 1; intIndex <= lstCollisionEnergies.Count - 1; intIndex++)
                                {
                                    strCollisionEnergies += ", " + lstCollisionEnergies[intIndex].ToString("0.0");
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(strCollisionEnergies))
                        {
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine("; CE " + strCollisionEnergies);
                        }

                        if (oScanInfo.TryGetScanEvent("Monoisotopic M/Z:", out var monoMZ))
                        {
                            Console.WriteLine("Monoisotopic M/Z: " + monoMZ);
                        }

                        if (oScanInfo.TryGetScanEvent("Charge State", out var chargeState, true))
                        {
                            Console.WriteLine("Charge State: " + chargeState);
                        }

                        if (oScanInfo.TryGetScanEvent("MS2 Isolation Width", out var isolationWidth, true))
                        {
                            Console.WriteLine("MS2 Isolation Width: " + isolationWidth);
                        }

                        if (scanNum % 50 != 0 && scanEnd - scanStart > 50)
                            continue;

                        // Get the data for scan scanNum
                        Console.WriteLine();
                        Console.WriteLine("Spectrum for scan " + scanNum);

                        oReader.GetScanData(scanNum, out var dblMzList, out var dblIntensityList, 0, centroid);

                        var mzDisplayStepSize = 50;
                        if (centroid)
                        {
                            mzDisplayStepSize = 1;
                        }

                        for (var iDataPoint = 0; iDataPoint <= dblMzList.Length - 1; iDataPoint += mzDisplayStepSize)
                        {
                            Console.WriteLine("  " + dblMzList[iDataPoint].ToString("0.000") + " mz   " + dblIntensityList[iDataPoint].ToString("0"));
                        }
                        Console.WriteLine();

                        const int scansToSum = 15;

                        if (scanNum + scansToSum < iNumScans & testSumming)
                        {
                            // Get the data for scan scanNum through scanNum + 15

                            oReader.GetScanDataSumScans(scanNum, scanNum + scansToSum, out var dblMassIntensityPairs, 0, centroid);

                            Console.WriteLine("Summed spectrum, scans " + scanNum + " through " + (scanNum + scansToSum));

                            for (var iDataPoint = 0; iDataPoint <= dblMassIntensityPairs.GetLength(1) - 1; iDataPoint += 50)
                            {
                                Console.WriteLine("  " + dblMassIntensityPairs[0, iDataPoint].ToString("0.000") + " mz   " +
                                                  dblMassIntensityPairs[1, iDataPoint].ToString("0"));
                            }

                            Console.WriteLine();
                        }

                        if (!oScanInfo.IsFTMS)
                            continue;

                        var dataCount = oReader.GetScanLabelData(scanNum, out var ftLabelData);

                        Console.WriteLine();
                        Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,12}", "Mass", "Intensity", "Resolution", "Baseline", "Noise", "Charge");


                        for (var iDataPoint = 0; iDataPoint <= dataCount - 1; iDataPoint += 50)
                        {
                            Console.WriteLine("{0,12:F3}{1,12:0}{2,12:0}{3,12:F1}{4,12:0}{5,12:0}",
                                              ftLabelData[iDataPoint].Mass,
                                              ftLabelData[iDataPoint].Intensity,
                                              ftLabelData[iDataPoint].Resolution,
                                              ftLabelData[iDataPoint].Baseline,
                                              ftLabelData[iDataPoint].Noise,
                                              ftLabelData[iDataPoint].Charge);
                        }

                        dataCount = oReader.GetScanPrecisionData(scanNum, out var ftPrecisionData);

                        Console.WriteLine();
                        Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}", "Mass", "Intensity", "AccuracyMMU", "AccuracyPPM", "Resolution");


                        for (var iDataPoint = 0; iDataPoint <= dataCount - 1; iDataPoint += 50)
                        {
                            Console.WriteLine("{0,12:F3}{1,12:0}{2,12:F3}{3,12:F3}{4,12:0}",
                                              ftPrecisionData[iDataPoint].Mass,
                                              ftPrecisionData[iDataPoint].Intensity, ftPrecisionData[iDataPoint].AccuracyMMU,
                                              ftPrecisionData[iDataPoint].AccuracyPPM,
                                              ftPrecisionData[iDataPoint].Resolution);
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
                if (!File.Exists(rawFilePath))
                {
                    Console.WriteLine("File not found, skipping: " + rawFilePath);
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("Opening " + rawFilePath);

                // Keys in this dictionary are event names
                // Values are dictionaries tracking all of the values for each event (key is value, value is occurrence of that value)
               var scanEventStats = new Dictionary<string, Dictionary<string, int>>();

                var lastProgress = DateTime.UtcNow;
                var scansRead = 0;

                using (var oReader = new XRawFileIO(rawFilePath))
                {
                    var scanCount = oReader.GetNumScans();

                    for (var scanNum = 1; scanNum <= scanCount; scanNum++)
                    {
                        var success = oReader.GetScanInfo(scanNum, out clsScanInfo oScanInfo);

                        if (!success)
                        {
                            ShowWarning("GetScanInfo returned false for scan " + scanNum);
                            continue;
                        }
                        foreach (var eventItem in oScanInfo.ScanEvents)
                        {
                            if (scanEventStats.TryGetValue(eventItem.Key, out var valueStats))
                            {
                            }
                            else
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
                        if (DateTime.UtcNow.Subtract(lastProgress).TotalSeconds > 3 && scansRead % 100 == 0)
                        {
                            lastProgress = DateTime.UtcNow;
                            var percentComplete = scansRead / (double)scanCount * 100;
                            Console.WriteLine("Reading scan events; {0:F1}% complete ({1} / {2} scans)", percentComplete, scansRead, scanCount);
                        }
                    }

                }

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
            // Note: See also class ThermoReaderUnitTests in the RawFileReaderUnitTests project

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
                var genericFilter = XRawFileIO.MakeGenericFinniganScanFilter(filterItem);
                var scanType = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(filterItem);

                Console.WriteLine(filterItem);
                Console.WriteLine("  {0,-12} {1}", scanType, genericFilter);

                var validMS1Scan = XRawFileIO.ValidateMSScan(filterItem, out var intMSLevel, out var blnSIMScan, out var eMRMScanType, out var blnZoomScan);

                if (validMS1Scan)
                {
                    if (eMRMScanType == MRMScanTypeConstants.NotMRM)
                    {
                        Console.Write("  MSLevel={0}", intMSLevel);
                    }
                    else
                    {
                        Console.Write("  MSLevel={0}, MRMScanType={1}", intMSLevel, eMRMScanType);
                    }

                    if (blnSIMScan)
                    {
                        Console.Write(", SIM Scan");
                    }

                    if (blnZoomScan)
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


        private static void ShowMethod(XRawFileIO oReader)
        {

            try
            {
                foreach (var method in oReader.FileInfo.InstMethods)
                {

                    Console.WriteLine("Instrument model: " + oReader.FileInfo.InstModel);
                    Console.WriteLine("Instrument name: " + oReader.FileInfo.InstName);
                    Console.WriteLine("Instrument description: " + oReader.FileInfo.InstrumentDescription);
                    Console.WriteLine("Instrument serial number: " + oReader.FileInfo.InstSerialNumber);
                    Console.WriteLine();

                    Console.WriteLine(method);

                }

            }
            catch (Exception ex)
            {
                ShowError("Error loading the MS Method: " + ex.Message, ex);
            }

        }

    }
}
