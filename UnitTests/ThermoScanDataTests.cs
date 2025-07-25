﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PRISM;
using ThermoRawFileReader;

namespace RawFileReaderTests
{
    [TestFixture]
    public class ThermoScanDataTests
    {
        // Ignore Spelling: Andro, Angiotensin, cid, ETciD, etd, EThcD, fst, Gimli, hcd, histone
        // Ignore Spelling: LCQa, Legolas, lowdose, mnx, Orbitrap, QC_Mam, sa, Samwise, Wrighton, XRawfile
        // Ignore Spelling: True True

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW")]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw")]
        [TestCase("HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw")]
        [TestCase("MZ0210MnxEF889ETD.raw")]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw")]
        [TestCase("Blank04_29Mar17_Smeagol.raw")]
        public void TestGetCollisionEnergy(string rawFileName)
        {
            // Keys in this Dictionary are filename (without the extension), values are Collision Energies by scan
            var expectedData = new Dictionary<string, Dictionary<int, List<double>>>();

            var ce30 = new List<double> { 30.00 };
            var ce45 = new List<double> { 45.00 };
            var ce20_120 = new List<double> { 20.00, 120.550003 };
            var ce120 = new List<double> { 120.550003 };
            var ms1Scan = new List<double>();
            var srmScan = new List<double> { 0 };

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
                {27807, ce30}
            };

            expectedData.Add("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01", file5Data);

            var file6Data = new Dictionary<int, List<double>>
            {
                {3200, srmScan},
                {3201, srmScan},
                {3202, srmScan},
                {3203, srmScan},
                {3204, srmScan},
                {3205, srmScan},
                {3206, srmScan},
                {3207, srmScan},
                {3208, srmScan},
                {3209, srmScan},
            };

            expectedData.Add("Blank04_29Mar17_Smeagol", file6Data);

            var dataFile = GetRawDataFile(rawFileName);

            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var collisionEnergiesThisFile))
            {
                Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
            }

            // Keys are scan number, values are the list of collision energies
            var collisionEnergiesActual = new Dictionary<int, List<double>>();

            // Keys are scan number, values are msLevel
            var msLevelsActual = new Dictionary<int, int>();

            // Keys are scan number, values are the ActivationType, for example cid, etd, hcd
            var activationTypesActual = new Dictionary<int, string>();

            using var reader = new XRawFileIO(dataFile.FullName);

            foreach (var scanNumber in collisionEnergiesThisFile.Keys)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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

                        Assert.That(isValid, Is.True, $"Unexpected collision energy {actualEnergy:F2} for scan {scanNumber}");
                    }
                }

                if (expectedEnergies.Count != actualEnergiesOneScan.Value.Count)
                {
                    var msg = string.Format("Collision energy count mismatch for scan {0}", scanNumber);
                    Console.WriteLine(msg);
                    Assert.That(actualEnergiesOneScan.Value, Has.Count.EqualTo(expectedEnergies.Count), msg);
                }
            }
        }

        [Test]
        [TestCase("blank_MeOH-3_18May16_Rainier_Thermo_10344958.raw", 1500, 1900, 190, 211, 0, 0)]
        [TestCase("Corrupt_QC_Shew_13_04_pt1_a_5Sep13_Cougar_13-06-14.raw", 0, -1, -1, 0, 0, 0)]
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
                using var reader = new XRawFileIO(dataFile.FullName);

                var scanCount = reader.GetNumScans();
                Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);

                using (Assert.EnterMultipleScope())
                {
                    if (expectedMS1 == -1 && expectedMS2 == 0)
                    {
                        Assert.That(reader.FileInfo.CorruptFile, Is.True, "CorruptFile is false while we expected it to be true (a)");
                        Assert.That(scanCount, Is.EqualTo(-1), "ScanCount is not -1");
                    }
                    else if (expectedMS1 + expectedMS2 == 0)
                    {
                        Assert.That(reader.FileInfo.CorruptFile, Is.True, "CorruptFile is false while we expected it to be true (b)");
                        Assert.That(scanCount, Is.LessThanOrEqualTo(0), "ScanCount is non-zero, while we expected it to be 0");
                    }
                    else
                    {
                        Assert.That(reader.FileInfo.CorruptFile, Is.False, "CorruptFile is true while we expected it to be false (c)");
                        Assert.That(scanCount, Is.GreaterThan(0), "ScanCount is zero, while we expected it to be > 0");
                    }
                }

                var scanCountMS1 = 0;
                var scanCountMS2 = 0;

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    try
                    {
                        reader.GetScanInfo(scanNumber, out var scanInfo);

                        if (reader.FileInfo.CorruptFile)
                        {
                            Assert.That(string.IsNullOrEmpty(scanInfo.FilterText), Is.True, "FilterText is not empty but should be since corrupt file");
                        }
                        else
                        {
                            Assert.That(string.IsNullOrEmpty(scanInfo.FilterText), Is.False, "FilterText is empty but should not be");

                            if (scanInfo.MSLevel > 1)
                                scanCountMS2++;
                            else
                                scanCountMS1++;
                        }

                        // Note: this function call will fail randomly with file Corrupt_Scans6920-7021_AID_STM_013_101104_06_LTQ_16Nov04_Earth_0904-8.raw
                        // Furthermore, we are unable to catch the exception that occurs (or no exception is thrown) and adding
                        // [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions] to the function does not help
                        var dataPointCount = reader.GetScanData(scanNumber, out var mzList, out var intensityList);

                        using (Assert.EnterMultipleScope())
                        {
                            if (reader.FileInfo.CorruptFile)
                            {
                                Assert.That(dataPointCount, Is.Zero, $"GetScanData unexpectedly reported a non-zero data count for scan {scanNumber}");
                                Assert.That(mzList, Has.Length.Zero, $"GetScanData unexpectedly returned m/z data for scan {scanNumber}");
                                Assert.That(intensityList, Has.Length.Zero, $"GetScanData unexpectedly returned intensity data for scan {scanNumber}");
                            }
                            else
                            {
                                if (dataPointCount == 0)
                                {
                                    Console.WriteLine("Corrupt scan encountered: {0}", scanNumber);

                                    Assert.That(scanNumber, Is.InRange(corruptScanStart, corruptScanEnd), $"Unexpected corrupt scan found, scan {scanNumber}");
                                    Assert.That(mzList, Has.Length.Zero, $"GetScanData unexpectedly returned m/z data for scan {scanNumber}");
                                    Assert.That(intensityList, Has.Length.Zero, $"GetScanData unexpectedly returned intensity data for scan {scanNumber}");
                                }
                                else
                                {
                                    Assert.That(dataPointCount, Is.GreaterThan(0), $"GetScanData reported a data point count of 0 for scan {scanNumber}");
                                    Assert.That(mzList, Has.Length.GreaterThan(0), $"GetScanData unexpectedly returned no m/z data for scan {scanNumber}");
                                    Assert.That(intensityList, Has.Length.GreaterThan(0), $"GetScanData unexpectedly returned no intensity data for scan {scanNumber}");
                                    Assert.That(mzList, Has.Length.EqualTo(intensityList.Length), $"Array length mismatch for m/z and intensity data for scan {scanNumber}");
                                    Assert.That(mzList, Has.Length.EqualTo(dataPointCount), $"Array length does not agree with dataPointCount for scan {scanNumber}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception reading scan {0}: {1}", scanNumber, ex.Message);
                        Assert.Fail($"Exception reading scan {scanNumber}");
                    }
                }

                Console.WriteLine("scanCountMS1={0}", scanCountMS1);
                Console.WriteLine("scanCountMS2={0}", scanCountMS2);

                if (expectedMS1 >= 0)
                    Assert.That(scanCountMS1, Is.EqualTo(expectedMS1), "MS1 scan count mismatch");

                if (expectedMS2 >= 0)
                    Assert.That(scanCountMS2, Is.EqualTo(expectedMS2), "MS2 scan count mismatch");
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

            using var reader = new XRawFileIO(dataFile.FullName);

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
                    Console.WriteLine(
                        "{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10} {6,-8} {7,-10} {8,-8}  {9}",
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

                    Assert.That(unsortedMzValues, Is.Zero, $"Scan {scanNumber} has {unsortedMzValues} m/z values not sorted properly");

                    scansProcessed++;

                    if (scansProcessed % statsInterval == 0)
                    {
                        reader.GetScanInfo(scanNumber, out var scanInfo);

                        if (mzList.Length > 0)
                        {
                            var midIndex = (int)Math.Floor(mzList.Length / 2.0);

                            Console.WriteLine(
                                "{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10:0.0000} {6,-8:0.0} {7,-10:0.0000} {8,-8:0.0}  {9}",
                                scanNumber, maxNumberOfPeaks, centroidData, mzList.Length, intensityList.Length,
                                mzList[0], intensityList[0], mzList[midIndex], intensityList[midIndex], scanInfo.FilterText);
                        }
                        else
                        {
                            Console.WriteLine(
                                "{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-10} {6,-8} {7,-10} {8,-8}  {9}",
                                scanNumber, maxNumberOfPeaks, centroidData, mzList.Length, intensityList.Length,
                                "n/a", "n/a", "n/a", "n/a", scanInfo.FilterText);
                        }
                    }
                }
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 500, 525, "", "502, 503, 504", "", "", "", "506, 507, 508", "", "", "", "510, 511, 512", "", "", "", "514, 515, 516", "", "", "", "518, 519, 520", "", "", "", "522, 523, 524", "", "", "", "526, 527, 528")]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 500, 525, "", "", "", "505, 506", "508, 509", "", "", "511, 512, 513, 514", "", "", "516 ", "", "", "", "", "518, 519, 520", "", "", "", "", "", "523 ", "525, 526", "", "528, 529", "")]
        [TestCase("Angiotensin_325-CID.raw", 1, 5, "", "", "", "", "")]
        [TestCase("Angiotensin_AllScans.raw", 510, 525, "", "513, 514, 515", "516, 517, 518", "", "", "", "", "", "", "541, 542, 543, 553, 554", "523, 524, 525", "526, 527, 528", "529, 530, 531", "", "", "")]
        public void TestGetDependentScans(string rawFileName, int startScan, int endScan, params string[] expectedDependentScans)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            var i = 0;
            var validScanCount = 0;

            for (var scanNumber = startScan; scanNumber <= endScan; scanNumber++)
            {
                if (!reader.GetScanInfo(scanNumber, out var scanInfo))
                {
                    ConsoleMsgUtils.ShowWarning("Invalid scan number: {0}", scanNumber);
                    i++;
                    continue;
                }

                validScanCount++;

                var dependentScanList = string.Join(", ", scanInfo.DependentScans);

                Console.WriteLine("MS{0} scan {1,-4} has dependent scans: {2,-4}", scanInfo.MSLevel, scanNumber, dependentScanList);

                if (i < expectedDependentScans.Length && !string.IsNullOrWhiteSpace(expectedDependentScans[i]))
                {
                    var scansToMatch = expectedDependentScans[i].Split(',');

                    if (scansToMatch.Length == 0)
                        break;

                    for (var j = 0; j < scansToMatch.Length; j++)
                    {
                        var scanToMatch = int.Parse(scansToMatch[j]);

                        Assert.That(
                            scanInfo.DependentScans[j], Is.EqualTo(scanToMatch),
                            $"Dependent scan does not match expected value: {scanToMatch}");
                    }
                }

                i++;
            }

            var percentValid = validScanCount / (double)(endScan - startScan + 1) * 100;
            Assert.That(percentValid, Is.GreaterThan(90), "Over 10% of the spectra had invalid scan numbers");
        }

        // ReSharper disable StringLiteralTypo

        [Test]
        [TestCase("B5_50uM_MS_r1.RAW", 1, 20)]
        [TestCase("MNSLTFKK_ms.raw", 1, 88)]
        [TestCase("QCShew200uL.raw", 4000, 4100)]
        [TestCase("Wrighton_MT2_SPE_200avg_240k_neg_330-380.raw", 1, 200)]
        [TestCase("1229_02blk1.raw", 6000, 6100)]
        [TestCase("MCF7_histone_32_49B_400min_HCD_ETD_01172014_b.raw", 2300, 2400)]
        [TestCase("lowdose_IMAC_iTRAQ1_PQDMSA.raw", 15000, 15100)]
        [TestCase("MZ20150721blank2.raw", 1, 434)]
        [TestCase("OG_CEPC_PU_22Oct13_Legolas_13-05-12.raw", 5000, 5100)]
        [TestCase("blank_MeOH-3_18May16_Rainier_Thermo_10344958.raw", 1500, 1900)]
        [TestCase("HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53.raw", 25200, 25600)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 5900, 6000)]
        [TestCase("IPA-blank-07_25Oct13_Gimli.raw", 1750, 1850)]
        [TestCase("Angiotensin_325-CID.raw", 1, 10)]
        [TestCase("Angiotensin_325-ETciD-15.raw", 1, 10)]
        [TestCase("Angiotensin_325-ETD.raw", 1, 10)]
        [TestCase("Angiotensin_325-HCD.raw", 1, 10)]
        [TestCase("Angiotensin_AllScans.raw", 1000, 1200)]
        [TestCase("QC_mam_16_01_125ng_CPTACpt7-3s-a_02Nov17_Pippin_REP-17-10-01.raw", 65, 80)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 2500, 2600)] // SRM data
        [TestCase("20181115_arginine_Gua13C_CIDcol25_158_HCDcol35.raw", 10, 20)]                                               // MS3 scans
        [TestCase("calmix_Q3_10192022_03.RAW", 5, 15)]                                                                         // MRM data (Q3MS)
        [TestCase("MM_Strap_IMAC_FT_10xDilution_FAIMS_ID_01_FAIMS_Merry_03Feb23_REP-22-11-13.raw", 42000, 42224, true)] // DIA data
        // ReSharper restore StringLiteralTypo
        public void TestIsolationWindowWidth(
            string rawFileName,
            int scanStart,
            int scanEnd,
            bool skipIfMissing = false)
        {
            // Keys in this Dictionary are filename (without the extension), values are ScanCounts by isolation window width, where the key is a Tuple of MSLevel and Isolation Window Width
            var expectedData = new Dictionary<string, Dictionary<Tuple<int, double>, int>>();

            AddExpectedTupleAndCount(expectedData, "B5_50uM_MS_r1", 1, 0.0, 20);

            AddExpectedTupleAndCount(expectedData, "MNSLTFKK_ms", 1, 0.0, 88);

            AddExpectedTupleAndCount(expectedData, "QCShew200uL", 1, 0.0, 101);

            AddExpectedTupleAndCount(expectedData, "Wrighton_MT2_SPE_200avg_240k_neg_330-380", 1, 50.0, 200);

            const string file5 = "1229_02blk1";
            AddExpectedTupleAndCount(expectedData, file5, 1, 0.0, 77);
            AddExpectedTupleAndCount(expectedData, file5, 2, 0.0, 24);

            const string file6 = "MCF7_histone_32_49B_400min_HCD_ETD_01172014_b";
            AddExpectedTupleAndCount(expectedData, file6, 1, 0.0, 18);
            AddExpectedTupleAndCount(expectedData, file6, 2, 3.0, 83);

            const string file7 = "lowdose_IMAC_iTRAQ1_PQDMSA";
            AddExpectedTupleAndCount(expectedData, file7, 1, 0.0, 16);
            AddExpectedTupleAndCount(expectedData, file7, 2, 3.0, 85);

            const string file8 = "MZ20150721blank2";
            AddExpectedTupleAndCount(expectedData, file8, 1, 0.0, 62);
            AddExpectedTupleAndCount(expectedData, file8, 2, 3.0, 372);

            const string file9 = "OG_CEPC_PU_22Oct13_Legolas_13-05-12";
            AddExpectedTupleAndCount(expectedData, file9, 1, 0.0, 9);
            AddExpectedTupleAndCount(expectedData, file9, 2, 3.0, 92);

            const string file10 = "blank_MeOH-3_18May16_Rainier_Thermo_10344958";
            AddExpectedTupleAndCount(expectedData, file10, 1, 0.0, 190);
            AddExpectedTupleAndCount(expectedData, file10, 2, 2.0, 207);
            AddExpectedTupleAndCount(expectedData, file10, 3, 2.0, 4);

            const string file11 = "HCC-38_ETciD_EThcD_07Jan16_Pippin_15-08-53";
            AddExpectedTupleAndCount(expectedData, file11, 1, 1150.0, 20);
            AddExpectedTupleAndCount(expectedData, file11, 2, 2.0, 381);

            const string file12 = "MeOHBlank03POS_11May16_Legolas_HSS-T3_A925";
            AddExpectedTupleAndCount(expectedData, file12, 1, 0.0, 8);
            AddExpectedTupleAndCount(expectedData, file12, 2, 2.0, 93);

            const string file13 = "Angiotensin_AllScans";
            AddExpectedTupleAndCount(expectedData, file13, 1, -1, 10);
            AddExpectedTupleAndCount(expectedData, file13, 2, 1.6, 48);
            AddExpectedTupleAndCount(expectedData, file13, 2, 2.0, 143);

            AddExpectedTupleAndCount(expectedData, "IPA-blank-07_25Oct13_Gimli", 1, 0.0, 101);

            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-CID", 2, 2.0, 10);

            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-ETciD-15", 2, 2.0, 10);
            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-ETD", 2, 2.0, 10);
            AddExpectedTupleAndCount(expectedData, "Angiotensin_325-HCD", 2, 2.0, 10);

            AddExpectedTupleAndCount(expectedData, "Blank04_29Mar17_Smeagol", 2, 0.0, 101);

            // ReSharper disable StringLiteralTypo

            const string file14 = "QC_mam_16_01_125ng_CPTACpt7-3s-a_02Nov17_Pippin_REP-17-10-01";
            AddExpectedTupleAndCount(expectedData, file14, 1, 1450.0, 4);
            AddExpectedTupleAndCount(expectedData, file14, 2, 0.7, 12);

            AddExpectedTupleAndCount(expectedData, "20181115_arginine_Gua13C_CIDcol25_158_HCDcol35", 3, 1.0, 11);

            AddExpectedTupleAndCount(expectedData, "calmix_Q3_10192022_03", 1, 0.0, 11);

            // ReSharper restore StringLiteralTypo

            // DIA dataset
            const string file15 = "MM_Strap_IMAC_FT_10xDilution_FAIMS_ID_01_FAIMS_Merry_03Feb23_REP-22-11-13";
            AddExpectedTupleAndCount(expectedData, file15, 1, -1.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 23.0, 27);
            AddExpectedTupleAndCount(expectedData, file15, 2, 24.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 25.0, 27);
            AddExpectedTupleAndCount(expectedData, file15, 2, 26.0, 18);
            AddExpectedTupleAndCount(expectedData, file15, 2, 27.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 28.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 29.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 30.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 32.0, 18);
            AddExpectedTupleAndCount(expectedData, file15, 2, 35.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 37.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 42.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 48.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 52.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 54.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 71.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 129.0, 9);
            AddExpectedTupleAndCount(expectedData, file15, 2, 453.0, 9);

            var dataFile = GetRawDataFile(rawFileName, skipIfMissing);

            if (dataFile == null)
            {
                Console.WriteLine("Skipping unit tests for " + rawFileName);
                return;
            }

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Parsing scan headers for {0}", dataFile.Name);
            Console.WriteLine();

            var scanCountsActual = new Dictionary<Tuple<int, double>, int>();

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

                var isolationWindowWidth = scanInfo.IsolationWindowWidthMZ;

                var isolationWindowKey = new Tuple<int, double>(scanInfo.MSLevel, isolationWindowWidth);

                if (scanCountsActual.TryGetValue(isolationWindowKey, out var observedScanCount))
                {
                    scanCountsActual[isolationWindowKey] = observedScanCount + 1;
                }
                else
                {
                    scanCountsActual.Add(isolationWindowKey, 1);
                }
            }

            var datasetName = Path.GetFileNameWithoutExtension(dataFile.Name);

            if (!expectedData.TryGetValue(datasetName, out var expectedScanInfo))
            {
                Console.WriteLine("Dataset {0} not found in dictionary expectedData", datasetName);
                expectedScanInfo = new Dictionary<Tuple<int, double>, int>();
            }

            Console.WriteLine("{0,-5} {1,-7} {2,-9} {3}", "Valid", "MSLevel", "Width m/z", "Scan Count");

            foreach (var isolationWindow in (from item in scanCountsActual orderby item.Key select item))
            {
                if (expectedScanInfo.Count == 0)
                {
                    Console.WriteLine("{0,-5} {1,-7} {2,-9:0.0###} {3}", string.Empty, isolationWindow.Key.Item1, isolationWindow.Key.Item2, isolationWindow.Value);
                    continue;
                }

                if (expectedScanInfo.TryGetValue(isolationWindow.Key, out var expectedScanCount))
                {
                    var isValid = isolationWindow.Value == expectedScanCount;

                    Console.WriteLine("{0,-5} {1,-7} {2,-9:0.0###} {3}", isValid, isolationWindow.Key.Item1, isolationWindow.Key.Item2, isolationWindow.Value);

                    if (expectedScanCount >= 0)
                        Assert.That(isolationWindow.Value, Is.EqualTo(expectedScanCount), "Scan count mismatch");
                }
                else
                {
                    Console.WriteLine("{0,-5} {1,-7} {2,-9:0.0###} {3}", "??", isolationWindow.Key.Item1, isolationWindow.Key.Item2, isolationWindow.Value);

                    Assert.Fail($"Unexpected window width key found: {isolationWindow.Key}");
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
        [TestCase("Blank04_29Mar17_Smeagol.raw", 4330)]
        public void TestGetNumScans(string rawFileName, int expectedResult)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            var scanCount = reader.GetNumScans();

            Console.WriteLine("Scan count for {0}: {1}", dataFile.Name, scanCount);

            if (expectedResult >= 0)
                Assert.That(scanCount, Is.EqualTo(expectedResult), "Scan count mismatch");
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 500, 525, 497, 0, 501, 501, 501, 0, 505, 505, 505, 0, 509, 509, 509, 0, 513, 513, 513, 0, 517, 517, 517, 0, 521, 521, 521, 0)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 500, 525, 493, 493, 0, 0, 0, 503, 503, 0, 504, 504, 0, 507, 507, 507, 507, 0, 510, 0, 515, 515, 515, 0, 0, 521, 0, 522)]
        [TestCase("Angiotensin_325-CID.raw", 1, 5, -1, -1, -1, -1, -1)]
        [TestCase("Angiotensin_325-ETciD-15.raw", 1, 5, -1, -1, -1, -1, -1)]
        [TestCase("Angiotensin_325-ETD.raw", 1, 5, -1, -1, -1, -1, -1)]
        [TestCase("Angiotensin_325-HCD.raw", 1, 5, -1, -1, -1, -1, -1)]
        [TestCase("Angiotensin_AllScans.raw", 500, 550, 477, 477, 499, 499, 499, 500, 500, 500, 501, 501, 501, 477, 477, 511, 511, 511, 512, 512, 512, 0, 498, 498, 498, 520, 520, 520, 521, 521, 521, 522, 522, 522, 498, 498, 532, 532, 532, 533, 533, 533, 0, 519, 519, 519, 541, 541, 541, 542, 542, 542, 543)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 1500, 1510, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        public void TestGetParentScan(string rawFileName, int startScan, int endScan, params int[] expectedParents)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            var i = 0;
            var validScanCount = 0;

            for (var scanNumber = startScan; scanNumber <= endScan; scanNumber++)
            {
                if (!reader.GetScanInfo(scanNumber, out var scanInfo))
                {
                    ConsoleMsgUtils.ShowWarning("Invalid scan number: {0}", scanNumber);
                    i++;
                    continue;
                }

                validScanCount++;

                Console.WriteLine("MS{0} scan {1,-4} has parent {2,-4}", scanInfo.MSLevel, scanNumber, scanInfo.ParentScan);

                if (i < expectedParents.Length && expectedParents[i] != 0)
                {
                    Assert.That(
                        scanInfo.ParentScan, Is.EqualTo(expectedParents[i]),
                        $"Parent scan does not match expected value: {expectedParents[i]}");
                }

                i++;
            }

            var percentValid = validScanCount / (double)(endScan - startScan + 1) * 100;
            Assert.That(percentValid, Is.GreaterThan(90), "Over 10% of the spectra had invalid scan numbers");
        }

        // ReSharper disable StringLiteralTypo

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
        [TestCase("QC_mam_16_01_125ng_CPTACpt7-3s-a_02Nov17_Pippin_REP-17-10-01.raw", 65, 80, 4, 12, 126)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 2500, 2600, 0, 101, 4330)]                                 // SRM data
        [TestCase("20181115_arginine_Gua13C_CIDcol25_158_HCDcol35.raw", 10, 20, 0, 11, 34)]                 // MS3 scans
        [TestCase("calmix_Q3_10192022_03.RAW", 5, 15, 11, 0, 20)]                                           // MRM data (Q3MS)
        [TestCase("MM_Strap_IMAC_FT_10xDilution_FAIMS_ID_01_FAIMS_Merry_03Feb23_REP-22-11-13.raw", 42000, 42224, 9, 216, 92550, true)] // DIA data
        // ReSharper restore StringLiteralTypo
        public void TestGetScanCountsByScanType(
            string rawFileName,
            int scanStart,
            int scanEnd,
            int expectedMS1,
            int expectedMS2,
            int expectedTotalScanCount,
            bool skipIfMissing = false)
        {
            // Keys in this Dictionary are filename (without the extension), values are ScanCounts by collision mode, where the key is a Tuple of ScanType and FilterString
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

            AddExpectedTupleAndCount(expectedData, "Blank04_29Mar17_Smeagol", "CID-SRM", "+ c NSI SRM ms2", 101);

            // ReSharper disable StringLiteralTypo

            AddExpectedTupleAndCount(expectedData, "QC_mam_16_01_125ng_CPTACpt7-3s-a_02Nov17_Pippin_REP-17-10-01", "HMS", "FTMS + p NSI Full ms", 4);
            AddExpectedTupleAndCount(expectedData, "QC_mam_16_01_125ng_CPTACpt7-3s-a_02Nov17_Pippin_REP-17-10-01", "HCD-HMSn", "FTMS + c NSI d Full ms2 0@hcd30.00", 12);

            AddExpectedTupleAndCount(expectedData, "20181115_arginine_Gua13C_CIDcol25_158_HCDcol35", "HCD-HMSn", "FTMS + c NSI Full ms3 0@cid25.00 0@hcd35.00", 11);

            AddExpectedTupleAndCount(expectedData, "calmix_Q3_10192022_03", "Q3MS", "+ p NSI Q3MS", 11);

            // ReSharper restore StringLiteralTypo

            // DIA dataset
            // ReSharper disable once StringLiteralTypo
            const string file14 = "MM_Strap_IMAC_FT_10xDilution_FAIMS_ID_01_FAIMS_Merry_03Feb23_REP-22-11-13";

            // Selected scan range (42000 - 42224) covers 24 isolation windows, run at three different CVs (-40, -60, and -80)

            // Use a for loop to add rows to the expected data dictionary, for each CV level

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var voltage in new List<int> { -40, -60, -80 })
            {
                // Example text: cv=-40.00
                var currentCV = string.Format("cv={0}.00", voltage);

                AddExpectedTupleAndCount(expectedData, file14, "HMS", string.Format("FTMS + p NSI {0} Full ms", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 377.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 419.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 448.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 473.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 497.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 520.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 542.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 564.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 587.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 610.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 635.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 660.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 685.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 712.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 741.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 771.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 803.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 838.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 877.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 921.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 972.0@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 1034.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 1133.5@hcd32.00", currentCV), 3);
                AddExpectedTupleAndCount(expectedData, file14, "DIA-HCD-HMSn", string.Format("FTMS + p NSI {0} Full ms2 1423.5@hcd32.00", currentCV), 3);
            }

            var dataFile = GetRawDataFile(rawFileName, skipIfMissing);

            if (dataFile == null)
            {
                Console.WriteLine("Skipping unit tests for " + rawFileName);
                return;
            }

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Parsing scan headers for {0}", dataFile.Name);

            var scanCount = reader.GetNumScans();
            Console.WriteLine("Total scans: {0}", scanCount);
            Console.WriteLine();

            if (expectedTotalScanCount > 0)
                Assert.That(scanCount, Is.EqualTo(expectedTotalScanCount), "Total scan count mismatch");

            var scanCountMS1 = 0;
            var scanCountMS2 = 0;
            var scanTypeCountsActual = new Dictionary<Tuple<string, string>, int>();

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

                var scanType = XRawFileIO.GetScanTypeNameFromThermoScanFilterText(scanInfo.FilterText, scanInfo.IsDIA, null);
                var genericScanFilter = XRawFileIO.MakeGenericThermoScanFilter(scanInfo.FilterText, scanInfo.IsDIA);

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

            if (expectedTotalScanCount > 0)
                Assert.That(scanCountMS1, Is.EqualTo(expectedMS1), "MS1 scan count mismatch");

            if (expectedTotalScanCount > 0)
                Assert.That(scanCountMS2, Is.EqualTo(expectedMS2), "MS2 scan count mismatch");

            var datasetName = Path.GetFileNameWithoutExtension(dataFile.Name);

            if (!expectedData.TryGetValue(datasetName, out var expectedScanInfo))
            {
                Console.WriteLine("Dataset {0} not found in dictionary expectedData", datasetName);
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
                        Assert.That(scanType.Value, Is.EqualTo(expectedScanCount), "Scan type count mismatch");
                }
                else
                {
                    Console.WriteLine("Unexpected scan type found: {0}", scanType.Key);
                    Assert.Fail($"Unexpected scan type found: {scanType.Key}");
                }
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521, 3, 6)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165, 3, 42)]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw", 20500, 20520, 7, 14)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 3000, 3030, 0, 31)]
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

            var file4Data = new Dictionary<int, string>
            {
                {3000, "2 2     4 26.67 458 1152 7.0E+0  607.323 4.9E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3001, "2 2     4 26.68 458 1152 3.9E+0  773.404 1.5E+0   552.80 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3002, "2 2     4 26.68 458 1152 1.6E+1  781.418 1.4E+1   556.81 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3003, "2 2     4 26.68 458 1152 4.8E+1  465.213 4.0E+1   528.94 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3004, "2 2     4 26.68 458 1152 3.1E+0  685.379 1.0E+0   531.61 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3005, "2 2     4 26.69 458 1152 3.1E+0  710.398 1.1E+0   513.27 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3006, "2 2     4 26.69 458 1152 6.7E+0  720.407 4.6E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3007, "2 2     4 26.69 458 1152 3.4E+0  773.404 1.4E+0   552.80 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3008, "2 2     4 26.70 458 1152 3.1E+0  781.418 1.0E+0   556.81 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3009, "2 2     4 26.70 458 1152 2.8E+1  465.213 2.5E+1   528.94 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3010, "2 2     4 26.70 458 1152 3.8E+0  586.311 1.6E+0   531.61 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3011, "2 2     4 26.70 458 1152 8.4E+0  823.482 6.3E+0   513.27 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3012, "2 2     4 26.71 458 1152 4.7E+0  833.491 2.3E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3013, "2 2     4 26.71 458 1152 3.1E+0  886.488 1.1E+0   552.80 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3014, "2 2     4 26.71 458 1152 5.0E+0  466.275 3.0E+0   556.81 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3015, "2 2     4 26.71 458 1152 3.2E+1  465.213 2.9E+1   528.94 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3016, "2 2     4 26.72 458 1152 7.9E+0  685.379 5.5E+0   531.61 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3017, "2 2     4 26.72 458 1152 3.2E+0  597.314 1.1E+0   513.27 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3018, "2 2     4 26.72 458 1152 3.1E+0  607.323 1.0E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3019, "2 2     4 26.73 458 1152 2.0E+1  773.404 1.8E+1   552.80 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3020, "2 2     4 26.73 458 1152 3.6E+0  781.418 1.6E+0   556.81 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3021, "2 2     4 26.73 458 1152 1.5E+1  465.213 9.6E+0   528.94 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3022, "2 2     4 26.73 458 1152 6.1E+0  473.227 3.2E+0   531.61 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3023, "2 2     4 26.74 458 1152 3.0E+0  597.314 1.0E+0   513.27 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3024, "2 2     4 26.74 458 1152 3.7E+0  720.407 1.6E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3025, "2 2     4 26.74 458 1152 3.2E+0  886.488 1.1E+0   552.80 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3026, "2 2     4 26.74 458 1152 2.3E+1  466.275 2.1E+1   556.81 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3027, "2 2     4 26.75 458 1152 6.6E+0  465.213 4.5E+0   528.94 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3028, "2 2     4 26.75 458 1152 4.6E+0  473.227 2.2E+0   531.61 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3029, "2 2     4 26.75 458 1152 3.0E+0  597.314 1.0E+0   513.27 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."},
                {3030, "2 2     4 26.76 458 1152 8.0E+0  833.491 3.6E+0   518.28 AnyType       Positive True False 3 63   0.00 + c NSI SRM ..."}
            };

            expectedData.Add("Blank04_29Mar17_Smeagol", file4Data);

            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Scan info for {0}", dataFile.Name);
            Console.WriteLine(
                "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19}",
                "Scan", "MSLevel", "Event",
                "NumPeaks", "RetentionTime",
                "LowMass", "HighMass", "TotalIonCurrent",
                "BasePeakMZ", "BasePeakIntensity",
                "ParentIonMZ", "ActivationType", "CollisionMode",
                "IonMode", "IsCentroided", "IsHighResolution",
                "ScanEvents.Count", "StatusLog.Count",
                "IonInjectionTime", "FilterText");

            var scanCountMS1 = 0;
            var scanCountMS2 = 0;

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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
                        scanInfo.IsHighResolution, scanInfo.ScanEvents.Count, scanInfo.StatusLog.Count, ionInjectionTime,
                        scanInfo.FilterText.Substring(0, 12) + "...");

                Console.WriteLine(scanSummary);

                if (scanInfo.MSLevel > 1)
                    scanCountMS2++;
                else
                    scanCountMS1++;

                if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                {
                    Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                }

                if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
                {
                    Assert.That(scanSummary, Is.EqualTo(scanNumber + " " + expectedScanSummary),
                        "Scan summary mismatch, scan " + scanNumber);
                }
            }

            Console.WriteLine("scanCountMS1={0}", scanCountMS1);
            Console.WriteLine("scanCountMS2={0}", scanCountMS2);

            if (expectedMS1 < 0 && expectedMS2 < 0)
                return;

            Assert.That(scanCountMS1, Is.EqualTo(expectedMS1), "MS1 scan count mismatch");
        }

        [Test]
        [TestCase("B5_50uM_MS_r1.RAW", 1, 20)]
        [TestCase("MNSLTFKK_ms.raw", 1, 88)]
        [TestCase("QCShew200uL.raw", 4000, 4100)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 500, 600)]
        public void TestGetScanInfoMRM(string rawFileName, int scanStart, int scanEnd)
        {
            // Keys in this Dictionary are filename (without the extension), values are a Dictionary of expected SIM scan counts (by type)
            var expectedData = new Dictionary<string, Dictionary<string, int>>
            {
                {
                    "B5_50uM_MS_r1", new Dictionary<string, int>
                    {
                        { "200.0_600.0_1000.0", 20 }
                    }
                },
                {
                    "MNSLTFKK_ms", new Dictionary<string, int>
                    {
                        { "200.1_700.0_1200.0", 88 }
                    }
                },
                { "QCShew200uL", new Dictionary<string, int>
                    {
                        { "400.0_900.0_1400.0", 101 }
                    }
                },
                { "Blank04_29Mar17_Smeagol", new Dictionary<string, int>
                    {
                        { "602.3_602.3_602.3", 16 },
                        { "610.3_610.3_610.3", 17 },
                        { "637.3_637.3_637.3", 17 },
                        { "645.3_645.3_645.3", 17 },
                        { "715.4_715.4_715.4", 16 },
                        { "723.4_723.4_723.4", 17 },
                        { "750.3_750.3_750.3", 17 },
                        { "758.4_758.4_758.4", 17 },
                        { "897.4_897.4_897.4", 17 },
                        { "907.4_907.4_907.4", 17 },
                        { "938.4_938.4_938.4", 17 },
                        { "946.4_946.4_946.4", 17 },
                        { "988.5_988.5_988.5", 16 },
                        { "996.5_996.5_996.5", 17 },
                        { "1012.5_1012.5_1012.5", 17 },
                        { "1022.5_1022.5_1022.5", 17 },
                        { "1141.5_1141.5_1141.5", 17 },
                        { "1151.5_1151.5_1151.5", 17 }
                    }
                }
            };

            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Examining MRM details in {0}", dataFile.Name);
            Console.WriteLine();

            var mrmRangeCountsActual = new Dictionary<string, int>();

            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedMRMInfo))
            {
                Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
            }

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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
            }

            if (mrmRangeCountsActual.Count != expectedMRMInfo.Count)
            {
                Console.WriteLine("{0,-20} {1,-9}", "MRMScanRange", "Count");

                foreach (var mrmRangeActual in mrmRangeCountsActual)
                {
                    Console.WriteLine("{0,-20} {1,-9}", mrmRangeActual.Key, mrmRangeActual.Value);
                }

                Assert.That(mrmRangeCountsActual, Has.Count.EqualTo(expectedMRMInfo.Count),
                    $"Found {mrmRangeCountsActual.Count} MRM scan ranges; expected to find {expectedMRMInfo.Count}");

                return;
            }

            Console.WriteLine("{0,-5} {1,-5} {2}", "Valid", "Count", "MRMScanRange");

            var mismatches = new List<string>();

            foreach (var mrmRangeActual in mrmRangeCountsActual)
            {
                bool isValid;

                if (expectedMRMInfo.TryGetValue(mrmRangeActual.Key, out var expectedCount))
                {
                    isValid = mrmRangeActual.Value == expectedCount;

                    if (!isValid)
                    {
                        mismatches.Add(string.Format(
                            "Unexpected MRM scan range found: {0} has {1} scans instead of {2}",
                            mrmRangeActual.Key,
                            mrmRangeActual.Value,
                            expectedCount));
                    }
                }
                else
                {
                    isValid = false;

                    mismatches.Add(string.Format(
                        "Unexpected MRM scan range found: {0} with {1} scans is not in the dictionary",
                        mrmRangeActual.Key,
                        mrmRangeActual.Value));
                }

                Console.WriteLine("{0,-5} {1,-5} {2}", isValid, mrmRangeActual.Value, mrmRangeActual.Key);
            }

            if (mismatches.Count == 0)
                return;

            Console.WriteLine();

            foreach (var item in mismatches)
            {
                Console.WriteLine(item);
            }

            Assert.Fail("Unexpected MRM scan range(s) found");
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
            file1Data[1513].Add("0_False", "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False", "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_False", "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_False", "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_False", "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("0_True", "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True", "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_True", "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_True", "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_True", "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_False", "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False", "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_False", "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_False", "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_False", "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_True", "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True", "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_True", "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_True", "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_True", "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

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
            file2Data[16121].Add("0_False", "11888    11888     346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False", "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_False", "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_False", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_False", "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_False", "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("0_True", "  833      833     351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True", "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_True", "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_True", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_True", "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_True", "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_False", "   50       50     503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_False", "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_False", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_False", "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_False", "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_True", "   50       50     371.733   6.2E+6  681.010   6.2E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True", "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_True", "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_True", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_True", "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_True", "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

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
            file3Data[3101].Add("0_False", "19200    19200     400.083   1.7E+3 1200.083  5.2E-23  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_False", "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("0_True", "  906      906     400.389   1.5E+4  760.724   3.9E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_True", "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_False", "   50       50     500.333   4.8E+4  555.250   4.2E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_False", "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_True", "   50       50     423.593   1.1E+5  596.215   9.5E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_True", "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");

            expectedData.Add("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08", file3Data);

            var file4Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file4Data, 4371, 4373);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file4Data[4371].Add("0_False", " 9271     9271     200.000   0.0E+0  597.504   0.0E+0  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_False", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_False", "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("0_True", "  691      691     200.052   4.7E+2  600.505   7.1E+2  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_True", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_True", "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_False", "   50       50     324.984   4.3E+4  447.116   8.4E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_False", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_False", "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_True", "   50       50     217.018   4.9E+3  449.337   1.3E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_True", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_True", "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");

            expectedData.Add("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925", file4Data);

            var file5Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file5Data, 22010, 22014);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file5Data[22010].Add("0_False", "35347    35347     396.014   0.0E+0  642.910   5.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_False", " 3910     3910      92.733   0.0E+0  262.166   9.7E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_False", "34829    34829     396.014   0.0E+0  639.990   5.6E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_False", " 3756     3756      99.003   0.0E+0  244.134   1.7E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_False", " 3403     3403     140.253   0.0E+0  367.176   0.0E+0  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("0_True", " 2500     2500     401.286   5.6E+5  644.143   2.2E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_True", "  262      262     101.071   5.3E+4  267.660   8.2E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_True", " 2444     2444     400.264   3.8E+5  638.584   6.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_True", "  271      271     101.071   9.8E+4  244.166   3.3E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_True", "  236      236     142.062   2.1E+4  361.153   1.8E+4  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_False", "   50       50     469.269   1.8E+8  495.792   6.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_False", "   50       50     110.071   2.1E+5  183.153   5.6E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_False", "   50       50     469.269   2.5E+8  495.789   4.6E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_False", "   50       50     110.070   3.4E+5  169.100   4.5E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_False", "   50       50     147.112   3.0E+5  687.427   2.8E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_True", "   50       50     469.272   2.7E+8  606.977   9.0E+7  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_True", "   50       50     102.055   7.1E+4  233.165   1.1E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_True", "   50       50     469.272   3.6E+8  606.643   1.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_True", "   50       50     102.055   1.1E+5  218.150   1.4E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_True", "   50       50     147.113   3.4E+5  428.252   1.4E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");

            expectedData.Add("Lewy2_19Ct1_2Nov13_Samwise_13-07-28", file5Data);

            var options = new ThermoReaderOptions
            {
                IncludeReferenceAndExceptionData = false
            };

            TestGetScanDataWork(rawFileName, options, scanStart, scanEnd, expectedData);
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1521)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16165)]
        [TestCase("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08.raw", 3101, 3102)]
        [TestCase("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925.raw", 4371, 4373)]
        [TestCase("Lewy2_19Ct1_2Nov13_Samwise_13-07-28.raw", 22010, 22014)]
        public void TestGetScanDataWithReferenceAndExceptionPeaks(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

            // Keys in this dictionary are the scan number of data being retrieved
            var file1Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file1Data, 1513, 1517);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file1Data[1513].Add("0_False", "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False", "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_False", "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_False", "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_False", "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("0_True", "  851      851     409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True", "  109      109     281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("0_True", "  290      290     335.798   3.8E+4 1034.194   1.6E+4  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("0_True", "  154      154     461.889   7.3E+3 1203.274   2.6E+3  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("0_True", "  887      887     420.016   9.7E+5 1232.206   8.0E+5  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_False", "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False", "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_False", "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_False", "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_False", "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

            file1Data[1513].Add("50_True", "   50       50     747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True", "   50       50     281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1515].Add("50_True", "   50       50     353.590   9.7E+4 1157.949   3.6E+5  + c d Full ms2 1147.67@cid45.00 [305.00-2000.00]");
            file1Data[1516].Add("50_True", "   50       50     461.889   7.3E+3 1146.341   1.4E+4  + c d Full ms2 1492.90@cid45.00 [400.00-2000.00]");
            file1Data[1517].Add("50_True", "   50       50     883.347   8.9E+6 1206.792   5.5E+6  + c ESI Full ms [400.00-2000.00]");

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
            file2Data[16121].Add("0_False", "11888    11888     346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False", "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_False", "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_False", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_False", "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_False", "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("0_True", "  833      833     351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True", "  490      490     116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("0_True", "  753      753     231.045   1.1E+1 1004.586   2.0E+1  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("0_True", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("0_True", "  280      280     260.118   2.3E+1  663.160   7.7E+0  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("0_True", "  240      240     304.425   1.3E+1 1447.649   3.0E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_False", "   50       50     503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_False", "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_False", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_False", "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_False", "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

            file2Data[16121].Add("50_True", "   50       50     371.733   6.2E+6  681.010   6.2E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True", "   50       50     157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16126].Add("50_True", "   50       50     535.311   2.5E+3  798.982   1.3E+3  ITMS + c NSI r d sa Full ms2 538.8400@etd53.58@cid20.00 [120.0000-1627.0000]");
            file2Data[16131].Add("50_True", "   29       29     984.504   9.5E+3 1931.917   2.4E+1  ITMS + c NSI r d Full ms2 987.8934@etd120.55 [120.0000-1986.0000]");
            file2Data[16133].Add("50_True", "   50       50     356.206   7.5E+1  795.543   1.3E+2  ITMS + c NSI r d sa Full ms2 421.2619@etd120.55@cid20.00 [120.0000-853.0000]");
            file2Data[16141].Add("50_True", "   50       50     853.937   5.6E+1 1705.974   9.8E+1  ITMS + c NSI r d sa Full ms2 874.8397@etd120.55@hcd20.00 [120.0000-1760.0000]");

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
            file3Data[3101].Add("0_False", "19200    19200     400.083   1.7E+3 1200.083  5.2E-23  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_False", "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("0_True", "  906      906     400.389   1.5E+4  760.724   3.9E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_True", "  329      329     147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_False", "   50       50     500.333   4.8E+4  555.250   4.2E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_False", "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_True", "   50       50     423.593   1.1E+5  596.215   9.5E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_True", "   50       50     147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");

            expectedData.Add("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08", file3Data);

            var file4Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file4Data, 4371, 4373);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file4Data[4371].Add("0_False", " 9271     9271     200.000   0.0E+0  597.504   0.0E+0  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_False", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_False", "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("0_True", "  691      691     200.052   4.7E+2  600.505   7.1E+2  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_True", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("0_True", "   97       97      95.192   6.9E+0  337.598   7.2E+0  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_False", "   50       50     324.984   4.3E+4  447.116   8.4E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_False", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_False", "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");
            file4Data[4371].Add("50_True", "   50       50     217.018   4.9E+3  449.337   1.3E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_True", "   23       23      91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4373].Add("50_True", "   50       50     122.133   2.0E+1  377.493   1.7E+1  ITMS + c ESI d Full ms2 465.14@cid35.00 [80.00-480.00]");

            expectedData.Add("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925", file4Data);

            var file5Data = new Dictionary<int, Dictionary<string, string>>();
            AddEmptyDictionaries(file5Data, 22010, 22014);

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file5Data[22010].Add("0_False", "35347    35347     396.014   0.0E+0  642.910   5.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_False", " 3910     3910      92.733   0.0E+0  262.166   9.7E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_False", "34829    34829     396.014   0.0E+0  639.990   5.6E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_False", " 3756     3756      99.003   0.0E+0  244.134   1.7E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_False", " 3403     3403     140.253   0.0E+0  367.176   0.0E+0  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("0_True", " 2500     2500     401.286   5.6E+5  644.143   2.2E+6  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("0_True", "  262      262     101.071   5.3E+4  267.660   8.2E+3  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("0_True", " 2444     2444     400.264   3.8E+5  638.584   6.1E+5  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("0_True", "  271      271     101.071   9.8E+4  244.166   3.3E+4  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("0_True", "  236      236     142.062   2.1E+4  361.153   1.8E+4  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_False", "   50       50     469.269   1.8E+8  495.792   6.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_False", "   50       50     110.071   2.1E+5  183.153   5.6E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_False", "   50       50     469.269   2.5E+8  495.789   4.6E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_False", "   50       50     110.070   3.4E+5  169.100   4.5E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_False", "   50       50     147.112   3.0E+5  687.427   2.8E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");
            file5Data[22010].Add("50_True", "   50       50     469.272   2.7E+8  606.977   9.0E+7  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22011].Add("50_True", "   50       50     102.055   7.1E+4  233.165   1.1E+5  FTMS + p NSI d Full ms2 451.62@hcd32.00 [93.67-1405.00]");
            file5Data[22012].Add("50_True", "   50       50     469.272   3.6E+8  606.643   1.7E+8  FTMS + p NSI Full ms [400.00-2000.00]");
            file5Data[22013].Add("50_True", "   50       50     102.055   1.1E+5  218.150   1.4E+5  FTMS + p NSI d Full ms2 726.87@hcd32.00 [100.00-1500.00]");
            file5Data[22014].Add("50_True", "   50       50     147.113   3.4E+5  428.252   1.4E+5  FTMS + p NSI d Full ms2 687.05@hcd32.00 [141.67-2125.00]");

            expectedData.Add("Lewy2_19Ct1_2Nov13_Samwise_13-07-28", file5Data);

            var options = new ThermoReaderOptions
            {
                IncludeReferenceAndExceptionData = true
            };

            TestGetScanDataWork(rawFileName, options, scanStart, scanEnd, expectedData);
        }

        private void TestGetScanDataWork(
            string rawFileName,
            ThermoReaderOptions options,
            int scanStart,
            int scanEnd,
            IReadOnlyDictionary<string, Dictionary<int, Dictionary<string, string>>> expectedData)
        {
            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName, options);

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
                    Console.WriteLine(
                        "{0,5} {1,-5} {2,-10} {3,-8} {4,-8} {5,-8} {6,-8} {7,-9} {8,-7} {9}",
                        "Scan", "Max#", "Centroid", "MzCount", "IntCount",
                        "FirstMz", "FirstInt", "MidMz", "MidInt", "ScanFilter");
                }

                for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
                {
                    var dataPointsRead = reader.GetScanData(scanNumber, out var mzList, out var intensityList, maxNumberOfPeaks, centroidData);

                    Assert.That(dataPointsRead, Is.GreaterThan(0), $"GetScanData returned 0 for scan {scanNumber}");

                    Assert.That(dataPointsRead, Is.EqualTo(mzList.Length), "Data count mismatch vs. function return value");

                    var midPoint = (int)(intensityList.Length / 2f);

                    var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                    Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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
                        Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                    }

                    if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedDataByType))
                    {
                        var keySpec = maxNumberOfPeaks + "_" + centroidData;

                        if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
                        {
                            var observedValue = scanSummary.Substring(22);

                            Assert.That(observedValue, Is.EqualTo(expectedDataDetails),
                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
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
            file1Data[1513].Add("0_False", "  851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_False", "  109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("0_True", "  851  409.615   4.8E+5 1227.956   1.6E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("0_True", "  109  281.601   2.4E+4  633.151   4.4E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_False", "   50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_False", "   50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");
            file1Data[1513].Add("50_True", "   50  747.055   2.5E+6 1148.485   3.4E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1514].Add("50_True", "   50  281.601   2.4E+4  632.089   2.6E+4  + c d Full ms2 884.41@cid45.00 [230.00-1780.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, Dictionary<string, string>>
            {
                {16121, new Dictionary<string, string>()},
                {16122, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False", "11888  346.518   0.0E+0  706.844   9.8E+4  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_False", "  490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("0_True", "  833  351.231   2.9E+5  712.813   2.9E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("0_True", "  490  116.232   7.0E+1  403.932   1.1E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("50_False", "   50  503.553   2.0E+7  504.571   2.1E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_False", "   50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");
            file2Data[16121].Add("50_True", "   50  371.733   6.2E+6  681.010   6.2E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16122].Add("50_True", "   50  157.049   2.0E+4  385.181   6.0E+3  ITMS + c NSI r d Full ms2 403.2206@cid30.00 [106.0000-817.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);

            var file3Data = new Dictionary<int, Dictionary<string, string>>
            {
                {3101, new Dictionary<string, string>()},
                {3102, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file3Data[3101].Add("0_False", "19200  400.083   1.7E+3 1200.083  5.2E-23  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_False", "  329  147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("0_True", "  906  400.389   1.5E+4  760.724   3.9E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("0_True", "  329  147.123   4.3E+2  550.548   1.0E+1  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_False", "   50  500.333   4.8E+4  555.250   4.2E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_False", "   50  147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");
            file3Data[3101].Add("50_True", "   50  423.593   1.1E+5  596.215   9.5E+4  ITMS + p NSI Full ms [400.00-2000.00]");
            file3Data[3102].Add("50_True", "   50  147.123   4.3E+2  545.401   1.4E+3  ITMS + c NSI d Full ms2 500.85@cid35.00 [125.00-2000.00]");

            expectedData.Add("QC_Shew_15_02_Run-2_9Nov15_Oak_14-11-08", file2Data);

            var file4Data = new Dictionary<int, Dictionary<string, string>>
            {
                {4371, new Dictionary<string, string>()},
                {4372, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file4Data[4371].Add("0_False", " 9271  200.000   0.0E+0  597.504   0.0E+0  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_False", "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("0_True", "  691  200.052   4.7E+2  600.505   7.1E+2  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("0_True", "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("50_False", "   50  324.984   4.3E+4  447.116   8.4E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_False", "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");
            file4Data[4371].Add("50_True", "   50  217.018   4.9E+3  449.337   1.3E+4  FTMS + p ESI Full ms [200.00-2000.00]");
            file4Data[4372].Add("50_True", "   23   91.297   7.5E+2  223.823   6.3E+2  FTMS + c ESI d Full ms2 465.14@hcd30.00 [90.00-480.00]");

            expectedData.Add("MeOHBlank03POS_11May16_Legolas_HSS-T3_A925", file4Data);

            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Scan data for {0}", dataFile.Name);
            Console.WriteLine(
                "{0,5} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
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

                    Assert.That(dataPointsRead, Is.GreaterThan(0), $"GetScanData2D returned 0 for scan {scanNumber}");

                    var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                    Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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
                                Assert.That(massIntensityPairs[0, dataIndex], Is.Zero, "Non-zero m/z value found in 2D array beyond expected index");
                            }

                            if (massIntensityPairs[1, dataIndex] > 0)
                            {
                                Console.WriteLine("Non-zero intensity value found at index {0} for scan {1}", dataIndex, scanNumber);
                                Assert.That(massIntensityPairs[1, dataIndex], Is.Zero, "Non-zero intensity value found in 2D array beyond expected index");
                            }
                        }
                    }
                    else
                    {
                        dataCount = lastIndex + 1;
                    }

                    Assert.That(dataCount, Is.EqualTo(dataPointsRead), "Data count mismatch vs. function return value");

                    var midPoint = (int)(dataCount / 2f);

                    var scanSummary =
                        string.Format(
                            "{0,5} {1,3} {2,8} {3,8} {4,8:F3} {5,8:0.0E+0} {6,8:F3} {7,8:0.0E+0}  {8}",
                            scanNumber, maxNumberOfPeaks, centroidData,
                            dataCount,
                            massIntensityPairs[0, 0], massIntensityPairs[1, 0],
                            massIntensityPairs[0, midPoint], massIntensityPairs[1, midPoint],
                            scanInfo.FilterText);

                    Console.WriteLine(scanSummary);

                    if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedDataThisFile))
                    {
                        Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                    }

                    if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedDataByType))
                    {
                        var keySpec = maxNumberOfPeaks + "_" + centroidData;

                        if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
                        {
                            Assert.That(scanSummary.Substring(22), Is.EqualTo(expectedDataDetails),
                                "Scan details mismatch, scan " + scanNumber + ", keySpec " + keySpec);
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
            file1Data[1513].Add("0_False", "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("0_True", "1390  409.769   2.7E+5 1241.231   4.0E+5  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_False", "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");
            file1Data[1513].Add("50_True", "  50  883.357   5.5E+6 1213.223   2.0E+6  + c ESI Full ms [400.00-2000.00]");

            expectedData.Add("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20", file1Data);

            var file2Data = new Dictionary<int, Dictionary<string, string>>
            {
                {16121, new Dictionary<string, string>()}
            };

            // The KeySpec for each dictionary entry is MaxDataCount_Centroid
            file2Data[16121].Add("0_False", "26057  346.518   0.0E+0  753.312   8.7E+0  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("0_True", " 1786  351.231   9.5E+4  758.261   1.4E+5  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("50_False", "   50  503.553   1.2E+7  521.201   1.6E+7  FTMS + p NSI Full ms [350.0000-1550.0000]");
            file2Data[16121].Add("50_True", "   50  371.733   4.4E+6  691.981   9.9E+6  FTMS + p NSI Full ms [350.0000-1550.0000]");

            expectedData.Add("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53", file2Data);

            var dataFile = GetRawDataFile(rawFileName);

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Scan data for {0}", dataFile.Name);
            Console.WriteLine(
                "{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
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

                Assert.That(dataPointsRead, Is.GreaterThan(0), $"GetScanDataSumScans returned 0 summing scans {scanStart} to {scanEnd}");

                var success = reader.GetScanInfo(scanStart, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanStart}");

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
                            Assert.That(massIntensityPairs[0, dataIndex], Is.Zero, "Non-zero m/z value found in 2D array beyond expected index");
                        }

                        if (massIntensityPairs[1, dataIndex] > 0)
                        {
                            Console.WriteLine("Non-zero intensity value found at index {0} for scan {1}", dataIndex, scanStart);
                            Assert.That(massIntensityPairs[1, dataIndex], Is.Zero, "Non-zero intensity value found in 2D array beyond expected index");
                        }
                    }
                }
                else
                {
                    dataCount = lastIndex + 1;
                }

                Assert.That(dataCount, Is.EqualTo(dataPointsRead), "Data count mismatch vs. function return value");

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
                    Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                }

                if (expectedDataThisFile.TryGetValue(scanStart, out var expectedDataByType))
                {
                    var keySpec = maxNumberOfPeaks + "_" + centroidData;

                    if (expectedDataByType.TryGetValue(keySpec, out var expectedDataDetails))
                    {
                        Assert.That(scanSummary.Substring(22), Is.EqualTo(expectedDataDetails),
                            "Scan details mismatch, scan " + scanStart + ", keySpec " + keySpec);
                    }
                }

                Console.WriteLine(scanSummary);
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanLabelData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            const string noMatch = "  0                                                        ";

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

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Scan label data for {0}", dataFile.Name);
            Console.WriteLine(
                "{0} {1,3} {2,8} {3,-8} {4,-8} {5,-8} {6,-8} {7,-8}  {8}",
                "Scan", "Count", "Mass", "Intensity",
                "Resolution", "Baseline", "Noise", "Charge", "ScanFilter");

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                // List of mass, intensity, resolution, baseline intensity, noise floor, and charge for each data point

                var dataPointsRead = reader.GetScanLabelData(scanNumber, out var ftLabelData);

                if (dataPointsRead == -1)
                    Assert.That(ftLabelData, Has.Length.Zero, "Data count mismatch vs. function return value");
                else
                    Assert.That(ftLabelData, Has.Length.EqualTo(dataPointsRead), "Data count mismatch vs. function return value");

                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanStart}");

                if (ftLabelData.Length == 0 && scanInfo.IsHighResolution)
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
                    Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                }

                if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
                {
                    Assert.That(scanSummary, Is.EqualTo(scanNumber + " " + expectedScanSummary),
                        "Scan summary mismatch, scan " + scanNumber);
                }

                Console.WriteLine(scanSummary);
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 1513, 1514)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 16121, 16142)]
        public void TestGetScanPrecisionData(string rawFileName, int scanStart, int scanEnd)
        {
            var expectedData = new Dictionary<string, Dictionary<int, string>>();

            const string noMatch = "  0                                               ";

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

            using var reader = new XRawFileIO(dataFile.FullName);

            Console.WriteLine("Scan label data for {0}", dataFile.Name);
            Console.WriteLine(
                "{0} {1,3} {2,8} {3,8} {4,8} {5,8} {6,8}  {7}",
                "Scan", "Count", "Mass", "Intensity",
                "Resolution", "AccuracyMMU", "AccuracyPPM", "ScanFilter");

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                // List of Intensity, Mass, AccuracyMMU, AccuracyPPM, and Resolution for each data point

                var dataPointsRead = reader.GetScanPrecisionData(scanNumber, out var massResolutionData);

                if (dataPointsRead == -1)
                    Assert.That(massResolutionData, Has.Length.Zero, "Data count mismatch vs. function return value");
                else
                    Assert.That(massResolutionData, Has.Length.EqualTo(dataPointsRead), "Data count mismatch vs. function return value");

                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanStart}");

                if (massResolutionData.Length == 0 && scanInfo.IsHighResolution)
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
                    Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
                }

                if (expectedDataThisFile.TryGetValue(scanNumber, out var expectedScanSummary))
                {
                    Assert.That(scanSummary, Is.EqualTo(scanNumber + " " + expectedScanSummary),
                        "Scan summary mismatch, scan " + scanNumber);
                }

                Console.WriteLine(scanSummary);
            }
        }

        [Test]
        [TestCase("Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW", 2000, 2100)]
        [TestCase("HCC-38_ETciD_EThcD_4xdil_20uL_3hr_3_08Jan16_Pippin_15-08-53.raw", 45000, 45200)]
        [TestCase("QC_Mam_16_01_125ng_2pt0-IT22_Run-A_16Oct17_Pippin_AQ_17-10-01.raw", 15000, 15006)]
        [TestCase("Blank04_29Mar17_Smeagol.raw", 800, 810)]
        public void TestScanEventData(string rawFileName, int scanStart, int scanEnd)
        {
            // Keys in this Dictionary are filename (without the extension), values are ScanCounts by event, where the key is a Tuple of EventName and EventValue
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

            AddExpectedTupleAndCount(expectedData, "Blank04_29Mar17_Smeagol", "Average Scan by Inst:", "Yes", 11);
            AddExpectedTupleAndCount(expectedData, "Blank04_29Mar17_Smeagol", "Micro Scan count:", "1", 11);

            var dataFile = GetRawDataFile(rawFileName);

            if (!expectedData.TryGetValue(Path.GetFileNameWithoutExtension(dataFile.Name), out var expectedEventsThisFile))
            {
                Assert.Fail($"Dataset {dataFile.Name} not found in dictionary expectedData");
            }

            var eventsToCheck = (from item in expectedEventsThisFile select item.Key.Item1).Distinct().ToList();
            var eventCountsActual = new Dictionary<Tuple<string, string>, int>();

            using var reader = new XRawFileIO(dataFile.FullName);

            for (var scanNumber = scanStart; scanNumber <= scanEnd; scanNumber++)
            {
                var success = reader.GetScanInfo(scanNumber, out var scanInfo);

                Assert.That(success, Is.True, $"GetScanInfo returned false for scan {scanNumber}");

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

            Console.WriteLine("{0,-5} {1,-5} {2}", "Valid", "Count", "Event");

            foreach (var observedEvent in (from item in eventCountsActual orderby item.Key select item))
            {
                if (expectedEventsThisFile.TryGetValue(observedEvent.Key, out var expectedScanCount))
                {
                    var isValid = observedEvent.Value == expectedScanCount;

                    Console.WriteLine("{0,-5} {1,-5} {2} {3}", isValid, observedEvent.Value, observedEvent.Key.Item1, observedEvent.Key.Item2);

                    Assert.That(observedEvent.Value, Is.EqualTo(expectedScanCount), "Event count mismatch");
                }
                else
                {
                    Console.WriteLine("Unexpected event/value found: {0} {1}", observedEvent.Key.Item1, observedEvent.Key.Item2);
                    Assert.Fail($"Unexpected event/value found: {observedEvent.Key.Item1} {observedEvent.Key.Item2}");
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
            IDictionary<string, Dictionary<Tuple<int, double>, int>> expectedData,
            string fileName,
            int tupleKey1,
            double tupleKey2,
            int scanCount)
        {
            if (!expectedData.TryGetValue(fileName, out var expectedScanInfo))
            {
                expectedScanInfo = new Dictionary<Tuple<int, double>, int>();
                expectedData.Add(fileName, expectedScanInfo);
            }

            expectedScanInfo.Add(new Tuple<int, double>(tupleKey1, tupleKey2), scanCount);
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

        private FileInfo GetRawDataFile(string rawFileName, bool skipIfMissing = false)
        {
            var localDirPath = Path.Combine("..", "..", "Docs");
            const string remoteDirPath = @"\\proto-2\UnitTest_Files\ThermoRawFileReader";

            var compileOutputDirectory = TestContext.CurrentContext.TestDirectory;

            var localFile = new FileInfo(Path.Combine(compileOutputDirectory, localDirPath, rawFileName));

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

            if (skipIfMissing)
            {
                Console.WriteLine("Skipping raw file since not found: " + rawFileName);
                return null;
            }

            var msg = string.Format("File not found: {0}; checked in both {1} and {2}", rawFileName, localDirPath, remoteDirPath);

            Console.WriteLine(msg);
            Assert.Fail(msg);

            return null;
        }
    }
}
