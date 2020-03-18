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

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw")]
        [TestCase("HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw")]
        [TestCase("MZ0210MnxEF889ETD.raw")]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw")]
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
            var file1Data = new Dictionary<int, List<double>>
            {
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

            var file2Data = new Dictionary<int, List<double>>
            {
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

            var file3Data = new Dictionary<int, List<double>>
            {
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

            var file4Data = new Dictionary<int, List<double>>
            {
                {1, ce30},
                {2, ce30}

            };
            expectedData.Add("MZ0210MnxEF889ETD", file4Data);

            var file5Data = new Dictionary<int, List<double>>
            {
                {27799, ms1Scan},
                {27800, ce30},
                {27801, ce30},
                {27802, ce30},
                {27803, ce30},
                {27804, ce30},
                {27805, ms1Scan},
                {27806, ce30},
                {27807, ce30},

            };
            expectedData.Add("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", file5Data);

            var dataFile = GetRawDataFile(rawFileName);

            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var collisionEnergiesThisFile))
            {
                Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
            }

            // Keys are scan number, values are the list of collision energies
            var collisionEnergiesActual = new Dictionary<int, List<double>>();

            // Keys are scan number, values are msLevel
            var msLevelsActual = new Dictionary<int, int>();

            // Keys are scan number, values are the ActivationType, for example cid, etd, hcd
            var activationTypesActual = new Dictionary<int, string>();
            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                foreach (var scanNumber in collisionEnergiesThisFile.Keys)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                    var collisionEnergiesThisScan = reader.GetCollisionEnergy(scanNumber);
                    collisionEnergiesActual.Add(scanNumber, collisionEnergiesThisScan);

                    msLevelsActual.Add(scanNumber, scanInfo.MSLevel);

                    activationTypesActual.Add(scanNumber, scanInfo.ActivationType.ToString());
                }

                Console.WriteLine("{0,-5} {1,-5} {2}", "Valid", "Scan", "Collision Energy");

                foreach (var actualEnergiesOneScan in (from item in collisionEnergiesActual orderby item.Key select item))
                {
                    var scanNumber = actualEnergiesOneScan.Key;

                    var expectedEnergies = collisionEnergiesThisFile[scanNumber];

                    var activationTypes = string.Join(", ", activationTypesActual[scanNumber]);

                    if (actualEnergiesOneScan.Value.Count == 0)
                    {
                        var msLevel = msLevelsActual[scanNumber];

                        if (msLevel != 1)
                        {
                            var msg = string.Format(
                                "Scan {0} has no collision energies, which should only be true for spectra with msLevel=1. This scan has msLevel={1} and activationType={2}",
                                scanNumber, msLevel, activationTypes);
                            Console.WriteLine(msg);

                            Assert.Fail(msg);
                        }
                        else
                        {
                            Console.WriteLine("{0,-5} {1,-5} {2}", true, scanNumber, "MS1 scan");
                        }
                    }
                    else
                    {
                        foreach (var actualEnergy in actualEnergiesOneScan.Value)
                        {
                            var isValid = expectedEnergies.Any(expectedEnergy => Math.Abs(actualEnergy - expectedEnergy) < 0.00001);

                            Console.WriteLine("{0,-5} {1,-5} {2:F2}", isValid, scanNumber, actualEnergy);

                            Assert.IsTrue(isValid, "Unexpected collision energy {0:F2} for scan {1}", actualEnergy, scanNumber);
                        }
                    }

                    if (expectedEnergies.Count != actualEnergiesOneScan.Value.Count)
                    {
                        var msg = string.Format("Collision energy count mismatch for scan {0}", scanNumber);
                        Console.WriteLine(msg);
                        Assert.AreEqual(expectedEnergies.Count, actualEnergiesOneScan.Value.Count, msg);
                    }

                }

            }
        }

        [Test]
        [TestCase("blank_MeOH-3_18May16_Rainier_Thermo_10344958.raw", 1500, 1900, 190, 211, 0, 0)]
        [TestCase("Corrupt_Qc_Shew_13_04_pt1_a_5Sep13_Cougar_13-06-14.raw", 0, -1, -1, 0, 0, 0)]
        [TestCase("Corrupt_QC_Shew_07_03_pt25_e_6Apr08_Falcon_Fst-75-1.raw", 0, -1, -1, 0, 0, 0)]
        // When using XRawfile, this dataset caused .NET to become unstable and abort the unit tests
        // In contrast, ThermoFisher.CommonCore.RawFileReader can open this file
        [TestCase("Corrupt_Scans6920-7021_AID_STM_013_101104_06_LTQ_16Nov04_Earth_0904-8.raw", 6900, 7050, 25, 126, 6920, 7021)]
        public void TestCorruptDataHandling(
            string rawFileName,
            int scanStart,
            int scanEnd,
            int expectedMS1,
            int expectedMS2,
            int corruptScanStart,
            int corruptScanEnd)
        {
            var dataFile = GetRawDataFile(rawFileName);

            try
            {

                using (var reader = new XRawFileIO(dataFile.FullName))
                {

                    var scanCount = reader.GetNumScans();
                    Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);

                    if (expectedMS1 == -1 && expectedMS2 == 0)
                    {
                        Assert.IsFalse(reader.FileInfo.CorruptFile, "CorruptFile is false while we expected it to be true");
                        Assert.IsTrue(scanCount == -1, "ScanCount is not -1");
                    }
                    else if (expectedMS1 + expectedMS2 == 0)
                    {
                        Assert.IsTrue(reader.FileInfo.CorruptFile, "CorruptFile is false while we expected it to be true");
                        Assert.IsTrue(scanCount <= 0, "ScanCount is non-zero, while we expected it to be 0");
                    }
                    else
                    {
                        Assert.IsFalse(reader.FileInfo.CorruptFile, "CorruptFile is true while we expected it to be false");
                        Assert.IsTrue(scanCount > 0, "ScanCount is zero, while we expected it to be > 0");
                    }

                    var scanCountMS1 = 0;
                    var scanCountMS2 = 0;

                    for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                    {
                        try
                        {
                            reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                            if (reader.FileInfo.CorruptFile)
                            {
                                Assert.IsTrue(string.IsNullOrEmpty(scanInfo.FilterText), "FilterText is not empty but should be since corrupt file");
                            }
                            else
                            {
                                Assert.IsFalse(string.IsNullOrEmpty(scanInfo.FilterText), "FilterText is empty but should not be");

                                if (scanInfo.MSLevel > 1)
                                    scanCountMS2++;
                                else
                                    scanCountMS1++;
                            }

                            // Note: this function call will fail randomly with file Corrupt_Scans6920-7021_AID_STM_013_101104_06_LTQ_16Nov04_Earth_0904-8.raw
                            // Furthermore, we are unable to catch the exception that occurs (or no exception is thrown) and adding
                            // [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions] to the function does not help
                            var dataPointCount = reader.GetScanData(scanNumber, out var mzList, out var intensityList);

                            if (reader.FileInfo.CorruptFile)
                            {
                                Assert.IsTrue(dataPointCount == 0, "GetScanData unexpectedly reported a non-zero data count for scan {0}", scanNumber);
                                Assert.IsTrue(mzList.Length == 0, "GetScanData unexpectedly returned m/z data for scan {0}", scanNumber);
                                Assert.IsTrue(intensityList.Length == 0, "GetScanData unexpectedly returned intensity data for scan {0}", scanNumber);
                            }
                            else
                            {
                                if (dataPointCount == 0)
                                {
                                    Console.WriteLine("Corrupt scan encountered: {0}", scanNumber);

                                    Assert.IsTrue(scanNumber >= corruptScanStart && scanNumber <= corruptScanEnd, "Unexpected corrupt scan found, scan {0}", scanNumber);
                                    Assert.IsTrue(mzList.Length == 0, "GetScanData unexpectedly returned m/z data for scan {0}", scanNumber);
                                    Assert.IsTrue(intensityList.Length == 0, "GetScanData unexpectedly returned intensity data for scan {0}", scanNumber);
                                }
                                else
                                {
                                    Assert.IsTrue(dataPointCount > 0, "GetScanData reported a data point count of 0 for scan {0}", scanNumber);
                                    Assert.IsTrue(mzList.Length > 0, "GetScanData unexpectedly returned no m/z data for scan {0}", scanNumber);
                                    Assert.IsTrue(intensityList.Length > 0, "GetScanData unexpectedly returned no intensity data for scan {0}", scanNumber);
                                    Assert.IsTrue(mzList.Length == intensityList.Length, "Array length mismatch for m/z and intensity data for scan {0}", scanNumber);
                                    Assert.IsTrue(dataPointCount == mzList.Length, "Array length does not agree with dataPointCount for scan {0}", scanNumber);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception reading scan {0}: {1}", scanNumber, ex.Message);
                            Assert.Fail("Exception reading scan {0}", scanNumber);
                        }
                    }

                    Console.WriteLine("scanCountMS1={0}", scanCountMS1);
                    Console.WriteLine("scanCountMS2={0}", scanCountMS2);

                    if (expectedMS1 >= 0)
                        Assert.AreEqual(expectedMS1, scanCountMS1, "MS1 scan count mismatch");

                    if (expectedMS2 >= 0)
                        Assert.AreEqual(expectedMS2, scanCountMS2, "MS2 scan count mismatch");

                }
            }
            catch (Exception ex)
            {
                var msg = string.Format("Exception opening .raw file {0}:\n{1}", rawFileName, ex.Message);
                Console.WriteLine(msg);
                Assert.Fail(msg);
            }

        }


        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1, 5000, 23)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 1, 75000, 127)]
        [TestCase("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08.raw", 1, 8000, 29)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 1, 8000, 27)]
        [TestCase("Lewy2_19Ct1_2Nov13_Samwise_13-07-28.raw", 1, 44000, 127)]
        public void TestDataIsSortedByMz(string rawFileName, int scanStart, int scanEnd, int scanStep)
        {

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

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

                    if (iteration == 1)
                    {
                        Console.WriteLine("Scan data for {0}", dataFile.Name);
                        Console.WriteLine("{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10} {6,-8} {7,-10} {8,-8}  {9}",
                                          "Scan", "Max#", "Centroid", "MzCount", "IntCount",
                                          "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");
                    }

                    if (scanEnd > reader.FileInfo.ScanEnd)
                        scanEnd = reader.FileInfo.ScanEnd;

                    if (scanStep < 1)
                        scanStep = 1;

                    var statsInterval = (int)Math.Floor((scanEnd - scanStart) / (double)scanStep / 10);
                    var scansProcessed = 0;

                    for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber += scanStep)
                    {

                        var dataPointsRead = reader.GetScanData(scanNumber, out var mzList, out var intensityList, maxNumberOfPeaks, centroidData);

                        var unsortedMzValues = 0;

                        for (var i = 0; i < dataPointsRead - 1; i++)
                        {
                            if (mzList[i] > mzList[i + 1])
                                unsortedMzValues++;
                        }

                        Assert.AreEqual(0, unsortedMzValues, "Scan {0} has {1} m/z values not sorted properly", scanNumber, unsortedMzValues);

                        scansProcessed++;
                        if (scansProcessed % statsInterval == 0)
                        {
                            reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                            if (mzList.Length > 0)
                            {
                                var midIndex = (int)Math.Floor(mzList.Length / 2.0);

                                Console.WriteLine("{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10:0.0000} {6,-8:0.0} {7,-10:0.0000} {8,-8:0.0}  {9}",
                                                  scanNumber, maxNumberOfPeaks, centroidData, mzList.Length, intensityList.Length,
                                                  mzList[0], intensityList[0], mzList[midIndex], intensityList[midIndex], scanInfo.FilterText);
                            }
                            else
                            {
                                Console.WriteLine("{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10} {6,-8} {7,-10} {8,-8}  {9}",
                                                  scanNumber, maxNumberOfPeaks, centroidData, mzList.Length, intensityList.Length,
                                                  "n/a", "n/a", "n/a", "n/a", scanInfo.FilterText);
                            }
                        }

                    }

                }
            }

        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 3316)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 71147)]
        [TestCase("Angiotensin_325-CID.raw", 10)]
        [TestCase("Angiotensin_325-ETciD-15.raw", 10)]
        [TestCase("Angiotensin_325-ETD.raw", 10)]
        [TestCase("Angiotensin_325-HCD.raw", 10)]
        [TestCase("Angiotensin_AllScans.raw", 1775)]
        public void TestGetNumScans(string rawFileName, int expectedResult)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                var scanCount = reader.GetNumScans();

                Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);
                if (expectedResult >= 0)
                    Assert.AreEqual(expectedResult, scanCount, "Scan count mismatch");
            }
        }

        [Test]
        [TestCase("B5_50uM_MS_r1.RAW", 1, 20, 20, 0, 20)]
        [TestCase("MNSLTFKK_ms.raw", 1, 88, 88, 0, 88)]
        [TestCase("QCShew200uL.raw", 4000, 4100, 101, 0, 8151)]
        [TestCase("Wrighton_MT2_SPE_200avg_240k_neg_330-380.raw", 1, 200, 200, 0, 200)]
        [TestCase("1229_02blk1.raw", 6000, 6100, 77, 24, 16142)]
        [TestCase("MCF7_histone_32_49B_400min_HCD_ETD_01172014_b.raw", 2300, 2400, 18, 83, 8237)]
        [TestCase("lowdose_IMAC_iTRAQ1_PQDMSA.raw", 15000, 15100, 16, 85, 27282)]
        [TestCase("MZ20150721blank2.raw", 1, 434, 62, 372, 434)]
        [TestCase("OG_CEPC_PU_22Oct13_Legolas_13-05-12.raw", 5000, 5100, 9, 92, 11715)]
        [TestCase("blank_MeOH-3_18May16_Rainier_Thermo_10344958.raw", 1500, 1900, 190, 211, 3139)]
        [TestCase("HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw", 25200, 25600, 20, 381, 39157)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 5900, 6000, 8, 93, 7906)]
        [TestCase("IPA-blank-07_25Oct13_Gimli.raw", 1750, 1850, 101, 0, 3085)]
        [TestCase("Angiotensin_325-CID.raw", 1, 10, 0, 10, 10)]
        [TestCase("Angiotensin_325-ETciD-15.raw", 1, 10, 0, 10, 10)]
        [TestCase("Angiotensin_325-ETD.raw", 1, 10, 0, 10, 10)]
        [TestCase("Angiotensin_325-HCD.raw", 1, 10, 0, 10, 10)]
        [TestCase("Angiotensin_AllScans.raw", 1000, 1200, 10, 191, 1775)]
        public void TestGetScanCountsByScanType(
            string rawFileName,
            int scanStart,
            int scanEnd,
            int expectedMS1,
            int expectedMS2,
            int expectedTotalScanCount)
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

            const string file13 = "Angiotensin_AllScans";
            AddExpectedTupleAndCount(expectedData, file13, "ETD-HMSn", "FTMS + c ESI d Full ms2 0@etd121.47", 28);
            AddExpectedTupleAndCount(expectedData, file13, "ETD-HMSn", "FTMS + c ESI d Full ms2 0@etd53.99", 20);
            AddExpectedTupleAndCount(expectedData, file13, "HCD-HMSn", "FTMS + c ESI d Full ms2 0@hcd30.00", 47);
            AddExpectedTupleAndCount(expectedData, file13, "HMS", "FTMS + p ESI Full ms", 10);
            AddExpectedTupleAndCount(expectedData, file13, "SA_CID-HMSn", "FTMS + c ESI d sa Full ms2 0@etd121.47 0@cid30.00", 28);
            AddExpectedTupleAndCount(expectedData, file13, "SA_CID-HMSn", "FTMS + c ESI d sa Full ms2 0@etd53.99 0@cid30.00", 20);
            AddExpectedTupleAndCount(expectedData, file13, "SA_HCD-HMSn", "FTMS + c ESI d sa Full ms2 0@etd121.47 0@hcd30.00", 28);
            AddExpectedTupleAndCount(expectedData, file13, "SA_HCD-HMSn", "FTMS + c ESI d sa Full ms2 0@etd53.99 0@hcd30.00", 20);


            AddExpectedTupleAndCount(expectedData, "IPA-blank-07_25Oct13_Gimli", "Zoom-MS", "ITMS + p NSI Z ms", 101);

            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-CID", "CID-HMSn", "FTMS + p ESI Full ms2 0@cid35.00", 10);

            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-ETciD-15", "SA_CID-HMSn", "FTMS + p ESI sa Full ms2 0@etd50.00 0@cid15.00", 10);
            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-ETD", "SA_CID-HMSn", "FTMS + p ESI sa Full ms2 0@etd50.00 0@cid15.00", 10);
            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-HCD", "HCD-HMSn", "FTMS + p ESI Full ms2 0@hcd30.00", 10);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Parsing scan headers for {0}", dataFile.Name);

                var scanCount = reader.GetNumScans();
                Console.WriteLine("Total scans: {0}", scanCount);
                Assert.AreEqual(expectedTotalScanCount, scanCount, "Total scan count mismatch");
                Console.WriteLine();

                var scanCountMS1 = 0;
                var scanCountMS2 = 0;
                var scanTypeCountsActual = new Dictionary<Tuple<string, string>, int>();

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                    var scanType = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(scanInfo.FilterText);
                    var genericScanFilter = XRawFileIO.MakeGenericThermoScanFilter(scanInfo.FilterText);

                    var scanTypeKey = new Tuple<string, string>(scanType, genericScanFilter);

                    if (scanTypeCountsActual.TryGetValue(scanTypeKey, out var observedScanCount))
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

                if (expectedMS1 >= 0)
                    Assert.AreEqual(expectedMS1, scanCountMS1, "MS1 scan count mismatch");

                if (expectedMS2 >= 0)
                    Assert.AreEqual(expectedMS2, scanCountMS2, "MS2 scan count mismatch");

                if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedScanInfo))
                {
                    // Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    expectedScanInfo = new Dictionary<Tuple<string, string>, int>();
                }

                Console.WriteLine("{0,-5} {1,5} {2}", "Valid", "Count", "ScanType");

                foreach (var scanType in (from item in scanTypeCountsActual orderby item.Key select item))
                {
                    if (expectedScanInfo.Count == 0)
                    {
                        Console.WriteLine("{0,5} {1}", scanType.Value, scanType.Key);
                        continue;
                    }

                    if (expectedScanInfo.TryGetValue(scanType.Key, out var expectedScanCount))
                    {
                        var isValid = scanType.Value == expectedScanCount;

                        Console.WriteLine("{0,-5} {1,5} {2}", isValid, scanType.Value, scanType.Key);

                        if (expectedScanCount >= 0)
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
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521, 3, 6)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165, 3, 42)]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw", 20500, 20520, 7, 14)]
        public void TestGetScanInfo(string rawFileName, int scanStart, int scanEnd, int expectedMS1, int expectedMS2)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            // Keys in this dictionary are the scan number whose metadata is being retrieved
            var file1Data = new Dictionary<int, string>
            {
                {1513, "1 1   851 44.57 400 2000 6.3E+8 1089.978 1.2E+7     0.00 CID       Positive True False 11 79   1.50 + c ESI Full..."},
                {1514, "2 2   109 44.60 230 1780 5.0E+6  528.128 7.2E+5   884.41 CID   cid Positive True False 11 79  28.96 + c d Full m..."},
                {1515, "2 3   290 44.63 305 2000 2.6E+7 1327.414 6.0E+6  1147.67 CID   cid Positive True False 11 79  14.13 + c d Full m..."},
                {1516, "2 4   154 44.66 400 2000 7.6E+5 1251.554 3.7E+4  1492.90 CID   cid Positive True False 11 79 123.30 + c d Full m..."},
                {1517, "1 1   887 44.69 400 2000 8.0E+8 1147.613 1.0E+7     0.00 CID       Positive True False 11 79   1.41 + c ESI Full..."},
                {1518, "2 2   190 44.71 380 2000 4.6E+6 1844.618 2.7E+5  1421.21 CID   cid Positive True False 11 79  40.91 + c d Full m..."},
                {1519, "2 3   165 44.74 380 2000 6.0E+6 1842.547 6.9E+5  1419.24 CID   cid Positive True False 11 79  37.84 + c d Full m..."},
                {1520, "2 4   210 44.77 265 2000 1.5E+6 1361.745 4.2E+4  1014.93 CID   cid Positive True False 11 79  96.14 + c d Full m..."},
                {1521, "1 1   860 44.80 400 2000 6.9E+8 1126.627 2.9E+7     0.00 CID       Positive True False 11 79   1.45 + c ESI Full..."}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            // Note that for this dataset NumPeaks does not accurately reflect the number of data in each mass spectrum (it's much higher than it should be)
            // For example, scan 16121 has NumPeaks = 45876, but TestGetScanData() correctly finds 11888 data points for that scan
            var file2Data = new Dictionary<int, string>
            {
                {16121, "1 1 45876 47.68 350 1550 1.9E+9  503.565 3.4E+8     0.00 CID       Positive False True 46 219   0.44 FTMS + p NSI..."},
                {16122, "2 2  4124 47.68 106  817 1.6E+6  550.309 2.1E+5   403.22 CID   cid Positive True False 46 219  11.82 ITMS + c NSI..."},
                {16123, "2 2  6484 47.68 143 1627 5.5E+5  506.272 4.9E+4   538.84 CID   cid Positive True False 46 219  26.07 ITMS + c NSI..."},
                {16124, "2 2  8172 47.68 208 2000 7.8E+5  737.530 7.0E+4   776.27 CID   cid Positive True False 46 219  24.65 ITMS + c NSI..."},
                {16125, "2 2  5828 47.68 120 1627 2.1E+5  808.486 2.2E+4   538.84 ETD   etd Positive True False 46 219  42.48 ITMS + c NSI..."},
                {16126, "2 2  6228 47.68 120 1627 1.4E+5  536.209 9.0E+3   538.84 ETD ETciD Positive True False 46 219  58.96 ITMS + c NSI..."},
                {16127, "2 2  7180 47.68 120 1627 1.3E+5  808.487 1.4E+4   538.84 ETD EThcD Positive True False 46 219  58.96 ITMS + c NSI..."},
                {16128, "2 2  7980 47.69 225 1682 4.4E+5  805.579 2.3E+4   835.88 CID   cid Positive True False 46 219  42.71 ITMS + c NSI..."},
                {16129, "2 2  7700 47.69 266 1986 3.4E+5  938.679 2.9E+4   987.89 CID   cid Positive True False 46 219  35.75 ITMS + c NSI..."},
                {16130, "2 2  5180 47.69 110  853 2.7E+5  411.977 1.2E+4   421.26 CID   cid Positive True False 46 219  50.98 ITMS + c NSI..."},
                {16131, "2 2   436 47.69 120 1986 2.1E+4  984.504 9.5E+3   987.89 ETD   etd Positive True False 46 219  26.55 ITMS + c NSI..."},
                {16132, "2 2  2116 47.69 120  853 1.2E+4  421.052 6.8E+2   421.26 ETD   etd Positive True False 46 219 127.21 ITMS + c NSI..."},
                {16133, "2 2  2444 47.70 120  853 1.5E+4  421.232 1.2E+3   421.26 ETD ETciD Positive True False 46 219 110.22 ITMS + c NSI..."},
                {16134, "2 2  2948 47.70 120  853 1.4E+4  838.487 7.5E+2   421.26 ETD EThcD Positive True False 46 219 110.22 ITMS + c NSI..."},
                {16135, "2 2   508 47.70 120 1986 2.1E+4  984.498 9.2E+3   987.89 ETD ETciD Positive True False 46 219  31.82 ITMS + c NSI..."},
                {16136, "2 2   948 47.71 120 1986 2.3E+4  984.491 9.4E+3   987.89 ETD EThcD Positive True False 46 219  31.82 ITMS + c NSI..."},
                {16137, "2 2  9580 47.71 336 2000 3.5E+5 1536.038 4.7E+3  1241.01 CID   cid Positive True False 46 219  30.70 ITMS + c NSI..."},
                {16138, "2 2  7604 47.72 235 1760 2.9E+5  826.095 2.5E+4   874.84 CID   cid Positive True False 46 219  40.56 ITMS + c NSI..."},
                {16139, "2 2   972 47.72 120 1760 1.6E+4  875.506 2.1E+3   874.84 ETD   etd Positive True False 46 219  45.88 ITMS + c NSI..."},
                {16140, "2 2  1596 47.72 120 1760 1.8E+4 1749.846 2.0E+3   874.84 ETD ETciD Positive True False 46 219  54.15 ITMS + c NSI..."},
                {16141, "2 2  2124 47.72 120 1760 1.6E+4  874.664 1.6E+3   874.84 ETD EThcD Positive True False 46 219  54.15 ITMS + c NSI..."},
                {16142, "1 1 51976 47.73 350 1550 1.3E+9  503.565 1.9E+8     0.00 CID       Positive False True 46 219   0.79 FTMS + p NSI..."},
                {16143, "2 2  5412 47.73 128  981 6.5E+5  444.288 6.4E+4   485.28 CID   cid Positive True False 46 219  22.26 ITMS + c NSI..."},
                {16144, "2 2  4300 47.73 101 1561 5.0E+5  591.309 4.0E+4   387.66 CID   cid Positive True False 46 219  28.20 ITMS + c NSI..."},
                {16145, "2 2  6740 47.73 162 1830 4.0E+5  567.912 2.8E+4   606.62 CID   cid Positive True False 46 219  37.30 ITMS + c NSI..."},
                {16146, "2 2  4788 47.73  99  770 1.9E+5  532.308 3.4E+4   379.72 CID   cid Positive True False 46 219 100.00 ITMS + c NSI..."},
                {16147, "2 2  6708 47.74 120 1830 3.8E+5  603.095 3.1E+4   606.62 ETD   etd Positive True False 46 219  25.47 ITMS + c NSI..."},
                {16148, "2 2  7260 47.74 120 1830 1.5E+5  603.076 1.3E+4   606.62 ETD ETciD Positive True False 46 219  61.48 ITMS + c NSI..."},
                {16149, "2 2  9172 47.74 120 1830 1.6E+5  603.027 1.1E+4   606.62 ETD EThcD Positive True False 46 219  61.48 ITMS + c NSI..."},
                {16150, "2 2  5204 47.74  95 1108 3.8E+5  418.536 1.2E+5   365.88 CID   cid Positive True False 46 219 134.71 ITMS + c NSI..."},
                {16151, "2 2  5636 47.75 146 1656 2.8E+5  501.523 4.3E+4   548.54 CID   cid Positive True False 46 219  30.60 ITMS + c NSI..."},
                {16152, "2 2  9572 47.75 328 2000 1.8E+5  848.497 2.2E+3  1210.30 CID   cid Positive True False 46 219  38.05 ITMS + c NSI..."},
                {16153, "2 2  5004 47.75 120 1656 1.3E+5  548.396 1.3E+4   548.54 ETD   etd Positive True False 46 219  50.35 ITMS + c NSI..."},
                {16154, "2 2  4732 47.75 120 1656 4.2E+4  548.450 4.2E+3   548.54 ETD ETciD Positive True False 46 219 122.26 ITMS + c NSI..."},
                {16155, "2 2  6228 47.76 120 1656 4.2E+4  550.402 3.6E+3   548.54 ETD EThcD Positive True False 46 219 122.26 ITMS + c NSI..."},
                {16156, "2 2  9164 47.76 324 2000 1.5E+5 1491.872 1.0E+4  1197.57 CID   cid Positive True False 46 219  63.61 ITMS + c NSI..."},
                {16157, "2 2  5916 47.76 124  950 2.2E+5  420.689 2.2E+4   469.71 CID   cid Positive True False 46 219 100.00 ITMS + c NSI..."},
                {16158, "2 2  5740 47.76 306 2000 1.3E+5 1100.042 3.5E+3  1132.02 CID   cid Positive True False 46 219  27.79 ITMS + c NSI..."},
                {16159, "2 2  5540 47.76 122  935 1.9E+5  445.117 2.7E+4   462.15 CID   cid Positive True False 46 219  69.09 ITMS + c NSI..."},
                {16160, "2 2  5756 47.77 145 1646 3.4E+5  539.065 6.0E+4   545.18 CID   cid Positive True False 46 219  28.97 ITMS + c NSI..."},
                {16161, "2 2  6100 47.77 157 1191 2.8E+5  541.462 6.0E+4   590.28 CID   cid Positive True False 46 219  37.92 ITMS + c NSI..."},
                {16162, "2 2  2508 47.77 120 1191 8.4E+4 1180.615 5.1E+3   590.28 ETD   etd Positive True False 46 219  38.31 ITMS + c NSI..."},
                {16163, "2 2  2644 47.77 120 1191 1.8E+4 1184.614 9.0E+2   590.28 ETD ETciD Positive True False 46 219 109.20 ITMS + c NSI..."},
                {16164, "2 2  3180 47.77 120 1191 1.7E+4 1184.644 8.7E+2   590.28 ETD EThcD Positive True False 46 219 109.20 ITMS + c NSI..."},
                {16165, "1 1 53252 47.78 350 1550 1.2E+9  503.565 1.6E+8     0.00 CID       Positive False True 46 219   0.76 FTMS + p NSI..."}
            };
            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);

            var file3Data = new Dictionary<int, string>
            {
                {20500, "2 2  1264 41.90 110 1068 2.6E+6  416.224 2.9E+5   352.54 HCD   hcd Positive True True 33 221  12.93 FTMS + c NSI..."},
                {20501, "1 1 13472 41.90 350 1800 1.3E+9  599.293 1.0E+8     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20502, "2 2  1680 41.90 110 1883 3.9E+6 1063.568 2.2E+5   624.30 HCD   hcd Positive True True 33 221  12.87 FTMS + c NSI..."},
                {20503, "2 2  1392 41.90 110 1924 3.1E+6  637.336 3.0E+5   637.69 HCD   hcd Positive True True 33 221  13.69 FTMS + c NSI..."},
                {20504, "1 1 14120 41.90 350 1800 1.3E+9  554.304 9.7E+7     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20505, "2 2  2048 41.90 110 1233 8.4E+6  911.447 1.1E+6   611.29 HCD   hcd Positive True True 33 221  17.86 FTMS + c NSI..."},
                {20506, "2 2  1488 41.90 110 1111 6.9E+6  207.112 6.4E+5   550.31 HCD   hcd Positive True True 33 221  10.58 FTMS + c NSI..."},
                {20507, "1 1 14016 41.91 350 1800 1.2E+9  554.304 9.0E+7     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20508, "2 2  1296 41.91 110 1253 3.9E+6  887.511 4.0E+5   621.35 HCD   hcd Positive True True 33 221  14.21 FTMS + c NSI..."},
                {20509, "2 2  1776 41.91 110 1075 6.0E+6  445.242 5.1E+5   532.29 HCD   hcd Positive True True 33 221  14.14 FTMS + c NSI..."},
                {20510, "1 1 14184 41.91 350 1800 1.3E+9  554.304 9.4E+7     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20511, "2 2  2224 41.91 110  958 1.1E+7  120.081 8.8E+5   473.77 HCD   hcd Positive True True 33 221  12.22 FTMS + c NSI..."},
                {20512, "2 2  1568 41.91 110 1401 8.4E+6  891.457 1.5E+6   695.36 HCD   hcd Positive True True 33 221  12.01 FTMS + c NSI..."},
                {20513, "2 2  2112 41.91 110  926 5.1E+6  777.422 4.6E+5   457.74 HCD   hcd Positive True True 33 221  20.47 FTMS + c NSI..."},
                {20514, "1 1 14804 41.91 350 1800 1.4E+9  554.305 1.0E+8     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20515, "2 2   928 41.91 110 1730 8.0E+6  859.948 3.1E+6   859.94 HCD   hcd Positive True True 33 221  10.05 FTMS + c NSI..."},
                {20516, "1 1 14232 41.92 350 1800 1.4E+9  554.305 1.1E+8     0.00 CID       Positive False True 33 221   0.09 FTMS + p NSI..."},
                {20517, "2 2  1792 41.92 110 1339 4.2E+6  697.397 3.0E+5   442.91 HCD   hcd Positive True True 33 221  14.09 FTMS + c NSI..."},
                {20518, "2 2  1216 41.92 110 2000 3.7E+6  999.457 3.8E+5   737.69 HCD   hcd Positive True True 33 221  12.55 FTMS + c NSI..."},
                {20519, "2 2  2144 41.92 110 1241 8.9E+6  742.409 5.7E+5   615.27 HCD   hcd Positive True True 33 221  12.77 FTMS + c NSI..."},
                {20520, "1 1 14428 41.92 350 1800 1.7E+9  554.305 1.3E+8     0.00 CID       Positive False True 33 221   0.08 FTMS + p NSI..."}

            };
            expectedData.Add("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", file3Data);

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan info for {0}", dataFile.Name);
                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19}",
                                  "Scan", "MSLevel", "Event",
                                  "NumPeaks", "RetentionTime",
                                  "LowMass", "HighMass", "TotalIonCurrent",
                                  "BasePeakMZ", "BasePeakIntensity",
                                  "ParentIonMZ", "ActivationType", "CollisionMode",
                                  "IonMode", "IsCentroided", "IsFTMS",
                                  "ScanEvents.Count", "StatusLog.Count",
                                  "IonInjectionTime", "FilterText");

                var scanCountMS1 = 0;
                var scanCountMS2 = 0;

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                    double ionInjectionTime;

                    if (scanInfo.TryGetScanEvent("Ion Injection Time (ms)", out var injectionTimeText, true))
                    {
                        double.TryParse(injectionTimeText, out ionInjectionTime);
                    }
                    else
                    {
                        ionInjectionTime = 0;
                    }

                    var scanSummary =
                        string.Format(
                            "{0} {1} {2} {3,5} {4:F2} {5,3:0} {6,4:0} {7:0.0E+0} {8,8:F3} {9:0.0E+0} {10,8:F2} {11} {12,5} {13} {14} {15} {16} {17} {18,6:F2} {19}",
                            scanInfo.ScanNumber, scanInfo.MSLevel, scanInfo.EventNumber,
                            scanInfo.NumPeaks, scanInfo.RetentionTime,
                            scanInfo.LowMass, scanInfo.HighMass,
                            scanInfo.TotalIonCurrent, scanInfo.BasePeakMZ, scanInfo.BasePeakIntensity, scanInfo.ParentIonMZ,
                            scanInfo.ActivationType, scanInfo.CollisionMode,
                            scanInfo.IonMode, scanInfo.IsCentroided,
                            scanInfo.IsFTMS, scanInfo.ScanEvents.Count, scanInfo.StatusLog.Count, ionInjectionTime,
                            scanInfo.FilterText.Substring(0, 12) + "...");

                    Console.WriteLine(scanSummary);

                    if (scanInfo.MSLevel > 1)
                        scanCountMS2++;
                    else
                        scanCountMS1++;

                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }

                    if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
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
        [TestCase("B5_50uM_MS_r1.RAW", 1, 20)]
        [TestCase("MNSLTFKK_ms.raw", 1, 88)]
        [TestCase("QCShew200uL.raw", 4000, 4100)]
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
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                    foreach (var mrmRange in scanInfo.MRMInfo.MRMMassList)
                    {
                        var mrmRangeKey =
                            mrmRange.StartMass.ToString("0.0") + "_" +
                            mrmRange.CentralMass.ToString("0.0") + "_" +
                            mrmRange.EndMass.ToString("0.0");

                        if (mrmRangeCountsActual.TryGetValue(mrmRangeKey, out var observedScanCount))
                        {
                            mrmRangeCountsActual[mrmRangeKey] = observedScanCount + 1;
                        }
                        else
                        {
                            mrmRangeCountsActual.Add(mrmRangeKey, 1);
                        }
                    }

                    Assert.IsTrue(mrmRangeCountsActual.Count == 1, "Found {0} MRM scan ranges; expected to only find 1", mrmRangeCountsActual.Count);
                }

                if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedMRMInfo))
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
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        public void TestGetScanInfoStruct(string rawFileName, int scanStart, int scanEnd)
        {

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

                Console.WriteLine("Checking GetScanInfo initializing from a struct using {0}", dataFile.Name);

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

#pragma warning disable 618
                    success = reader.GetScanInfo(scanNumber, out udtScanHeaderInfoType scanInfoStruct);
#pragma warning restore 618
                    Assert.IsTrue(success, "GetScanInfo (struct) returned false for scan {0}", scanNumber);

                    Assert.AreEqual(scanInfoStruct.MSLevel, scanInfo.MSLevel);
                    Assert.AreEqual(scanInfoStruct.IsCentroidScan, scanInfo.IsCentroided);
                    Assert.AreEqual(scanInfoStruct.FilterText, scanInfo.FilterText);
                    Assert.AreEqual(scanInfoStruct.BasePeakIntensity, scanInfo.BasePeakIntensity, 0.0001);
                    Assert.AreEqual(scanInfoStruct.TotalIonCurrent, scanInfo.TotalIonCurrent, 0.0001);

                }
            }

        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165)]
        [TestCase("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08.raw", 3101, 3102)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 4371, 4373)]
        [TestCase("Lewy2_19Ct1_2Nov13_Samwise_13-07-28.raw", 22010, 22014)]
        public void TestGetScanData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the scan number of data being retrieved
            var file1Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file1Data, 1513, 1517);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",  "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False",  "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_False",  "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_False",  "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_False",  "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("0_True",   "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True",   "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_True",   "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_True",   "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_True",   "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_False", "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False", "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_False", "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_False", "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_False", "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_True",  "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True",  "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_True",  "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_True",  "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_True",  "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);


            var file2Data = new Dictionary<int, Dictionary<string, string>>
            {
                {16121, new Dictionary<string, string>()},
                {16122, new Dictionary<string, string>()},
                {16126, new Dictionary<string, string>()},
                {16131, new Dictionary<string, string>()},
                {16133, new Dictionary<string, string>()},
                {16141, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False",  "11888    11888     346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False",  "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_False",  "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_False",  "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_False",  "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_False",  "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("0_True",   "  833      833     351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True",   "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_True",   "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_True",   "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_True",   "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_True",   "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_False", "   50       50     503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_False", "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_False", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_False", "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_False", "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_True",  "   50       50     371.733   6.2E+6  681.010   6.2E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True",  "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_True",  "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_True",  "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_True",  "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_True",  "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var file3Data = new Dictionary<int, Dictionary<string, string>>
            {
                {3101, new Dictionary<string, string>()},
                {3102, new Dictionary<string, string>()},
                {16126, new Dictionary<string, string>()},
                {16131, new Dictionary<string, string>()},
                {16133, new Dictionary<string, string>()},
                {16141, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file3Data[3101].Add("0_False",  "19200    19200     400.083   1.7E+3 1200.083  5.2E-23  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_False",  "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("0_True",   "  906      906     400.389   1.5E+4  760.724   3.9E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_True",   "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_False", "   50       50     500.333   4.8E+4  555.250   4.2E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_False", "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_True",  "   50       50     423.593   1.1E+5  596.215   9.5E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_True",  "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");

            expectedData.Add("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08", file3Data);


            var file4Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file4Data, 4371, 4373);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file4Data[4371].Add("0_False",  " 9271     9271     200.000   0.0E+0  597.504   0.0E+0  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_False",  "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_False",  "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("0_True",   "  691      691     200.052   4.7E+2  600.505   7.1E+2  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_True",   "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_True",   "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_False", "   50       50     324.984   4.3E+4  447.116   8.4E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_False", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_False", "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_True",  "   50       50     217.018   4.9E+3  449.337   1.3E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_True",  "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_True",  "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");

            expectedData.Add("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925", file4Data);


            var file5Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file5Data, 22010, 22014);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file5Data[22010].Add("0_False",  "35347    35347     396.014   0.0E+0  642.910   5.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_False",  " 3910     3910      92.733   0.0E+0  262.166   9.7E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_False",  "34829    34829     396.014   0.0E+0  639.990   5.6E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_False",  " 3756     3756      99.003   0.0E+0  244.134   1.7E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_False",  " 3403     3403     140.253   0.0E+0  367.176   0.0E+0  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("0_True",   " 2500     2500     401.286   5.6E+5  644.143   2.2E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_True",   "  262      262     101.071   5.3E+4  267.660   8.2E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_True",   " 2444     2444     400.264   3.8E+5  638.584   6.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_True",   "  271      271     101.071   9.8E+4  244.166   3.3E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_True",   "  236      236     142.062   2.1E+4  361.153   1.8E+4  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_False", "   50       50     469.269   1.8E+8  495.792   6.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_False", "   50       50     110.071   2.1E+5  183.153   5.6E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_False", "   50       50     469.269   2.5E+8  495.789   4.6E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_False", "   50       50     110.070   3.4E+5  169.100   4.5E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_False", "   50       50     147.112   3.0E+5  687.427   2.8E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_True",  "   50       50     469.272   2.7E+8  606.977   9.0E+7  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_True",  "   50       50     102.055   7.1E+4  233.165   1.1E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_True",  "   50       50     469.272   3.6E+8  606.643   1.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_True",  "   50       50     102.055   1.1E+5  218.150   1.4E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_True",  "   50       50     147.113   3.4E+5  428.252   1.4E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");

            expectedData.Add("Lewy2_19Ct1_2Nov13_Samwise_13-07-28", file5Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

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

                    if (iteration == 1)
                    {
                        Console.WriteLine("Scan data for {0}", dataFile.Name);
                        Console.WriteLine("{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-9} {8,-7} {9}",
                                          "Scan", "Max#", "Centroid", "MzCount", "IntCount",
                                          "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");
                    }

                    for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                    {
                        var dataPointsRead = reader.GetScanData(scanNumber, out var mzList, out var intensityList, maxNumberOfPeaks, centroidData);

                        Assert.IsTrue(dataPointsRead > 0, "GetScanData returned 0 for scan {0}", scanNumber);

                        Assert.AreEqual(dataPointsRead, mzList.Length, "Data count mismatch vs. function return value");

                        var midPoint = (int)(intensityList.Length / 2f);

                        var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                        Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                        var scanSummary =
                            // "{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10:F3} {6,-8:0.0E+0} {7,-10:F3} {8,8:0.0E+0}  {9}"
                            string.Format(
                                "{0,5} {1,-3} {2,8} {3,8} {4,8}    {5,8:F3} {6,8:0.0E+0} {7,8:F3} {8,8:0.0E+0}  {9}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                mzList.Length, intensityList.Length,
                                mzList[0], intensityList[0],
                                mzList[midPoint], intensityList[midPoint],
                                scanInfo.FilterText);

                        Console.WriteLine(scanSummary);

                        if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                        {
                            Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                        }

                        if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedDataByType))
                        {
                            var keySpec = maxNumberOfPeaks + "_" + centroidData;
                            if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
                            {
                                Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22),
                                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
                            }
                        }
                    }
                }
            }

        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16122)]
        [TestCase("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08.raw", 3101, 3102)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 4371, 4372)]
        public void TestGetScanData2D(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the scan number of data being retrieved
            var file1Data = new Dictionary<int, Dictionary<string, string>>
            {
                {1513, new Dictionary<string, string>()},
                {1514, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",   "  851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False",   "  109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("0_True",    "  851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True",    "  109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_False",  "   50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False",  "   50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_True",   "   50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True",   "   50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);


            var file2Data = new Dictionary<int, Dictionary<string, string>>
            {
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
            file2Data[16121].Add("50_True",  "   50  371.733   6.2E+6  681.010   6.2E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True",  "   50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);


            var file3Data = new Dictionary<int, Dictionary<string, string>>
            {
                {3101, new Dictionary<string, string>()},
                {3102, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file3Data[3101].Add("0_False",  "19200  400.083   1.7E+3 1200.083  5.2E-23  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_False",  "  329  147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("0_True",   "  906  400.389   1.5E+4  760.724   3.9E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_True",   "  329  147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_False", "   50  500.333   4.8E+4  555.250   4.2E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_False", "   50  147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_True",  "   50  423.593   1.1E+5  596.215   9.5E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_True",  "   50  147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");

            expectedData.Add("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08", file2Data);


            var file4Data = new Dictionary<int, Dictionary<string, string>>
            {
                {4371, new Dictionary<string, string>()},
                {4372, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file4Data[4371].Add("0_False",  " 9271  200.000   0.0E+0  597.504   0.0E+0  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_False",  "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("0_True",   "  691  200.052   4.7E+2  600.505   7.1E+2  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_True",   "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("50_False", "   50  324.984   4.3E+4  447.116   8.4E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_False", "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("50_True",  "   50  217.018   4.9E+3  449.337   1.3E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_True",  "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");

            expectedData.Add("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925", file4Data);


            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                Console.WriteLine("Scan data for {0}", dataFile.Name);
                Console.WriteLine("{0,5} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
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
                        var dataPointsRead = reader.GetScanData2D(scanNumber, out var massIntensityPairs, maxNumberOfPeaks, centroidData);

                        Assert.IsTrue(dataPointsRead > 0, "GetScanData2D returned 0 for scan {0}", scanNumber);

                        var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                        Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                        var lastIndex = massIntensityPairs.GetUpperBound(1);

                        int dataCount;

                        if (maxNumberOfPeaks > 0)
                        {
                            dataCount = maxNumberOfPeaks;
                            if (dataPointsRead < maxNumberOfPeaks)
                            {
                                dataCount = dataPointsRead;
                            }

                            // Make sure the 2D array has values of 0 for mass and intensity beyond index maxNumberOfPeaks
                            for (var dataIndex = maxNumberOfPeaks; dataIndex < lastIndex; dataIndex++)
                            {
                                if (massIntensityPairs[0, dataIndex] > 0)
                                {
                                    Console.WriteLine("Non-zero m/z value found at index {0} for scan {1}", dataIndex, scanNumber);
                                    Assert.AreEqual(0, massIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                                }

                                if (massIntensityPairs[1, dataIndex] > 0)
                                {
                                    Console.WriteLine("Non-zero intensity value found at index {0} for scan {1}", dataIndex, scanNumber);
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
                                "{0,5} {1,3} {2,8} {3,8} {4,8:F3} {5,8:0.0E+0} {6,8:F3} {7,8:0.0E+0}  {8}",
                                scanNumber, maxNumberOfPeaks, centroidData,
                                dataCount,
                                massIntensityPairs[0, 0], massIntensityPairs[1,0],
                                massIntensityPairs[0, midPoint], massIntensityPairs[1, midPoint],
                                scanInfo.FilterText);

                        Console.WriteLine(scanSummary);

                        if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                        {
                            Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                        }

                        if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedDataByType))
                        {
                            var keySpec = maxNumberOfPeaks + "_" + centroidData;
                            if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
                            {
                                Assert.AreEqual(expectedDataDetails, scanSummary.Substring(22),
                                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
                            }
                        }
                    }
                }
            }

        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165)]
        public void TestGetScanDataSumScans(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the start scan for summing
            var file1Data = new Dictionary<int, Dictionary<string, string>>
            {
                {1513, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False",  "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("0_True",   "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_False", "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_True",  "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, Dictionary<string, string>>
            {
                {16121, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False",  "26057  346.518   0.0E+0  753.312   8.7E+0  FTMS + p NSI Full ms [350.0000-1550.0000]");
          //file2Data[16121].Add("0_True",   "  818  351.230   3.2E+5  820.778   2.3E+5  FTMS + p NSI Full ms [350.0000-1550.0000]"); // MSFileReader number; bad centroids
            file2Data[16121].Add("0_True",   " 1786  351.231   9.5E+4  758.261   1.4E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("50_False", "   50  503.553   1.2E+7  521.201   1.6E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
          //file2Data[16121].Add("50_True",  "   50  371.885   2.6E+7  650.717   9.8E+6  FTMS + p NSI Full ms [350.0000-1550.0000]"); // MSFileReader number; bad centroids
            file2Data[16121].Add("50_True",  "   50  371.733   4.4E+6  691.981   9.9E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");

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

                    var dataPointsRead = reader.GetScanDataSumScans(scanStart, scanEnd, out var massIntensityPairs, maxNumberOfPeaks, centroidData);

                    Assert.IsTrue(dataPointsRead > 0, string.Format("GetScanDataSumScans returned 0 summing scans {0} to {1}", scanStart, scanEnd));

                    var success = reader.GetScanInfo(scanStart, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanStart);

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
                                Console.WriteLine("Non-zero m/z value found at index {0} for scan {1}", dataIndex, scanStart);
                                Assert.AreEqual(0, massIntensityPairs[0, dataIndex], "Non-zero m/z value found in 2D array beyond expected index");
                            }

                            if (massIntensityPairs[1, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero intensity value found at index {0} for scan {1}", dataIndex, scanStart);
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
                            "{0} {1,3} {2,8} {3,8} {4,8:F3} {5,8:0.0E+0} {6,8:F3} {7,8:0.0E+0}  {8}",
                            scanStart, maxNumberOfPeaks, centroidData,
                            dataCount,
                            massIntensityPairs[0, 0], massIntensityPairs[1, 0],
                            massIntensityPairs[0, midPoint], massIntensityPairs[1, midPoint],
                            scanInfo.FilterText);


                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }

                    if (expectedDataThisFile.TryGetValue(scanStart, out var expectedDataByType))
                    {
                        var keySpec = maxNumberOfPeaks + "_" + centroidData;
                        if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
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
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanLabelData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            var noMatch = "  0                                                        ";

            var file1Data = new Dictionary<int, string>
            {
                {1513, noMatch + "+ c ESI Full ms [400.00-2000.00]"},
                {1514, noMatch + "+ c d Full ms2 884.41@cid45.00 [230.00-1780.00]"}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string>
            {
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

                    var dataPointsRead = reader.GetScanLabelData(scanNumber, out var ftLabelData);

                    if (dataPointsRead == -1)
                        Assert.AreEqual(0, ftLabelData.Length, "Data count mismatch vs. function return value");
                    else
                        Assert.AreEqual(dataPointsRead, ftLabelData.Length, "Data count mismatch vs. function return value");

                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanStart);

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

                        scanSummary = string.Format("{0} {1,3} {2,8:F3} {3,8:0.0E+0} {4,8:F3} {5,8:F3} {6,8:F3} {7,8:0}  {8}",
                                scanNumber, ftLabelData.Length,
                                ftLabelData[midPoint].Mass,
                                ftLabelData[midPoint].Intensity,
                                ftLabelData[midPoint].Resolution,
                                ftLabelData[midPoint].Baseline,
                                ftLabelData[midPoint].Noise,
                                ftLabelData[midPoint].Charge,
                                scanInfo.FilterText);
                    }

                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }

                    if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
                    {
                        Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary,
                                        "Scan summary mismatch, scan " + scanNumber);
                    }

                    Console.WriteLine(scanSummary);
                }
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanPrecisionData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            var noMatch = "  0                                               ";

            var file1Data = new Dictionary<int, string>
            {
                {1513, noMatch + "+ c ESI Full ms [400.00-2000.00]"},
                {1514, noMatch + "+ c d Full ms2 884.41@cid45.00 [230.00-1780.00]"}
            };
            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, string>
            {
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

                    var dataPointsRead = reader.GetScanPrecisionData(scanNumber, out var massResolutionData);

                    if (dataPointsRead == -1)
                        Assert.AreEqual(0, massResolutionData.Length, "Data count mismatch vs. function return value");
                    else
                        Assert.AreEqual(dataPointsRead, massResolutionData.Length, "Data count mismatch vs. function return value");

                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanStart);

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

                        scanSummary = string.Format("{0} {1,3} {2,8:F3} {3,8:0.0E+0} {4,8:F3} {5,8:F3} {6,8:F3}  {7}",
                                scanNumber, massResolutionData.Length,
                                massResolutionData[midPoint].Mass,
                                massResolutionData[midPoint].Intensity,
                                massResolutionData[midPoint].Resolution,
                                massResolutionData[midPoint].AccuracyMMU,
                                massResolutionData[midPoint].AccuracyPPM,
                                scanInfo.FilterText);
                    }

                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                    {
                        Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
                    }

                    if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
                    {
                        Assert.AreEqual(scanNumber + " " + expectedScanSummary, scanSummary,
                                        "Scan summary mismatch, scan " + scanNumber);
                    }

                    Console.WriteLine(scanSummary);
                }
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 45000, 45200)]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw", 15000, 15006)]
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

            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "0.063", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "0.068", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "0.075", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "0.078", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "9.588", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "10.863", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Ion Injection Time (ms):", "12.628", 1);


            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Orbitrap Resolution:", "60000", 4);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "Orbitrap Resolution:", "7500", 3);

            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "HCD Energy:", "-1.00", 4);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "HCD Energy:", "-26.93", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "HCD Energy:", "-42.03", 1);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "HCD Energy:", "-24.22", 1);

            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "MS2 Isolation Width:", "-1.00", 4);
            AddExpectedTupleAndCount(expectedData, "QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", "MS2 Isolation Width:", "2.00", 3);


            var dataFile = GetRawDataFile(rawFileName);

            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedEventsThisFile))
            {
                Assert.Fail("Dataset {0} not found in dictionary expectedData", dataFile.Name);
            }

            var eventsToCheck = (from item in expectedEventsThisFile select item.Key.Item1).Distinct().ToList();
            var eventCountsActual = new Dictionary<Tuple<string, string>, int>();

            using (var reader = new XRawFileIO(dataFile.FullName))
            {
                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

                    foreach (var eventName in eventsToCheck)
                    {
                        scanInfo.TryGetScanEvent(eventName, out var eventValue);

                        var eventKey = new Tuple<string, string>(eventName, eventValue);
                        if (eventCountsActual.TryGetValue(eventKey, out var scanCount))
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
                    if (expectedEventsThisFile.TryGetValue(observedEvent.Key, out var expectedScanCount))
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
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        [TestCase("MZ20150721blank2.raw", 300, 400)]
        [TestCase("B5_50uM_MS_r1.RAW", 1, 20)]
        public void TestScanInfoCopyFromStruct(string rawFileName, int scanStart, int scanEnd)
        {

            var dataFile = GetRawDataFile(rawFileName);

            using (var reader = new XRawFileIO(dataFile.FullName))
            {

                Console.WriteLine("Checking clsScanInfo initializing from a struct using {0}", dataFile.Name);

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var success = reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo);

                    Assert.IsTrue(success, "GetScanInfo returned false for scan {0}", scanNumber);

#pragma warning disable 618
                    var udtScanHeaderInfo = new udtScanHeaderInfoType
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
#pragma warning restore 618

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

#pragma warning disable 618
                    var scanInfoFromStruct = new clsScanInfo(scanInfo.ScanNumber, udtScanHeaderInfo);
#pragma warning restore 618

                    Assert.AreEqual(scanInfoFromStruct.MSLevel, scanInfo.MSLevel);
                    Assert.AreEqual(scanInfoFromStruct.IsCentroided, scanInfo.IsCentroided);
                    Assert.AreEqual(scanInfoFromStruct.FilterText, scanInfo.FilterText);
                    Assert.AreEqual(scanInfoFromStruct.BasePeakIntensity, scanInfo.BasePeakIntensity, 0.0001);
                    Assert.AreEqual(scanInfoFromStruct.TotalIonCurrent, scanInfo.TotalIonCurrent, 0.0001);

                }
            }

        }

        private void AddEmptyDictionaries(IDictionary<int, Dictionary<string, string>> fileData, int scanStart, int scanEnd)
        {
            for (var scanNum = scanStart; scanNum <= scanEnd; scanNum++)
            {
                fileData.Add(scanNum, new Dictionary<string, string>());
            }
        }

        private void AddExpectedTupleAndCount(
            IDictionary<string, Dictionary<Tuple<string, string>, int>> expectedData,
            string fileName,
            string tupleKey1,
            string tupleKey2,
            int scanCount)
        {
            if (!expectedData.TryGetValue(fileName, out var expectedScanInfo))
            {
                expectedScanInfo = new Dictionary<Tuple<string, string>, int>();
                expectedData.Add(fileName, expectedScanInfo);
            }

            expectedScanInfo.Add(new Tuple<string, string>(tupleKey1, tupleKey2), scanCount);
        }

        private FileInfo GetRawDataFile(string rawFileName)
        {
            var localDirPath = Path.Combine("..", "..", "Docs");
            var remoteDirPath = @"\\proto-2\UnitTest_Files\ThermoRawFileReader";

            var localFile = new FileInfo(Path.Combine(localDirPath, rawFileName));

            if (localFile.Exists)
            {
                return localFile;
            }

            // Look for the file on Proto-2
            var remoteFile = new FileInfo(Path.Combine(remoteDirPath, rawFileName));
            if (remoteFile.Exists)
            {
                return remoteFile;
            }

            var msg = string.Format("File not found: {0}; checked in both {1} and {2}", rawFileName, localDirPath, remoteDirPath);

            Console.WriteLine(msg);
            Assert.Fail(msg);

            return null;
        }
    }
}
