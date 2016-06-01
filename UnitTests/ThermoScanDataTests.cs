using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThermoRawFileReader;

namespace RawFileReaderTests
{
    [TestFixture]
    public class ThermoScanDataTests
    {
        private const bool USE_REMOTE_PATHS = false;

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 3316)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 71147)]
        public void TestGetNumScans(string rawFileName, int expectedResult)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new ThermoRawFileReader.XRawFileIO(dataFile.FullName))
            {
                var scanCount = reader.GetNumScans();

                Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);
                Assert.AreEqual(expectedResult, scanCount, "Scan count mismatch");
            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521, 3, 6)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165, 3, 42)]
        public void TestGetScanInfo(string rawFileName, int scanStart, int scanEnd, int expectedMS1, int expectedMS2)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            // Keys in this dictionary are the scan number whose metadata is being retrieved
            var file1Data = new Dictionary<int, string> {
                {1513, "1 1   851 44.57 400 2000 6.3E+8 1089.978 1.2E+7     0.00 CID       Positive False False 11 79 + c ESI Full..."},
                {1514, "2 2   109 44.60 230 1780 5.0E+6  528.128 7.2E+5   884.41 CID   cid Positive False False 11 79 + c d Full m..."},
                {1515, "2 3   290 44.63 305 2000 2.6E+7 1327.414 6.0E+6  1147.67 CID   cid Positive False False 11 79 + c d Full m..."},
                {1516, "2 4   154 44.66 400 2000 7.6E+5 1251.554 3.7E+4  1492.90 CID   cid Positive False False 11 79 + c d Full m..."},
                {1517, "1 1   887 44.69 400 2000 8.0E+8 1147.613 1.0E+7     0.00 CID       Positive False False 11 79 + c ESI Full..."},
                {1518, "2 2   190 44.71 380 2000 4.6E+6 1844.618 2.7E+5  1421.21 CID   cid Positive False False 11 79 + c d Full m..."},
                {1519, "2 3   165 44.74 380 2000 6.0E+6 1842.547 6.9E+5  1419.24 CID   cid Positive False False 11 79 + c d Full m..."},
                {1520, "2 4   210 44.77 265 2000 1.5E+6 1361.745 4.2E+4  1014.93 CID   cid Positive False False 11 79 + c d Full m..."},
                {1521, "1 1   860 44.80 400 2000 6.9E+8 1126.627 2.9E+7     0.00 CID       Positive False False 11 79 + c ESI Full..."}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string> {
                {16121, "1 1 45876 47.68 350 1550 1.9E+9  503.565 3.4E+8     0.00 CID       Positive False True 46 219 FTMS + p NSI..."},
                {16122, "2 2  4124 47.68 106  817 1.6E+6  550.309 2.1E+5   403.22 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16123, "2 2  6484 47.68 143 1627 5.5E+5  506.272 4.9E+4   538.84 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16124, "2 2  8172 47.68 208 2000 7.8E+5  737.530 7.0E+4   776.27 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16125, "2 2  5828 47.68 120 1627 2.1E+5  808.486 2.2E+4   538.84 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16126, "2 2  6228 47.68 120 1627 1.4E+5  536.209 9.0E+3   538.84 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16127, "2 2  7180 47.68 120 1627 1.3E+5  808.487 1.4E+4   538.84 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16128, "2 2  7980 47.69 225 1682 4.4E+5  805.579 2.3E+4   835.88 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16129, "2 2  7700 47.69 266 1986 3.4E+5  938.679 2.9E+4   987.89 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16130, "2 2  5180 47.69 110  853 2.7E+5  411.977 1.2E+4   421.26 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16131, "2 2   436 47.69 120 1986 2.1E+4  984.504 9.5E+3   987.89 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16132, "2 2  2116 47.69 120  853 1.2E+4  421.052 6.8E+2   421.26 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16133, "2 2  2444 47.70 120  853 1.5E+4  421.232 1.2E+3   421.26 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16134, "2 2  2948 47.70 120  853 1.4E+4  838.487 7.5E+2   421.26 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16135, "2 2   508 47.70 120 1986 2.1E+4  984.498 9.2E+3   987.89 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16136, "2 2   948 47.71 120 1986 2.3E+4  984.491 9.4E+3   987.89 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16137, "2 2  9580 47.71 336 2000 3.5E+5 1536.038 4.7E+3  1241.01 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16138, "2 2  7604 47.72 235 1760 2.9E+5  826.095 2.5E+4   874.84 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16139, "2 2   972 47.72 120 1760 1.6E+4  875.506 2.1E+3   874.84 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16140, "2 2  1596 47.72 120 1760 1.8E+4 1749.846 2.0E+3   874.84 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16141, "2 2  2124 47.72 120 1760 1.6E+4  874.664 1.6E+3   874.84 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16142, "1 1 51976 47.73 350 1550 1.3E+9  503.565 1.9E+8     0.00 CID       Positive False True 46 219 FTMS + p NSI..."},
                {16143, "2 2  5412 47.73 128  981 6.5E+5  444.288 6.4E+4   485.28 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16144, "2 2  4300 47.73 101 1561 5.0E+5  591.309 4.0E+4   387.66 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16145, "2 2  6740 47.73 162 1830 4.0E+5  567.912 2.8E+4   606.62 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16146, "2 2  4788 47.73 99  770 1.9E+5  532.308 3.4E+4   379.72 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16147, "2 2  6708 47.74 120 1830 3.8E+5  603.095 3.1E+4   606.62 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16148, "2 2  7260 47.74 120 1830 1.5E+5  603.076 1.3E+4   606.62 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16149, "2 2  9172 47.74 120 1830 1.6E+5  603.027 1.1E+4   606.62 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16150, "2 2  5204 47.74 95 1108 3.8E+5  418.536 1.2E+5   365.88 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16151, "2 2  5636 47.75 146 1656 2.8E+5  501.523 4.3E+4   548.54 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16152, "2 2  9572 47.75 328 2000 1.8E+5  848.497 2.2E+3  1210.30 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16153, "2 2  5004 47.75 120 1656 1.3E+5  548.396 1.3E+4   548.54 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16154, "2 2  4732 47.75 120 1656 4.2E+4  548.450 4.2E+3   548.54 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16155, "2 2  6228 47.76 120 1656 4.2E+4  550.402 3.6E+3   548.54 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16156, "2 2  9164 47.76 324 2000 1.5E+5 1491.872 1.0E+4  1197.57 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16157, "2 2  5916 47.76 124  950 2.2E+5  420.689 2.2E+4   469.71 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16158, "2 2  5740 47.76 306 2000 1.3E+5 1100.042 3.5E+3  1132.02 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16159, "2 2  5540 47.76 122  935 1.9E+5  445.117 2.7E+4   462.15 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16160, "2 2  5756 47.77 145 1646 3.4E+5  539.065 6.0E+4   545.18 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16161, "2 2  6100 47.77 157 1191 2.8E+5  541.462 6.0E+4   590.28 CID   cid Positive False False 46 219 ITMS + c NSI..."},
                {16162, "2 2  2508 47.77 120 1191 8.4E+4 1180.615 5.1E+3   590.28 ETD   etd Positive False False 46 219 ITMS + c NSI..."},
                {16163, "2 2  2644 47.77 120 1191 1.8E+4 1184.614 9.0E+2   590.28 ETD ETciD Positive False False 46 219 ITMS + c NSI..."},
                {16164, "2 2  3180 47.77 120 1191 1.7E+4 1184.644 8.7E+2   590.28 ETD EThcD Positive False False 46 219 ITMS + c NSI..."},
                {16165, "1 1 53252 47.78 350 1550 1.2E+9  503.565 1.6E+8     0.00 CID       Positive False True 46 219 FTMS + p NSI..."},
            };
            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan info for {0}", dataFile.Name);
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18}",
                                  "ScanNumber", "MSLevel", "EventNumber",
                                  "NumPeaks", "RetentionTime", "LowMass", "HighMass", "TotalIonCurrent", "BasePeakMZ",
                                  "BasePeakIntensity", "ParentIonMZ", "ActivationType", "CollisionMode",
                                  "IonMode", "IsCentroided", "IsFTMS", "ScanEvents.Count", "StatusLog.Count", "FilterText");

                var scanCountMS1 = 0;
                var scanCountMS2 = 0;
                var fileDataWarned = false;

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    var scanSummary =
                        string.Format(
                            "{0} {1} {2} {3,5} {4} {5} {6,4} {7} {8,8} {9} {10,8} {11} {12,5} {13} {14} {15} {16} {17} {18}",
                            scanInfo.ScanNumber, scanInfo.MSLevel, scanInfo.EventNumber,
                            scanInfo.NumPeaks, scanInfo.RetentionTime.ToString("0.00"),
                            scanInfo.LowMass.ToString("0"), scanInfo.HighMass.ToString("0"),
                            scanInfo.TotalIonCurrent.ToString("0.0E+0"), scanInfo.BasePeakMZ.ToString("0.000"),
                            scanInfo.BasePeakIntensity.ToString("0.0E+0"), scanInfo.ParentIonMZ.ToString("0.00"),
                            scanInfo.ActivationType, scanInfo.CollisionMode,
                            scanInfo.IonMode, scanInfo.IsCentroided,
                            scanInfo.IsFTMS, scanInfo.ScanEvents.Count, scanInfo.StatusLog.Count,
                            scanInfo.FilterText.Substring(0, 12) + "...");

                    Console.WriteLine(scanSummary);

                    if (scanInfo.MSLevel > 1)
                        scanCountMS2++;
                    else
                        scanCountMS1++;

                    Dictionary<int, string> expectedDataThisFile;
                    if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {

                        string expectedScanSummary;
                        if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                        {
                            Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary, "Scan summary mismatch, scan " + scanNumber);
                        }

                    }
                    else
                    {
                        if (!fileDataWarned)
                        {
                            Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                            fileDataWarned = true;
                        }
                    }

                }

                Console.WriteLine("scanCountMS1={0}", scanCountMS1);
                Console.WriteLine("scanCountMS2={0}", scanCountMS2);

                Assert.AreEqual(expectedMS1, scanCountMS1, "MS1 scan count mismatch");
                Assert.AreEqual(expectedMS2, scanCountMS2, "MS1 scan count mismatch");
            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165)]
        public void TestGetScanData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the scan number of data being retrieved
            var file1Data = new Dictionary<int, Dictionary<string, string>> {
                {1513, new Dictionary<string, string> {} },
                {1514, new Dictionary<string, string> {} },
                {1515, new Dictionary<string, string> {} },
                {1516, new Dictionary<string, string> {} },
                {1517, new Dictionary<string, string> {} }
            
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",  " 851      851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False",  " 109      109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_False",  " 290      290  335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_False",  " 154      154  461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_False",  " 887      887  420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("0_True",   " 851      851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True",   " 109      109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_True",   " 290      290  335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_True",   " 154      154  461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_True",   " 887      887  420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_False", "  50       50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False", "  50       50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_False", "  50       50  353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_False", "  50       50  461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_False", "  50       50  883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_True",  "  50       50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True",  "  50       50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_True",  "  50       50  353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_True",  "  50       50  461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_True",  "  50       50  883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);


            var file2Data = new Dictionary<int, Dictionary<string, string>> {
                {16121, new Dictionary<string, string> {} },
                {16122, new Dictionary<string, string> {} },
                {16126, new Dictionary<string, string> {} },
                {16131, new Dictionary<string, string> {} },
                {16133, new Dictionary<string, string> {} },
                {16141, new Dictionary<string, string> {} }
            
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False", "11888    11888  346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False", "  490      490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_False", "  753      753  231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_False", "   29       29  984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_False", "  280      280  260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_False", "  240      240  304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("0_True", "  833      833  351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True", "  490      490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_True", "  753      753  231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_True", "   29       29  984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_True", "  280      280  260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_True", "  240      240  304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_False", "   50       50  503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50       50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_False", "   50       50  535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_False", "   29       29  984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_False", "   50       50  356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_False", "   50       50  853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_True", "  833      833  351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True", "   50       50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_True", "   50       50  535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_True", "   29       29  984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_True", "   50       50  356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_True", "   50       50  853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");


            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);
            

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan data for {0}", dataFile.Name);
                Console.WriteLine("{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8} {8,-8}  {9}",
                                "Scan", "Max#", "Centroid", "MzCount", "IntCount", 
                                "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");

                var fileDataWarned = false;

                for (var iteration = 1; iteration <= 4; iteration++)
                {
                    int maxNumberOfPeaks;
                    bool centroidData;

                    switch (iteration)
                    {
                        case 1:
                            maxNumberOfPeaks = 0;
                            centroidData = false;
                            break;
                        case 2:
                            maxNumberOfPeaks = 0;
                            centroidData = true;
                            break;
                        case 3:
                            maxNumberOfPeaks = 50;
                            centroidData = false;
                            break;
                        default:
                            maxNumberOfPeaks = 50;
                            centroidData = true;
                            break;
                    }

                    for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                    {

                        double[] mzList;
                        double[] intensityList;

                        var success = reader.GetScanData(scanNumber, out mzList, out intensityList, maxNumberOfPeaks,
                                                         centroidData);

                        var midPoint = (int)(intensityList.Length / 2f);

                        clsScanInfo scanInfo;
                        reader.GetScanInfo(scanNumber, out scanInfo);

                        var scanSummary =
                            string.Format(
                                "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8} {8,8}  {9}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                mzList.Length, intensityList.Length,
                                mzList[0].ToString("0.000"), intensityList[0].ToString("0.0E+0"),
                                mzList[midPoint].ToString("0.000"), intensityList[midPoint].ToString("0.0E+0"),
                                scanInfo.FilterText);

                 
                        Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                        if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                        {

                            Dictionary<string, string> expectedDataByType;
                            if (expectedDataThisFile.TryGetValue(scanNumber, out expectedDataByType))
                            {
                                var keySpec = maxNumberOfPeaks + "_" + centroidData;
                                string expectedDataDetails;
                                if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                                {
                                    Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22), "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
                                }
                            }

                        }
                        else
                        {
                            if (!fileDataWarned)
                            {
                                Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                                fileDataWarned = true;
                            }
                        }

                        Console.WriteLine(scanSummary);
                    }

                }

            }
        }


        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16122)]
        public void TestGetScanData2D(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the scan number of data being retrieved
            var file1Data = new Dictionary<int, Dictionary<string, string>> {
                {1513, new Dictionary<string, string> {} },
                {1514, new Dictionary<string, string> {} }            
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",   " 851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False",   " 109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("0_True",    " 851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True",    " 109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_False",  "  50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False",  "  50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_True",   "  50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True",   "  50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);


            var file2Data = new Dictionary<int, Dictionary<string, string>> {
                {16121, new Dictionary<string, string> {} },
                {16122, new Dictionary<string, string> {} }            
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False",  "11888  346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False",  "  490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("0_True",   "  833  351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True",   "  490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("50_False", "   50  503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("50_True",  "  833  351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True",  "   50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan data for {0}", dataFile.Name);
                Console.WriteLine("{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
                                "Scan", "Max#", "Centroid", "DataCount", 
                                "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");

                var fileDataWarned = false;

                for (var iteration = 1; iteration <= 4; iteration++)
                {
                    int maxNumberOfPeaks;
                    bool centroidData;

                    switch (iteration)
                    {
                        case 1:
                            maxNumberOfPeaks = 0;
                            centroidData = false;
                            break;
                        case 2:
                            maxNumberOfPeaks = 0;
                            centroidData = true;
                            break;
                        case 3:
                            maxNumberOfPeaks = 50;
                            centroidData = false;
                            break;
                        default:
                            maxNumberOfPeaks = 50;
                            centroidData = true;
                            break;
                    }

                    for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                    {

                        double[,] dblMassIntensityPairs;

                        var success = reader.GetScanData2D(scanNumber, out dblMassIntensityPairs, maxNumberOfPeaks, centroidData);

                        clsScanInfo scanInfo;
                        reader.GetScanInfo(scanNumber, out scanInfo);

                        var lastIndex = dblMassIntensityPairs.GetUpperBound(1);
                        int dataCount;

                        if (maxNumberOfPeaks > 0)
                        {

                            if (centroidData && scanInfo.IsFTMS)
                            {
                                // When centroiding FTMS data, the maxNumberOfPeaks value is ignored
                                dataCount = lastIndex + 1;

                                var pointToCheck = maxNumberOfPeaks + (int)((lastIndex - maxNumberOfPeaks) / 2f);

                                Assert.IsTrue(dblMassIntensityPairs[0, pointToCheck] > 50, "m/z value in 2D array is unexpectedly less than 50");
                            }
                            else
                            {
                                dataCount = maxNumberOfPeaks;

                                // Make sure the 2D array has values of 0 for mass and intensity beyond index maxNumberOfPeaks
                                for (var dataIndex = maxNumberOfPeaks; dataIndex < lastIndex; dataIndex++)
                                {
                                    if (dblMassIntensityPairs[0, dataIndex] > 0)
                                    {
                                        Console.WriteLine("Non-zero m/z value found at index " + dataIndex + " for scan " + scanNumber);
                                        Assert.AreEqual(0, dblMassIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                                    }

                                    if (dblMassIntensityPairs[1, dataIndex] > 0)
                                    {
                                        Console.WriteLine("Non-zero intensity value found at index " + dataIndex + " for scan " + scanNumber);
                                        Assert.AreEqual(0, dblMassIntensityPairs[1, dataIndex], "Non-zero intensity value found in 2D array beyond expected index");
                                    }
                                }
                            }
                        }
                        else
                        {
                            dataCount = lastIndex + 1;
                        }

                        var midPoint = (int)(dataCount / 2f);

                        var scanSummary =
                            string.Format(
                                "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                dataCount,
                                dblMassIntensityPairs[0, 0].ToString("0.000"), dblMassIntensityPairs[1,0].ToString("0.0E+0"),
                                dblMassIntensityPairs[0, midPoint].ToString("0.000"), dblMassIntensityPairs[1, midPoint].ToString("0.0E+0"),
                                scanInfo.FilterText);


                        Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                        if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                        {

                            Dictionary<string, string> expectedDataByType;
                            if (expectedDataThisFile.TryGetValue(scanNumber, out expectedDataByType))
                            {
                                var keySpec = maxNumberOfPeaks + "_" + centroidData;
                                string expectedDataDetails;
                                if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                                {
                                    Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22), "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
                                }
                            }

                        }
                        else
                        {
                            if (!fileDataWarned)
                            {
                                Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                                fileDataWarned = true;
                            }
                        }

                        Console.WriteLine(scanSummary);
                    }

                }

            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165)]
        public void TestGetScanDataSumScans(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the start scan for summing
            var file1Data = new Dictionary<int, Dictionary<string, string>> {
                {1513, new Dictionary<string, string> {} }        
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",  "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("0_True",   "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_False", "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_True",  "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, Dictionary<string, string>> {
                {16121, new Dictionary<string, string> {} }
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False",  "26057  346.518   0.0E+0  753.312   8.7E+0  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("0_True",   "  818  351.230   3.2E+5  820.778   2.3E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("50_False", "   50  503.553   1.2E+7  521.201   1.6E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("50_True",  "   50  371.885   2.6E+7  650.717   9.8E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan data for {0}", dataFile.Name);
                Console.WriteLine("{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
                                "Scan", "Max#", "Centroid", "DataCount",
                                "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");

                var fileDataWarned = false;

                for (var iteration = 1; iteration <= 4; iteration++)
                {
                    int maxNumberOfPeaks;
                    bool centroidData;

                    switch (iteration)
                    {
                        case 1:
                            maxNumberOfPeaks = 0;
                            centroidData = false;
                            break;
                        case 2:
                            maxNumberOfPeaks = 0;
                            centroidData = true;
                            break;
                        case 3:
                            maxNumberOfPeaks = 50;
                            centroidData = false;
                            break;
                        default:
                            maxNumberOfPeaks = 50;
                            centroidData = true;
                            break;
                    }

                    double[,] dblMassIntensityPairs;

                    var success = reader.GetScanDataSumScans(scanStart, scanEnd, out dblMassIntensityPairs, maxNumberOfPeaks, centroidData);

                    clsScanInfo scanInfo;
                    reader.GetScanInfo(scanStart, out scanInfo);

                    var lastIndex = dblMassIntensityPairs.GetUpperBound(1);
                    int dataCount;

                    if (maxNumberOfPeaks > 0)
                    {
                        dataCount = maxNumberOfPeaks;

                        // Make sure the 2D array has values of 0 for mass and intensity beyond index maxNumberOfPeaks
                        for (var dataIndex = maxNumberOfPeaks; dataIndex < lastIndex; dataIndex++)
                        {
                            if (dblMassIntensityPairs[0, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero m/z value found at index " + dataIndex + " for scan " + scanStart);
                                Assert.AreEqual(0, dblMassIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                            }

                            if (dblMassIntensityPairs[1, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero intensity value found at index " + dataIndex + " for scan " + scanStart);
                                Assert.AreEqual(0, dblMassIntensityPairs[1, dataIndex], "Non-zero intensity value found in 2D array beyond expected index");
                            }
                        }
                    }
                    else
                    {
                        dataCount = lastIndex + 1;
                    }

                    var midPoint = (int)(dataCount / 2f);

                    var scanSummary =
                        string.Format(
                            "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                            scanStart, maxNumberOfPeaks, centroidData,
                            dataCount,
                            dblMassIntensityPairs[0, 0].ToString("0.000"), dblMassIntensityPairs[1, 0].ToString("0.0E+0"),
                            dblMassIntensityPairs[0, midPoint].ToString("0.000"), dblMassIntensityPairs[1, midPoint].ToString("0.0E+0"),
                            scanInfo.FilterText);


                    Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                    if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {

                        Dictionary<string, string> expectedDataByType;
                        if (expectedDataThisFile.TryGetValue(scanStart, out expectedDataByType))
                        {
                            var keySpec = maxNumberOfPeaks + "_" + centroidData;
                            string expectedDataDetails;
                            if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                            {
                                Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22), "Scan details mismatch, scan " + scanStart + ", keySpec " + keySpec);
                            }
                        }

                    }
                    else
                    {
                        if (!fileDataWarned)
                        {
                            Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                            fileDataWarned = true;
                        }
                    }

                    Console.WriteLine(scanSummary);
                }


            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanLabelData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            var noMatch = "  0                                                        ";

            var file1Data = new Dictionary<int, string> {
                {1513, noMatch + "+ c ESI Full ms [400.00-2000.00]"},
                {1514, noMatch + "+ c d Full ms2 884.41@cid45.00 [230.00-1780.00]"}               
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string> {
                {16121, "833  712.813   2.9E+5 22100.000 5705.175 159099.000        0  FTMS + p NSI Full ms [350.0000-1550.0000]"},
                {16122, noMatch + "ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]"},
                {16123, noMatch + "ITMS + c NSI r d Full ms2 538.8400@cid30.00 [143.0000-1627.0000]"},
                {16124, noMatch + "ITMS + c NSI r d Full ms2 776.2740@cid30.00 [208.0000-2000.0000]"},
                {16125, noMatch + "ITMS + c NSI r d Full ms2 538.8400@etd53.58 [120.0000-1627.0000]"},
                {16126, noMatch + "ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]"},
                {16127, noMatch + "ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@hcd20.00 [120.0000-1627.0000]"},
                {16128, noMatch + "ITMS + c NSI r d Full ms2 835.8777@cid30.00 [225.0000-1682.0000]"},
                {16129, noMatch + "ITMS + c NSI r d Full ms2 987.8934@cid30.00 [266.0000-1986.0000]"},
                {16130, noMatch + "ITMS + c NSI r d Full ms2 421.2619@cid30.00 [110.0000-853.0000]"},
                {16131, noMatch + "ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]"},
                {16132, noMatch + "ITMS + c NSI r d Full ms2 421.2619@etd120.55 [120.0000-853.0000]"},
                {16133, noMatch + "ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]"},
                {16134, noMatch + "ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@hcd20.00 [120.0000-853.0000]"},
                {16135, noMatch + "ITMS + c NSI r d sa Full ms2 987.8934@etd120.55@cid20.00 [120.0000-1986.0000]"},
                {16136, noMatch + "ITMS + c NSI r d sa Full ms2 987.8934@etd120.55@hcd20.00 [120.0000-1986.0000]"},
                {16137, noMatch + "ITMS + c NSI r d Full ms2 1241.0092@cid30.00 [336.0000-2000.0000]"},
                {16138, noMatch + "ITMS + c NSI r d Full ms2 874.8397@cid30.00 [235.0000-1760.0000]"},
                {16139, noMatch + "ITMS + c NSI r d Full ms2 874.8397@etd120.55 [120.0000-1760.0000]"},
                {16140, noMatch + "ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@cid20.00 [120.0000-1760.0000]"},
                {16141, noMatch + "ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]"},
                {16142, "928  740.322   2.1E+5 28700.000 3257.063 93482.960        0  FTMS + p NSI Full ms [350.0000-1550.0000]"}
            };
            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan label data for {0}", dataFile.Name);
                Console.WriteLine("{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
                                  "Scan", "Count", "Mass", "Intensity",
                                  "Resolution", "Baseline", "Noise", "Charge", "ScanFilter");

                var fileDataWarned = false;

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {

                    // List of mass, intensity, resolution, baseline intensity, noise floor, and charge for each data point
                    XRawFileIO.udtFTLabelInfoType[] ftLabelData;

                    var success = reader.GetScanLabelData(scanNumber, out ftLabelData);

                    clsScanInfo scanInfo;
                    reader.GetScanInfo(scanNumber, out scanInfo);

                    string scanSummary;

                    if (ftLabelData.Length == 0)
                    {
                        scanSummary = string.Format("{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                                scanNumber, ftLabelData.Length,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                scanInfo.FilterText);

                    }
                    else
                    {
                        var midPoint = (int)(ftLabelData.Length / 2f);

                        scanSummary = string.Format("{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                                scanNumber, ftLabelData.Length,
                                ftLabelData[midPoint].Mass.ToString("0.000"),
                                ftLabelData[midPoint].Intensity.ToString("0.0E+0"),
                                ftLabelData[midPoint].Resolution.ToString("0.000"),
                                ftLabelData[midPoint].Baseline.ToString("0.000"),
                                ftLabelData[midPoint].Noise.ToString("0.000"),
                                ftLabelData[midPoint].Charge.ToString("0"),
                                scanInfo.FilterText);
                    }

                    Dictionary<int, string> expectedDataThisFile;
                    if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {

                        string expectedScanSummary;
                        if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                        {
                            Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary, "Scan summary mismatch, scan " + scanNumber);
                        }

                    }
                    else
                    {
                        if (!fileDataWarned)
                        {
                            Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                            fileDataWarned = true;
                        }
                    }

                    Console.WriteLine(scanSummary);
                }
            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanPrecisionData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            var noMatch = "  0                                               ";

            var file1Data = new Dictionary<int, string> {
                {1513, noMatch + "+ c ESI Full ms [400.00-2000.00]"},
                {1514, noMatch + "+ c d Full ms2 884.41@cid45.00 [230.00-1780.00]"}               
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string> {
                {16121, "833  712.813   2.9E+5 22100.000    2.138    3.000  FTMS + p NSI Full ms [350.0000-1550.0000]"},
                {16122, noMatch + "ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]"},
                {16123, noMatch + "ITMS + c NSI r d Full ms2 538.8400@cid30.00 [143.0000-1627.0000]"},
                {16124, noMatch + "ITMS + c NSI r d Full ms2 776.2740@cid30.00 [208.0000-2000.0000]"},
                {16125, noMatch + "ITMS + c NSI r d Full ms2 538.8400@etd53.58 [120.0000-1627.0000]"},
                {16126, noMatch + "ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]"},
                {16127, noMatch + "ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@hcd20.00 [120.0000-1627.0000]"},
                {16128, noMatch + "ITMS + c NSI r d Full ms2 835.8777@cid30.00 [225.0000-1682.0000]"},
                {16129, noMatch + "ITMS + c NSI r d Full ms2 987.8934@cid30.00 [266.0000-1986.0000]"},
                {16130, noMatch + "ITMS + c NSI r d Full ms2 421.2619@cid30.00 [110.0000-853.0000]"},
                {16131, noMatch + "ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]"},
                {16132, noMatch + "ITMS + c NSI r d Full ms2 421.2619@etd120.55 [120.0000-853.0000]"},
                {16133, noMatch + "ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]"},
                {16134, noMatch + "ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@hcd20.00 [120.0000-853.0000]"},
                {16135, noMatch + "ITMS + c NSI r d sa Full ms2 987.8934@etd120.55@cid20.00 [120.0000-1986.0000]"},
                {16136, noMatch + "ITMS + c NSI r d sa Full ms2 987.8934@etd120.55@hcd20.00 [120.0000-1986.0000]"},
                {16137, noMatch + "ITMS + c NSI r d Full ms2 1241.0092@cid30.00 [336.0000-2000.0000]"},
                {16138, noMatch + "ITMS + c NSI r d Full ms2 874.8397@cid30.00 [235.0000-1760.0000]"},
                {16139, noMatch + "ITMS + c NSI r d Full ms2 874.8397@etd120.55 [120.0000-1760.0000]"},
                {16140, noMatch + "ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@cid20.00 [120.0000-1760.0000]"},
                {16141, noMatch + "ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]"},
                {16142, "928  740.322   2.1E+5 28700.000    2.221    3.000  FTMS + p NSI Full ms [350.0000-1550.0000]"}
            };
            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan label data for {0}", dataFile.Name);
                Console.WriteLine("{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8}  {7}",
                                  "Scan", "Count", "Mass", "Intensity",
                                  "Resolution", "AccuracyMMU", "AccuracyPPM", "ScanFilter");

                var fileDataWarned = false;

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {

                    // List of Intensity, Mass, AccuracyMMU, AccuracyPPM, and Resolution for each data point
                    XRawFileIO.udtMassPrecisionInfoType[] massResolutionData;

                    var success = reader.GetScanPrecisionData(scanNumber, out massResolutionData);

                    clsScanInfo scanInfo;
                    reader.GetScanInfo(scanNumber, out scanInfo);

                    string scanSummary;

                    if (massResolutionData.Length == 0)
                    {
                        scanSummary = string.Format("{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8}  {7}",
                                scanNumber, massResolutionData.Length,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                scanInfo.FilterText);

                    }
                    else
                    {
                        var midPoint = (int)(massResolutionData.Length / 2f);

                        scanSummary = string.Format("{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8}  {7}",
                                scanNumber, massResolutionData.Length,
                                massResolutionData[midPoint].Mass.ToString("0.000"),
                                massResolutionData[midPoint].Intensity.ToString("0.0E+0"),
                                massResolutionData[midPoint].Resolution.ToString("0.000"),
                                massResolutionData[midPoint].AccuracyMMU.ToString("0.000"),
                                massResolutionData[midPoint].AccuracyPPM.ToString("0.000"),                               
                                scanInfo.FilterText);
                    }

                    Dictionary<int, string> expectedDataThisFile;
                    if (expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {

                        string expectedScanSummary;
                        if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                        {
                            Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary, "Scan summary mismatch, scan " + scanNumber);
                        }

                    }
                    else
                    {
                        if (!fileDataWarned)
                        {
                            Console.WriteLine("Warning: not validating scan results for " + dataFile.Name);
                            fileDataWarned = true;
                        }
                    }

                    Console.WriteLine(scanSummary);
                }
            }
        }


        private FileInfo GetRawDataFile(string rawFileName)
        {
            FileInfo dataFile;

            if (USE_REMOTE_PATHS)
            {
                dataFile = new FileInfo(Path.Combine(@"\\proto-2\UnitTest_Files\ThermoRawFileReader", rawFileName));
            }
            else
            {
                dataFile = new FileInfo(Path.Combine(@"..\..\..\Test_ThermoRawFileReader\bin", rawFileName));
            }


            if (!dataFile.Exists)
            {
                Assert.Fail("File not found: " + dataFile.FullName);
            }

            return dataFile;
        }
    }
}
