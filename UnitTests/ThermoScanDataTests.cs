using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ThermoRawFileReader;

namespace RawFileReaderTests
{
    [TestFixture]
    public class ThermoScanDataTests
    {
        private const bool USE_REMOTE_PATHS = true;

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw")]
        [TestCase(@"HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw")]
        public void TestGetCollisionEnergy(string rawFileName)
        {
            // Keys in this Dictionary are filename, values are Collision Energies by scan
            var expectedData = new Dictionary<string, Dictionary<int, List<double>>>();

            var ce30 = new List<double> { 30.00 };
            var ce45 = new List<double> { 45.00 };
            var ce20_120 = new List<double> { 20.00, 120.550003 };
            var ce120 = new List<double> { 120.550003 };
            var ms1Scan = new List<double>();

            // Keys in this dictionary are scan number and values are collision energies
            var file1Data = new Dictionary<int, List<double>> {
                {2250, ce45},
                {2251, ce45},
                {2252, ce45},
                {2253, ms1Scan},
                {2254, ce45},
                {2255, ce45},
                {2256, ce45},
                {2257, ms1Scan},
                {2258, ce45},
                {2259, ce45},
                {2260, ce45}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, List<double>> {
                {39000, ce30},
                {39001, ce30},
                {39002, ms1Scan},
                {39003, ce30},
                {39004, ce30},
                {39005, ce30},
                {39006, ce120},
                {39007, ce20_120},
                {39008, ce20_120},
                {39009, ce30},
                {39010, ce30}
            };
            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);

            var file3Data = new Dictionary<int, List<double>> {
                {19000, ce120},
                {19001, ce20_120},
                {19002, ce20_120},
                {19003, ms1Scan},
                {19004, ce30},
                {19005, ce30},
                {19006, ce30},
                {19007, ce120},
                {19008, ce20_120},
                {19009, ce20_120},
                {19010, ce30}
            };
            expectedData.Add("HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53", file3Data);

            var dataFile = GetRawDataFile(rawFileName);

            Dictionary<int, List<double>> collisionEnergiesThisFile;
            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out collisionEnergiesThisFile))
            {
                Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
            }

            var collisionEnergiesActual = new Dictionary<int, List<double>>();
            var msLevelsActual = new Dictionary<int, int>();

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                foreach(var scanNumber in collisionEnergiesThisFile.Keys)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    var collisionEnergiesThisScan = reader.GetCollisionEnergy(scanNumber);
                    collisionEnergiesActual.Add(scanNumber, collisionEnergiesThisScan);

                    msLevelsActual.Add(scanNumber, scanInfo.MSLevel);
                }

                Console.WriteLine("{0,-5} {1,-5} {2}", "Valid", "Scan", "Collision Energy");

                foreach (var actualEnergiesOneScan in (from item in collisionEnergiesActual orderby item.Key select item))
                {
                    var scanNumber = actualEnergiesOneScan.Key;

                    var expectedEnergies = collisionEnergiesThisFile[scanNumber];

                    if (actualEnergiesOneScan.Value.Count == 0)
                    {
                        var msLevel = msLevelsActual[scanNumber];

                        Assert.AreEqual(msLevel, 1,
                                        "Scan {0} has no collision energies; should be msLevel 1 but is {1}",
                                        scanNumber, msLevel);

                        Console.WriteLine("{0,-5} {1,-5} {2}", true, scanNumber, "MS1 scan");
                    }
                    else
                    {
                        foreach (var actualEnergy in actualEnergiesOneScan.Value)
                        {
                            var isValid = expectedEnergies.Any(expectedEnergy => Math.Abs(actualEnergy - expectedEnergy) < 0.00001);

                            Console.WriteLine("{0,-5} {1,-5} {2}", isValid, scanNumber, actualEnergy.ToString("0.00"));

                            Assert.IsTrue(isValid, "Unexpected collision energy {0} for scan {1}", actualEnergy.ToString("0.00"), scanNumber);
                        }
                    }

                    Assert.AreEqual(expectedEnergies.Count, actualEnergiesOneScan.Value.Count, 
                                    "Collision energy count mismatch for scan {0}", scanNumber);
                    
                }

            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 3316)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 71147)]
        public void TestGetNumScans(string rawFileName, int expectedResult)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                var scanCount = reader.GetNumScans();

                Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);
                Assert.AreEqual(expectedResult, scanCount, "Scan count mismatch");
            }
        }

        [Test]
        [TestCase(@"B5_50uM_MS_r1.RAW", 1, 20, 20, 0)]
        [TestCase(@"MNSLTFKK_ms.raw", 1, 88, 88, 0)]
        [TestCase(@"QCShew200uL.raw", 4000, 4100, 101, 0)]
        [TestCase(@"Wrighton_MT2_SPE_200avg_240k_neg_330-380.raw", 1, 200, 200, 0)]
        [TestCase(@"1229_02blk1.raw", 6000, 6100, 77, 24)]
        [TestCase(@"MCF7_histone_32_49B_400min_HCD_ETD_01172014_b.raw", 2300, 2400, 18, 83)]
        [TestCase(@"lowdose_IMAC_iTRAQ1_PQDMSA.raw", 15000, 15100, 16, 85)]
        [TestCase(@"MZ20150721blank2.raw", 1, 434, 62, 372)]
        [TestCase(@"OG_CEPC_PU_22Oct13_Legolas_13-05-12.raw", 5000, 5100, 9, 92)]
        [TestCase(@"blank_MeOH-3_18May16_Rainier_Thermo_10344958.raw", 1500, 1900, 190, 211)]
        [TestCase(@"HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw", 25200, 25600, 20, 381)]
        [TestCase(@"MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 5900, 6000, 8, 93)]
        [TestCase(@"IPA-blank-07_25Oct13_Gimli.raw", 1750, 1850, 101, 0)]
        public void TestGetScanCountsByScanType(string rawFileName, int scanStart, int scanEnd, int expectedMS1, int expectedMS2)
        {
            // Keys in this Dictionary are filename, values are ScanCounts by collision mode, where the key is a Tuple of ScanType and FilterString
            var expectedData = new Dictionary<string, Dictionary<Tuple<string, string>, int>>();

            // Keys in this dictionary are scan type, values are a Dictionary of FilterString and the number of scans with that filter string
            AddExpectedTupleAndCount(expectedData, "B5_50uM_MS_r1", "Q1MS", "+ c NSI Q1MS", 20);

            AddExpectedTupleAndCount(expectedData, "MNSLTFKK_ms", "Q1MS", "+ p NSI Q1MS", 88);

            AddExpectedTupleAndCount(expectedData, "QCShew200uL", "Q3MS", "+ c NSI Q3MS", 101);

            AddExpectedTupleAndCount(expectedData, "Wrighton_MT2_SPE_200avg_240k_neg_330-380", "SIM ms", "FTMS - p NSI SIM ms", 200);

            const string file5 = "1229_02blk1";
            AddExpectedTupleAndCount(expectedData, file5, "MS", "ITMS + c NSI Full ms", 8);
            AddExpectedTupleAndCount(expectedData, file5, "CID-MSn", "ITMS + c NSI d Full ms2 0@cid35.00", 24);
            AddExpectedTupleAndCount(expectedData, file5, "SIM ms", "ITMS + p NSI SIM ms", 69);

            const string file6 = "MCF7_histone_32_49B_400min_HCD_ETD_01172014_b";
            AddExpectedTupleAndCount(expectedData, file6, "HMS", "FTMS + p NSI Full ms", 9);
            AddExpectedTupleAndCount(expectedData, file6, "ETD-HMSn", "FTMS + p NSI d Full ms2 0@etd25.00", 46);
            AddExpectedTupleAndCount(expectedData, file6, "HCD-HMSn", "FTMS + p NSI d Full ms2 0@hcd28.00", 37);
            AddExpectedTupleAndCount(expectedData, file6, "SIM ms", "FTMS + p NSI d SIM ms", 9);

            const string file7 = "lowdose_IMAC_iTRAQ1_PQDMSA";
            AddExpectedTupleAndCount(expectedData, file7, "HMS", "FTMS + p NSI Full ms", 16);
            AddExpectedTupleAndCount(expectedData, file7, "CID-MSn", "ITMS + c NSI d Full ms2 0@cid35.00", 43);
            AddExpectedTupleAndCount(expectedData, file7, "PQD-MSn", "ITMS + c NSI d Full ms2 0@pqd22.00", 42);

            const string file8 = "MZ20150721blank2";
            AddExpectedTupleAndCount(expectedData, file8, "HMS", "FTMS + p NSI Full ms", 62);
            AddExpectedTupleAndCount(expectedData, file8, "ETD-HMSn", "FTMS + p NSI d Full ms2 0@etd20.00", 186);
            AddExpectedTupleAndCount(expectedData, file8, "ETD-HMSn", "FTMS + p NSI d Full ms2 0@etd25.00", 186);

            const string file9 = "OG_CEPC_PU_22Oct13_Legolas_13-05-12";
            AddExpectedTupleAndCount(expectedData, file9, "HMS", "FTMS + p NSI Full ms", 9);
            AddExpectedTupleAndCount(expectedData, file9, "CID-MSn", "ITMS + c NSI d Full ms2 0@cid35.00", 46);
            AddExpectedTupleAndCount(expectedData, file9, "ETD-MSn", "ITMS + c NSI d Full ms2 0@etd1000.00", 1);
            AddExpectedTupleAndCount(expectedData, file9, "ETD-MSn", "ITMS + c NSI d Full ms2 0@etd333.33", 1);
            AddExpectedTupleAndCount(expectedData, file9, "ETD-MSn", "ITMS + c NSI d Full ms2 0@etd400.00", 8);
            AddExpectedTupleAndCount(expectedData, file9, "ETD-MSn", "ITMS + c NSI d Full ms2 0@etd500.00", 8);
            AddExpectedTupleAndCount(expectedData, file9, "ETD-MSn", "ITMS + c NSI d Full ms2 0@etd666.67", 56);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd1000.00", 5);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd285.71", 1);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd333.33", 1);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd400.00", 14);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd500.00", 32);
            AddExpectedTupleAndCount(expectedData, file9, "SA_ETD-MSn", "ITMS + c NSI d sa Full ms2 0@etd666.67", 260);

            const string file10 = "blank_MeOH-3_18May16_Rainier_Thermo_10344958";
            AddExpectedTupleAndCount(expectedData, file10, "HMS", "FTMS - p ESI Full ms", 190);
            AddExpectedTupleAndCount(expectedData, file10, "CID-HMSn", "FTMS - c ESI d Full ms2 0@cid35.00", 207);
            AddExpectedTupleAndCount(expectedData, file10, "CID-HMSn", "FTMS - c ESI d Full ms3 0@cid35.00 0@cid35.00", 4);

            const string file11 = "HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53";
            AddExpectedTupleAndCount(expectedData, file11, "HMS", "FTMS + p NSI Full ms", 20);
            AddExpectedTupleAndCount(expectedData, file11, "CID-MSn", "ITMS + c NSI r d Full ms2 0@cid30.00", 231);
            AddExpectedTupleAndCount(expectedData, file11, "ETciD-MSn", "ITMS + c NSI r d sa Full ms2 0@etd120.55@cid20.00", 46);
            AddExpectedTupleAndCount(expectedData, file11, "ETciD-MSn", "ITMS + c NSI r d sa Full ms2 0@etd53.58@cid20.00", 4);
            AddExpectedTupleAndCount(expectedData, file11, "ETD-MSn", "ITMS + c NSI r d Full ms2 0@etd120.55", 46);
            AddExpectedTupleAndCount(expectedData, file11, "ETD-MSn", "ITMS + c NSI r d Full ms2 0@etd53.58", 4);
            AddExpectedTupleAndCount(expectedData, file11, "EThcD-MSn", "ITMS + c NSI r d sa Full ms2 0@etd120.55@hcd20.00", 46);
            AddExpectedTupleAndCount(expectedData, file11, "EThcD-MSn", "ITMS + c NSI r d sa Full ms2 0@etd53.58@hcd20.00", 4);

            const string file12 = "MeOHBlank03POS_11May16_Legolas_HSS-T3_A925";
            AddExpectedTupleAndCount(expectedData, file12, "HMS", "FTMS + p ESI Full ms", 8);
            AddExpectedTupleAndCount(expectedData, file12, "CID-MSn", "ITMS + c ESI d Full ms2 0@cid35.00", 47);
            AddExpectedTupleAndCount(expectedData, file12, "HCD-HMSn", "FTMS + c ESI d Full ms2 0@hcd30.00", 38);
            AddExpectedTupleAndCount(expectedData, file12, "HCD-HMSn", "FTMS + c ESI d Full ms2 0@hcd35.00", 8);

            AddExpectedTupleAndCount(expectedData, "IPA-blank-07_25Oct13_Gimli", "Zoom-MS", "ITMS + p NSI Z ms", 101);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Parsing scan headers for {0}", dataFile.Name);

                var scanCountMS1 = 0;
                var scanCountMS2 = 0;
                var scanTypeCountsActual = new Dictionary<Tuple<string, string>, int>();

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    var scanType = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(scanInfo.FilterText);
                    var genericScanFilter = XRawFileIO.MakeGenericFinniganScanFilter(scanInfo.FilterText);

                    var scanTypeKey = new Tuple<string, string>(scanType, genericScanFilter);

                    int observedScanCount;
                    if (scanTypeCountsActual.TryGetValue(scanTypeKey, out observedScanCount))
                    {
                        scanTypeCountsActual[scanTypeKey] = observedScanCount + 1;
                    }
                    else
                    {
                        scanTypeCountsActual.Add(scanTypeKey, 1);
                    }

                    if (scanInfo.MSLevel > 1)
                        scanCountMS2++;
                    else
                        scanCountMS1++;

                }

                Console.WriteLine("scanCountMS1={0}", scanCountMS1);
                Console.WriteLine("scanCountMS2={0}", scanCountMS2);

                Assert.AreEqual(expectedMS1, scanCountMS1, "MS1 scan count mismatch");
                Assert.AreEqual(expectedMS2, scanCountMS2, "MS2 scan count mismatch");

                Dictionary<Tuple<string, string>, int> expectedScanInfo;
                if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedScanInfo))
                {
                    Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                }
                
                Console.WriteLine("{0,-5} {1,5} {2}", "Valid", "Count", "ScanType");

                foreach (var scanType in (from item in scanTypeCountsActual orderby item.Key select item))
                {
                    int expectedScanCount;
                    if (expectedScanInfo.TryGetValue(scanType.Key, out expectedScanCount))
                    {
                        var isValid = scanType.Value == expectedScanCount;

                        Console.WriteLine("{0,-5} {1,5} {2}", isValid, scanType.Value, scanType.Key);

                        Assert.AreEqual(expectedScanCount, scanType.Value, "Scan type count mismatch");
                    }
                    else
                    {
                        Console.WriteLine("Unexpected scan type found: {0}", scanType.Key);
                        Assert.Fail("Unexpected scan type found: {0}", scanType.Key);
                    }
                }
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
                {1513, "1 1   851 44.57 400 2000 6.3E+8 1089.978 1.2E+7     0.00 CID       Positive True False 11 79 + c ESI Full..."},
                {1514, "2 2   109 44.60 230 1780 5.0E+6  528.128 7.2E+5   884.41 CID   cid Positive True False 11 79 + c d Full m..."},
                {1515, "2 3   290 44.63 305 2000 2.6E+7 1327.414 6.0E+6  1147.67 CID   cid Positive True False 11 79 + c d Full m..."},
                {1516, "2 4   154 44.66 400 2000 7.6E+5 1251.554 3.7E+4  1492.90 CID   cid Positive True False 11 79 + c d Full m..."},
                {1517, "1 1   887 44.69 400 2000 8.0E+8 1147.613 1.0E+7     0.00 CID       Positive True False 11 79 + c ESI Full..."},
                {1518, "2 2   190 44.71 380 2000 4.6E+6 1844.618 2.7E+5  1421.21 CID   cid Positive True False 11 79 + c d Full m..."},
                {1519, "2 3   165 44.74 380 2000 6.0E+6 1842.547 6.9E+5  1419.24 CID   cid Positive True False 11 79 + c d Full m..."},
                {1520, "2 4   210 44.77 265 2000 1.5E+6 1361.745 4.2E+4  1014.93 CID   cid Positive True False 11 79 + c d Full m..."},
                {1521, "1 1   860 44.80 400 2000 6.9E+8 1126.627 2.9E+7     0.00 CID       Positive True False 11 79 + c ESI Full..."}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string> {
                {16121, "1 1 45876 47.68 350 1550 1.9E+9  503.565 3.4E+8     0.00 CID       Positive False True 46 219 FTMS + p NSI..."},
                {16122, "2 2  4124 47.68 106  817 1.6E+6  550.309 2.1E+5   403.22 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16123, "2 2  6484 47.68 143 1627 5.5E+5  506.272 4.9E+4   538.84 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16124, "2 2  8172 47.68 208 2000 7.8E+5  737.530 7.0E+4   776.27 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16125, "2 2  5828 47.68 120 1627 2.1E+5  808.486 2.2E+4   538.84 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16126, "2 2  6228 47.68 120 1627 1.4E+5  536.209 9.0E+3   538.84 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16127, "2 2  7180 47.68 120 1627 1.3E+5  808.487 1.4E+4   538.84 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16128, "2 2  7980 47.69 225 1682 4.4E+5  805.579 2.3E+4   835.88 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16129, "2 2  7700 47.69 266 1986 3.4E+5  938.679 2.9E+4   987.89 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16130, "2 2  5180 47.69 110  853 2.7E+5  411.977 1.2E+4   421.26 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16131, "2 2   436 47.69 120 1986 2.1E+4  984.504 9.5E+3   987.89 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16132, "2 2  2116 47.69 120  853 1.2E+4  421.052 6.8E+2   421.26 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16133, "2 2  2444 47.70 120  853 1.5E+4  421.232 1.2E+3   421.26 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16134, "2 2  2948 47.70 120  853 1.4E+4  838.487 7.5E+2   421.26 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16135, "2 2   508 47.70 120 1986 2.1E+4  984.498 9.2E+3   987.89 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16136, "2 2   948 47.71 120 1986 2.3E+4  984.491 9.4E+3   987.89 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16137, "2 2  9580 47.71 336 2000 3.5E+5 1536.038 4.7E+3  1241.01 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16138, "2 2  7604 47.72 235 1760 2.9E+5  826.095 2.5E+4   874.84 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16139, "2 2   972 47.72 120 1760 1.6E+4  875.506 2.1E+3   874.84 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16140, "2 2  1596 47.72 120 1760 1.8E+4 1749.846 2.0E+3   874.84 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16141, "2 2  2124 47.72 120 1760 1.6E+4  874.664 1.6E+3   874.84 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16142, "1 1 51976 47.73 350 1550 1.3E+9  503.565 1.9E+8     0.00 CID       Positive False True 46 219 FTMS + p NSI..."},
                {16143, "2 2  5412 47.73 128  981 6.5E+5  444.288 6.4E+4   485.28 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16144, "2 2  4300 47.73 101 1561 5.0E+5  591.309 4.0E+4   387.66 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16145, "2 2  6740 47.73 162 1830 4.0E+5  567.912 2.8E+4   606.62 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16146, "2 2  4788 47.73 99  770 1.9E+5  532.308 3.4E+4   379.72 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16147, "2 2  6708 47.74 120 1830 3.8E+5  603.095 3.1E+4   606.62 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16148, "2 2  7260 47.74 120 1830 1.5E+5  603.076 1.3E+4   606.62 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16149, "2 2  9172 47.74 120 1830 1.6E+5  603.027 1.1E+4   606.62 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16150, "2 2  5204 47.74 95 1108 3.8E+5  418.536 1.2E+5   365.88 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16151, "2 2  5636 47.75 146 1656 2.8E+5  501.523 4.3E+4   548.54 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16152, "2 2  9572 47.75 328 2000 1.8E+5  848.497 2.2E+3  1210.30 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16153, "2 2  5004 47.75 120 1656 1.3E+5  548.396 1.3E+4   548.54 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16154, "2 2  4732 47.75 120 1656 4.2E+4  548.450 4.2E+3   548.54 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16155, "2 2  6228 47.76 120 1656 4.2E+4  550.402 3.6E+3   548.54 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
                {16156, "2 2  9164 47.76 324 2000 1.5E+5 1491.872 1.0E+4  1197.57 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16157, "2 2  5916 47.76 124  950 2.2E+5  420.689 2.2E+4   469.71 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16158, "2 2  5740 47.76 306 2000 1.3E+5 1100.042 3.5E+3  1132.02 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16159, "2 2  5540 47.76 122  935 1.9E+5  445.117 2.7E+4   462.15 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16160, "2 2  5756 47.77 145 1646 3.4E+5  539.065 6.0E+4   545.18 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16161, "2 2  6100 47.77 157 1191 2.8E+5  541.462 6.0E+4   590.28 CID   cid Positive True False 46 219 ITMS + c NSI..."},
                {16162, "2 2  2508 47.77 120 1191 8.4E+4 1180.615 5.1E+3   590.28 ETD   etd Positive True False 46 219 ITMS + c NSI..."},
                {16163, "2 2  2644 47.77 120 1191 1.8E+4 1184.614 9.0E+2   590.28 ETD ETciD Positive True False 46 219 ITMS + c NSI..."},
                {16164, "2 2  3180 47.77 120 1191 1.7E+4 1184.644 8.7E+2   590.28 ETD EThcD Positive True False 46 219 ITMS + c NSI..."},
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

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

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
                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }
                    
                    string expectedScanSummary;
                    if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                    {
                        Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary,
                                        "Scan summary mismatch, scan " + scanNumber);
                    }
                }

                Console.WriteLine("scanCountMS1={0}", scanCountMS1);
                Console.WriteLine("scanCountMS2={0}", scanCountMS2);

                Assert.AreEqual(expectedMS1, scanCountMS1, "MS1 scan count mismatch");
                Assert.AreEqual(expectedMS2, scanCountMS2, "MS2 scan count mismatch");
            }
        }

        [Test]
        [TestCase(@"B5_50uM_MS_r1.RAW", 1, 20)]
        [TestCase(@"MNSLTFKK_ms.raw", 1, 88)]
        [TestCase(@"QCShew200uL.raw", 4000, 4100)]
        public void TestGetScanInfoMRM(string rawFileName, int scanStart, int scanEnd)
        {
            // Keys in this Dictionary are filename, values are the expected SIM scan counts
            var expectedData = new Dictionary<string, KeyValuePair<string, int>>
            {
                {"B5_50uM_MS_r1", new KeyValuePair<string, int>("200.0_600.0_1000.0", 20)},
                {"MNSLTFKK_ms", new KeyValuePair<string, int>("200.1_700.0_1200.0", 88)},
                {"QCShew200uL", new KeyValuePair<string, int>("400.0_900.0_1400.0", 101)}
            };

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Examining MRM details in {0}", dataFile.Name);

                var mrmRangeCountsActual = new Dictionary<string, int>();

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    foreach (var mrmRange in scanInfo.MRMInfo.MRMMassList)
                    {
                        var mrmRangeKey = 
                            mrmRange.StartMass.ToString("0.0") + "_" + 
                            mrmRange.CentralMass.ToString("0.0") + "_" + 
                            mrmRange.EndMass.ToString("0.0");

                        int observedScanCount;
                        if (mrmRangeCountsActual.TryGetValue(mrmRangeKey, out observedScanCount))
                        {
                            mrmRangeCountsActual[mrmRangeKey] = observedScanCount + 1;
                        }
                        else
                        {
                            mrmRangeCountsActual.Add(mrmRangeKey, 1);
                        }
                    }

                    Assert.IsTrue(mrmRangeCountsActual.Count == 1, "Found {0} MRM scan ranges; espected to only find 1", mrmRangeCountsActual.Count);
                }

                KeyValuePair<string, int> expectedMRMInfo;
                if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedMRMInfo))
                {
                    Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                }
                
                Console.WriteLine("{0,-5} {1,-5} {2}", "Valid", "Count", "MRMScanRange");

                var mrmRangeActual = mrmRangeCountsActual.First();

                if (expectedMRMInfo.Key == mrmRangeActual.Key)
                {
                    var isValid = mrmRangeActual.Value == expectedMRMInfo.Value;

                    Console.WriteLine("{0,-5} {1,5} {2}", isValid, mrmRangeActual.Value, mrmRangeActual.Key);

                    Assert.AreEqual(expectedMRMInfo.Value, mrmRangeActual.Value, "Scan type count mismatch");
                }
                else
                {
                    Console.WriteLine("Unexpected MRM scan range found: {0}", mrmRangeActual.Key);
                    Assert.Fail("Unexpected MRM scan range found: {0}", mrmRangeActual.Key);
                }
                
            }
        }
        
        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        public void TestGetScanInfoStruct(string rawFileName, int scanStart, int scanEnd)
        {

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

                Console.WriteLine("Checking GetScanInfo initializing from a struct using {0}", dataFile.Name);

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    FinniganFileReaderBaseClass.udtScanHeaderInfoType scanInfoStruct;
#pragma warning disable 618
                    success = reader.GetScanInfo(scanNumber, out scanInfoStruct);
#pragma warning restore 618
                    Assert.IsTrue(success, "GetScanInfo (struct) returned false for scan " + scanNumber);

                    Assert.AreEqual(scanInfoStruct.MSLevel, scanInfo.MSLevel);
                    Assert.AreEqual(scanInfoStruct.IsCentroidScan, scanInfo.IsCentroided);
                    Assert.AreEqual(scanInfoStruct.FilterText, scanInfo.FilterText);
                    Assert.AreEqual(scanInfoStruct.BasePeakIntensity, scanInfo.BasePeakIntensity, 0.0001);
                    Assert.AreEqual(scanInfoStruct.TotalIonCurrent, scanInfo.TotalIonCurrent, 0.0001);

                }
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
                {1513, new Dictionary<string, string>()},
                {1514, new Dictionary<string, string>()},
                {1515, new Dictionary<string, string>()},
                {1516, new Dictionary<string, string>()},
                {1517, new Dictionary<string, string>()}
            
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
                {16121, new Dictionary<string, string>()},
                {16122, new Dictionary<string, string>()},
                {16126, new Dictionary<string, string>()},
                {16131, new Dictionary<string, string>()},
                {16133, new Dictionary<string, string>()},
                {16141, new Dictionary<string, string>()}
            
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

                        var dataPointsRead = reader.GetScanData(scanNumber, out mzList, out intensityList, maxNumberOfPeaks, centroidData);

                        Assert.IsTrue(dataPointsRead > 0, "GetScanData returned 0 for scan " + scanNumber);

                        Assert.AreEqual(dataPointsRead, mzList.Length, "Data count mismatch vs. function return value");

                        var midPoint = (int)(intensityList.Length / 2f);

                        clsScanInfo scanInfo;
                        var success = reader.GetScanInfo(scanNumber, out scanInfo);

                        Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                        var scanSummary =
                            string.Format(
                                "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8} {8,8}  {9}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                mzList.Length, intensityList.Length,
                                mzList[0].ToString("0.000"), intensityList[0].ToString("0.0E+0"),
                                mzList[midPoint].ToString("0.000"), intensityList[midPoint].ToString("0.0E+0"),
                                scanInfo.FilterText);

                 
                        Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                        if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                        {
                            Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                        }
                        
                        Dictionary<string, string> expectedDataByType;
                        if (expectedDataThisFile.TryGetValue(scanNumber, out expectedDataByType))
                        {
                            var keySpec = maxNumberOfPeaks + "_" + centroidData;
                            string expectedDataDetails;
                            if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                            {
                                Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22),
                                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
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
                {1513, new Dictionary<string, string>()},
                {1514, new Dictionary<string, string>()}            
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
                {16121, new Dictionary<string, string>()},
                {16122, new Dictionary<string, string>()}            
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

                        double[,] massIntensityPairs;

                        var dataPointsRead = reader.GetScanData2D(scanNumber, out massIntensityPairs, maxNumberOfPeaks, centroidData);

                        Assert.IsTrue(dataPointsRead > 0, "GetScanData2D returned 0 for scan " + scanNumber);

                        clsScanInfo scanInfo;
                        var success = reader.GetScanInfo(scanNumber, out scanInfo);

                        Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                        var lastIndex = massIntensityPairs.GetUpperBound(1);

                        int dataCount;

                        if (maxNumberOfPeaks > 0)
                        {

                            if (centroidData && scanInfo.IsFTMS)
                            {
                                // When centroiding FTMS data, the maxNumberOfPeaks value is ignored
                                dataCount = lastIndex + 1;

                                var pointToCheck = maxNumberOfPeaks + (int)((lastIndex - maxNumberOfPeaks) / 2f);

                                Assert.IsTrue(massIntensityPairs[0, pointToCheck] > 50, "m/z value in 2D array is unexpectedly less than 50");
                            }
                            else
                            {
                                dataCount = maxNumberOfPeaks;

                                // Make sure the 2D array has values of 0 for mass and intensity beyond index maxNumberOfPeaks
                                for (var dataIndex = maxNumberOfPeaks; dataIndex < lastIndex; dataIndex++)
                                {
                                    if (massIntensityPairs[0, dataIndex] > 0)
                                    {
                                        Console.WriteLine("Non-zero m/z value found at index " + dataIndex + " for scan " + scanNumber);
                                        Assert.AreEqual(0, massIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                                    }

                                    if (massIntensityPairs[1, dataIndex] > 0)
                                    {
                                        Console.WriteLine("Non-zero intensity value found at index " + dataIndex + " for scan " + scanNumber);
                                        Assert.AreEqual(0, massIntensityPairs[1, dataIndex], "Non-zero intensity value found in 2D array beyond expected index");
                                    }
                                }
                            }
                        }
                        else
                        {
                            dataCount = lastIndex + 1;
                        }

                        Assert.AreEqual(dataPointsRead, dataCount, "Data count mismatch vs. function return value");

                        var midPoint = (int)(dataCount / 2f);

                        var scanSummary =
                            string.Format(
                                "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                dataCount,
                                massIntensityPairs[0, 0].ToString("0.000"), massIntensityPairs[1,0].ToString("0.0E+0"),
                                massIntensityPairs[0, midPoint].ToString("0.000"), massIntensityPairs[1, midPoint].ToString("0.0E+0"),
                                scanInfo.FilterText);


                        Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                        if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                        {
                            Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                        }
                        
                        Dictionary<string, string> expectedDataByType;
                        if (expectedDataThisFile.TryGetValue(scanNumber, out expectedDataByType))
                        {
                            var keySpec = maxNumberOfPeaks + "_" + centroidData;
                            string expectedDataDetails;
                            if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                            {
                                Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22),
                                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
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
                {1513, new Dictionary<string, string>()}        
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",  "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("0_True",   "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_False", "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_True",  "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, Dictionary<string, string>> {
                {16121, new Dictionary<string, string>()}
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

                    double[,] massIntensityPairs;

                    var dataPointsRead = reader.GetScanDataSumScans(scanStart, scanEnd, out massIntensityPairs, maxNumberOfPeaks, centroidData);

                    Assert.IsTrue(dataPointsRead > 0, string.Format("GetScanDataSumScans returned 0 summing scans {0} to {1}", scanStart, scanEnd));

                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanStart, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanStart);

                    var lastIndex = massIntensityPairs.GetUpperBound(1);
                    int dataCount;

                    if (maxNumberOfPeaks > 0)
                    {
                        dataCount = maxNumberOfPeaks;

                        // Make sure the 2D array has values of 0 for mass and intensity beyond index maxNumberOfPeaks
                        for (var dataIndex = maxNumberOfPeaks; dataIndex < lastIndex; dataIndex++)
                        {
                            if (massIntensityPairs[0, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero m/z value found at index " + dataIndex + " for scan " + scanStart);
                                Assert.AreEqual(0, massIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                            }

                            if (massIntensityPairs[1, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero intensity value found at index " + dataIndex + " for scan " + scanStart);
                                Assert.AreEqual(0, massIntensityPairs[1, dataIndex], "Non-zero intensity value found in 2D array beyond expected index");
                            }
                        }
                    }
                    else
                    {
                        dataCount = lastIndex + 1;
                    }

                    Assert.AreEqual(dataPointsRead, dataCount, "Data count mismatch vs. function return value");

                    var midPoint = (int)(dataCount / 2f);

                    var scanSummary =
                        string.Format(
                            "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}  {8}",
                            scanStart, maxNumberOfPeaks, centroidData,
                            dataCount,
                            massIntensityPairs[0, 0].ToString("0.000"), massIntensityPairs[1, 0].ToString("0.0E+0"),
                            massIntensityPairs[0, midPoint].ToString("0.000"), massIntensityPairs[1, midPoint].ToString("0.0E+0"),
                            scanInfo.FilterText);


                    Dictionary<int, Dictionary<string, string>> expectedDataThisFile;
                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }

                    Dictionary<string, string> expectedDataByType;
                    if (expectedDataThisFile.TryGetValue(scanStart, out expectedDataByType))
                    {
                        var keySpec = maxNumberOfPeaks + "_" + centroidData;
                        string expectedDataDetails;
                        if (expectedDataByType.TryGetValue(keySpec, out expectedDataDetails))
                        {
                            Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22),
                                            "Scan details mismatch, scan " + scanStart + ", keySpec " + keySpec);
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

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {

                    // List of mass, intensity, resolution, baseline intensity, noise floor, and charge for each data point
                    XRawFileIO.udtFTLabelInfoType[] ftLabelData;

                    var dataPointsRead = reader.GetScanLabelData(scanNumber, out ftLabelData);

                    if (dataPointsRead == -1)
                        Assert.AreEqual(0, ftLabelData.Length, "Data count mismatch vs. function return value");
                    else
                        Assert.AreEqual(dataPointsRead, ftLabelData.Length, "Data count mismatch vs. function return value");

                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanStart);

                    if (ftLabelData.Length == 0 && scanInfo.IsFTMS)
                    {
                        Assert.Fail("GetScanLabelData returned no data for FTMS scan " + scanNumber);
                    }

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
                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }
                    
                    string expectedScanSummary;
                    if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                    {
                        Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary,
                                        "Scan summary mismatch, scan " + scanNumber);
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

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {

                    // List of Intensity, Mass, AccuracyMMU, AccuracyPPM, and Resolution for each data point
                    XRawFileIO.udtMassPrecisionInfoType[] massResolutionData;

                    var dataPointsRead = reader.GetScanPrecisionData(scanNumber, out massResolutionData);
                    
                    if (dataPointsRead == -1)
                        Assert.AreEqual(0, massResolutionData.Length, "Data count mismatch vs. function return value");
                    else
                        Assert.AreEqual(dataPointsRead, massResolutionData.Length, "Data count mismatch vs. function return value");

                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);
                    
                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanStart);

                    if (massResolutionData.Length == 0 && scanInfo.IsFTMS)
                    {
                        Assert.Fail("GetScanPrecisionData returned no data for FTMS scan " + scanNumber);                        
                    }

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
                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }
                    
                    string expectedScanSummary;
                    if (expectedDataThisFile.TryGetValue(scanNumber, out expectedScanSummary))
                    {
                        Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary,
                                        "Scan summary mismatch, scan " + scanNumber);
                    }

                    Console.WriteLine(scanSummary);
                }
            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        [TestCase(@"HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 45000, 45200)]
        public void TestScanEventData(string rawFileName, int scanStart, int scanEnd)
        {
            // Keys in this Dictionary are filename, values are ScanCounts by event, where the key is a Tuple of EventName and EventValue
            var expectedData = new Dictionary<string, Dictionary<Tuple<string, string>, int>>();

            AddExpectedTupleAndCount(expectedData, "Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", "Resolution:", "Low", 101);
            AddExpectedTupleAndCount(expectedData, "Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", "Scan Event:", "1", 25);
            AddExpectedTupleAndCount(expectedData, "Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", "Scan Event:", "2", 25);
            AddExpectedTupleAndCount(expectedData, "Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", "Scan Event:", "3", 25);
            AddExpectedTupleAndCount(expectedData, "Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", "Scan Event:", "4", 26);

            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "0", 21);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "2", 134);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "3", 30);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "4", 14);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "7", 1);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "Charge State:", "8", 1);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "MS2 Isolation Width:", "2.000", 180);
            AddExpectedTupleAndCount(expectedData, "HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", "MS2 Isolation Width:", "1200.000", 21);


            var dataFile = GetRawDataFile(rawFileName);

            Dictionary<Tuple<string, string>, int> expectedEventsThisFile;
            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out expectedEventsThisFile))
            {
                Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
            }

            var eventsToCheck = (from item in expectedEventsThisFile select item.Key.Item1).Distinct().ToList();
            var eventCountsActual = new Dictionary<Tuple<string, string>, int>();
 
            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    foreach (var eventName in eventsToCheck)
                    {
                        string eventValue;
                        scanInfo.TryGetScanEvent(eventName, out eventValue);

                        var eventKey = new Tuple<string, string>(eventName, eventValue);
                        int scanCount;
                        if (eventCountsActual.TryGetValue(eventKey, out scanCount))
                        {
                            eventCountsActual[eventKey] = scanCount + 1;
                        }
                        else
                        {
                            eventCountsActual.Add(eventKey, 1);
                        }
                    }

                }

                Console.WriteLine("{0,-5} {1,5} {2}", "Valid", "Count", "Event");

                foreach (var observedEvent in (from item in eventCountsActual orderby item.Key select item))
                {
                    int expectedScanCount;
                    if (expectedEventsThisFile.TryGetValue(observedEvent.Key, out expectedScanCount))                 
                    {
                        var isValid = observedEvent.Value == expectedScanCount;

                        Console.WriteLine("{0,-5} {1,5} {2} {3}", isValid, observedEvent.Value, observedEvent.Key.Item1, observedEvent.Key.Item2);

                        Assert.AreEqual(expectedScanCount, observedEvent.Value, "Event count mismatch");
                    }
                    else
                    {
                        Console.WriteLine("Unexpected event/value found: {0} {1}", observedEvent.Key.Item1, observedEvent.Key.Item2);
                        Assert.Fail("Unexpected event/value found: {0} {1}", observedEvent.Key.Item1, observedEvent.Key.Item2);
                    }
                }

            }
        }

        [Test]
        [TestCase(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        [TestCase(@"MZ20150721blank2.raw", 300, 400)]
        [TestCase(@"B5_50uM_MS_r1.RAW", 1, 20)]
        public void TestScanInfoCopyFromStruct(string rawFileName, int scanStart, int scanEnd)
        {

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

                Console.WriteLine("Checking clsScanInfo initializing from a struct using {0}", dataFile.Name);

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    clsScanInfo scanInfo;
                    var success = reader.GetScanInfo(scanNumber, out scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan " + scanNumber);

                    var udtScanHeaderInfo = new FinniganFileReaderBaseClass.udtScanHeaderInfoType
                    {
                        MSLevel = scanInfo.MSLevel,
                        EventNumber = scanInfo.EventNumber,
                        SIMScan = scanInfo.SIMScan,
                        MRMScanType = scanInfo.MRMScanType,
                        ZoomScan = scanInfo.ZoomScan,
                        NumPeaks = scanInfo.NumPeaks,
                        RetentionTime = scanInfo.RetentionTime,
                        LowMass = scanInfo.LowMass,
                        HighMass = scanInfo.HighMass,
                        TotalIonCurrent = scanInfo.TotalIonCurrent,
                        BasePeakMZ = scanInfo.BasePeakMZ,
                        BasePeakIntensity = scanInfo.BasePeakIntensity,
                        FilterText = scanInfo.FilterText,
                        ParentIonMZ = scanInfo.ParentIonMZ,
                        ActivationType = scanInfo.ActivationType,
                        CollisionMode = scanInfo.CollisionMode,
                        IonMode = scanInfo.IonMode,
                        MRMInfo = scanInfo.MRMInfo,
                        NumChannels = scanInfo.NumChannels,
                        UniformTime = scanInfo.UniformTime,
                        Frequency = scanInfo.Frequency,
                        IsCentroidScan = scanInfo.IsCentroided,
                        ScanEventNames = new string[scanInfo.ScanEvents.Count],
                        ScanEventValues = new string[scanInfo.ScanEvents.Count],
                        StatusLogNames = new string[scanInfo.StatusLog.Count],
                        StatusLogValues = new string[scanInfo.StatusLog.Count]
                    };

                    var targetIndex = 0;
                    foreach (var scanEvent in scanInfo.ScanEvents)
                    {
                        udtScanHeaderInfo.ScanEventNames[targetIndex] = scanEvent.Key;
                        udtScanHeaderInfo.ScanEventValues[targetIndex] = scanEvent.Value;
                        targetIndex++;
                    }

                    targetIndex = 0;
                    foreach (var scanEvent in scanInfo.StatusLog)
                    {
                        udtScanHeaderInfo.StatusLogNames[targetIndex] = scanEvent.Key;
                        udtScanHeaderInfo.StatusLogValues[targetIndex] = scanEvent.Value;
                        targetIndex++;
                    }

                    var scanInfoFromStruct = new clsScanInfo(scanInfo.ScanNumber, udtScanHeaderInfo);

                    Assert.AreEqual(scanInfoFromStruct.MSLevel, scanInfo.MSLevel);
                    Assert.AreEqual(scanInfoFromStruct.IsCentroided, scanInfo.IsCentroided);
                    Assert.AreEqual(scanInfoFromStruct.FilterText, scanInfo.FilterText);
                    Assert.AreEqual(scanInfoFromStruct.BasePeakIntensity, scanInfo.BasePeakIntensity, 0.0001);
                    Assert.AreEqual(scanInfoFromStruct.TotalIonCurrent, scanInfo.TotalIonCurrent, 0.0001);

                }
            }

        }

        private void AddExpectedTupleAndCount(
            IDictionary<string, Dictionary<Tuple<string, string>, int>> expectedData,
            string fileName,
            string tupleKey1,
            string tupleKey2,
            int scanCount)
                {

            Dictionary<Tuple<string, string>, int> expectedScanInfo;
            if (!expectedData.TryGetValue(fileName, out expectedScanInfo))
            {
                expectedScanInfo = new Dictionary<Tuple<string, string>, int>();
                expectedData.Add(fileName, expectedScanInfo);
            }

            expectedScanInfo.Add(new Tuple<string, string>(tupleKey1, tupleKey2), scanCount);
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
