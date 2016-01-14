using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThermoRawFileReaderDLL.FinniganFileIO;

namespace RawFileReaderTests
{
    [TestFixture]
    public class ThermoFilterStringTests
    {

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                             ", FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM)]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                          ", FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                                          ", FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                                         ", FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM)]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                        ", FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                ", FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM)]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                  ", FinniganFileReaderBaseClass.MRMScanTypeConstants.MRMQMS)]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                   ", FinniganFileReaderBaseClass.MRMScanTypeConstants.MRMQMS)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                         ", FinniganFileReaderBaseClass.MRMScanTypeConstants.FullNL)]
        public void DetermineMRMScanType(string filterText, FinniganFileReaderBaseClass.MRMScanTypeConstants expectedResult)
        {
            var mrmScanType = XRawFileIO.DetermineMRMScanType(filterText);

            Console.WriteLine(filterText + " " + mrmScanType);

            Assert.AreEqual(expectedResult, mrmScanType);
        }

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                             ", FinniganFileReaderBaseClass.IonModeConstants.Positive)]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                             ", FinniganFileReaderBaseClass.IonModeConstants.Positive)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                          ", FinniganFileReaderBaseClass.IonModeConstants.Positive)]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                    ", FinniganFileReaderBaseClass.IonModeConstants.Positive)]
        [TestCase("- p NSI Full ms2 168.070 [300.000-1500.00]                        ", FinniganFileReaderBaseClass.IonModeConstants.Negative)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                         ", FinniganFileReaderBaseClass.IonModeConstants.Unknown)]
        public void DetermineIonizationMode(string filterText, FinniganFileReaderBaseClass.IonModeConstants expectedResult)
        {
            var ionizationMode = XRawFileIO.DetermineIonizationMode(filterText);

            Console.WriteLine(filterText + " " + ionizationMode);

            Assert.AreEqual(expectedResult, ionizationMode);
        }

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                ", "")]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                ", "")]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]     ", "179.652-184.582; 505.778-510.708; 994.968-999.898")]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                      ", "150.070-1500.000")]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                            ", "300.000-1500.00")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]   ", "507.259-507.261; 635-319-635.32")]
        public void ExtractMRMMasses(string filterText, string expectedMassList)
        {
            FinniganFileReaderBaseClass.udtMRMInfoType udtMRMInfo;

            var mrmScanType = XRawFileIO.DetermineMRMScanType(filterText);
            XRawFileIO.ExtractMRMMasses(filterText, mrmScanType, out udtMRMInfo);

            Console.WriteLine(filterText + " -- " + udtMRMInfo.MRMMassCount + " mass ranges");

            if (string.IsNullOrWhiteSpace(expectedMassList))
            {
                Assert.AreEqual(0, udtMRMInfo.MRMMassCount, "Mass range count mismatch");
                return;
            }

            var expectedMassRanges = expectedMassList.Split(';');
            Assert.AreEqual(expectedMassRanges.Length, udtMRMInfo.MRMMassCount, "Mass range count mismatch");

            for (var i = 0; i < expectedMassRanges.Length; i++)
            {
                var expectedMassValues = expectedMassRanges[i].Split('-');

                var massStart= double.Parse(expectedMassValues[0]);
                var massEnd = double.Parse(expectedMassValues[1]);

                Assert.AreEqual(massStart, udtMRMInfo.MRMMassList[i].StartMass, .001, "Mass start mismatch");
                Assert.AreEqual(massEnd, udtMRMInfo.MRMMassList[i].EndMass, .001, "Mass end mismatch");
            }
        }

        [Test]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                      ", 3, "1312.95@45.00 873.85@45.00 [ 350.00-2000.00]")]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                            ", 10, "421.76@35.00")]
        [TestCase("ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]                      ", 2, "467.16@etd100.00 [50.00-1880.00]")]
        [TestCase("ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]                         ", 2, "467.16@etd100.00 [50.00-1880.00]")]
        [TestCase("ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]                         ", 2, "756.98@cid35.00 [195.00-2000.00]")]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                          ", 2, "606.30@pqd27.00 [50.00-2000.00]")]
        [TestCase("ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]                          ", 2, "342.90@cid35.00 [50.00-2000.00]")]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                            ", 1, "")]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                            ", 1, "")]
        [TestCase("ITMS + p ESI d Z ms [1108.00-1118.00]                                            ", 1, "")]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                         ", 2, "777.00@cid30.00 [210.00-1200.00]")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]               ", 2, "501.560@cid15.00 [507.259-507.261, 635-319-635.32]")]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]    ", 2, "712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]   ", 2, "1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]   ", 2, "1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", 2, "748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]")]     

        public void ExtractMSLevel(string filterText, int expectedMSLevel, string expectedMzText)
        {

            string parentIonMZ;
            int intMSLevel;

            var success = XRawFileIO.ExtractMSLevel(filterText, out intMSLevel, out parentIonMZ);

            Console.WriteLine(filterText + " -- ms" + intMSLevel + ", " + parentIonMZ);

            if (string.IsNullOrEmpty(expectedMzText))
            {
                Assert.AreEqual(false, success, "ExtractMSLevel returned true; expected false");
                return;
            }

            Assert.AreEqual(true, success, "ExtractMSLevel returned false");

            Assert.AreEqual(expectedMSLevel, intMSLevel, "MS level mismatch");
            Assert.AreEqual(expectedMzText, parentIonMZ, "mzText mismatch");

        }

        /// <summary>
        /// Test ExtractParentIonMZFromFilterText
        /// </summary>
        /// <param name="filterText">FilterText</param>
        /// <param name="expectedParentIons">ParentIon Mz (could be comma-separated list, with ! marking the "best" parent ion m/z)</param>
        /// <param name="expectedMSLevel"></param>
        /// <param name="expectedCollisionMode"></param>
        [Test]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                      ", "1312.95, 873.85!", 3, "")]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                            ", "421.76", 10, "")]
        [TestCase("ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]                      ", "467.16", 2, "sa_etd")]
        [TestCase("ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]                         ", "467.16", 2, "etd")]
        [TestCase("ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]                         ", "756.98", 2, "cid")]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                          ", "606.3", 2, "pqd")]
        [TestCase("ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]                          ", "342.9", 2, "cid")]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                            ", "0", 1, "")]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                            ", "0", 1, "")]
        [TestCase("ITMS + p ESI d Z ms [1108.00-1118.00]                                            ", "0", 1, "")]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                         ", "777", 2, "cid")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]               ", "501.56", 2, "cid")]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]    ", "712.85!, 407.92", 2, "hcd")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]   ", "1073.48", 2, "ETciD")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]   ", "1073.48", 2, "EThcD")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", "748.371", 2, "")]
        public void ExtractParentIonMZFromFilterText(string filterText, string expectedParentIons, int expectedMSLevel, string expectedCollisionMode)
        {

            double dblParentIonMZ;
            int intMSLevel;
            string collisionMode;
            List<XRawFileIO.udtParentIonInfoType> lstParentIons;

            var success = XRawFileIO.ExtractParentIonMZFromFilterText(filterText, out dblParentIonMZ, out intMSLevel, out collisionMode, out lstParentIons);

            Console.WriteLine(filterText + " -- ms" + intMSLevel + ", " + dblParentIonMZ.ToString("0.00") + " " + collisionMode);

            if (expectedMSLevel == 1)
            {
                Assert.AreEqual(false, success, "ExtractParentIonMZFromFilterText returned true; should have returned false");
                return;
            }

            Assert.AreEqual(true, success, "ExtractParentIonMZFromFilterText returned false");

            var parentIons = expectedParentIons.Split(',');
            var expectedParenIonMZ = double.Parse(parentIons[0].Replace("!", ""));
            foreach (var parentIon in parentIons)
            {
                if (parentIon.Contains('!'))
                    expectedParenIonMZ = double.Parse(parentIon.Replace("!", ""));
            }

            Assert.AreEqual(expectedMSLevel, intMSLevel, "MS level mismatch");
            Assert.AreEqual(parentIons.Length, lstParentIons.Count, "Parent ion count mismatch");

            Assert.AreEqual(expectedParenIonMZ, dblParentIonMZ, 0.001, "Parent ion m/z mismatch");

            Assert.AreEqual(expectedCollisionMode, collisionMode, "Collision mode mismatch");

            if (parentIons.Length > 0)
            {
                for (var i = 0; i < parentIons.Length; i++)
                {
                    var expectedParentIonMZ = double.Parse(parentIons[i].Replace("!", ""));
                    var actualParentIon = lstParentIons[i].ParentIonMZ;

                    Assert.AreEqual(expectedParentIonMZ, actualParentIon, .001,
                                    "Parent ion mismatch, ion " + (i + 1).ToString());
                }
            }
        }

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                          ", "FTMS + p NSI Full ms")]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                          ", "ITMS + c ESI Full ms")]
        [TestCase("ITMS + p ESI d Z ms [579.00-589.00]                                            ", "ITMS + p ESI d Z ms")]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                       ", "FTMS + c NSI d Full ms2 0@hcd40.00")]
        [TestCase("ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]                       ", "ITMS + c ESI d Full ms2 0@cid35.00")]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                        ", "ITMS + c NSI d Full ms2 0@pqd27.00")]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                          ", "ITMS + c NSI d Full ms10 0@35.00")]
        [TestCase("ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]                    ", "ITMS + c NSI d sa Full ms2 0@etd100.00")]
        [TestCase("ITMS + c NSI r d sa Full ms2 996.8542@etd120.55@cid20.00 [120.0000-2000.0000]  ", "ITMS + c NSI r d sa Full ms2 0@etd120.55@cid20.00")]
        [TestCase("+ c NSI Full ms2 1083.000 [300.000-1500.00]                                    ", "+ c NSI Full ms2")]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                       ", "+ p ms2 0@cid30.00")]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                 ", "+ c d Full ms2 0@45.00")]
        [TestCase("- p NSI Full ms2 168.070 [300.000-1500.00]                                     ", "- p NSI Full ms2")]
        [TestCase("- p NSI Full ms2 247.060 [300.000-1500.00]                                     ", "- p NSI Full ms2")]
        [TestCase("- c NSI d Full ms2 921.597 [300.000-1500.00]                                   ", "- c NSI d Full ms2")]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                    ", "+ c d Full ms3 0@45.00 0@45.00")]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]               ", "+ p NSI Q1MS")]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                ", "+ p NSI Q3MS")]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                      ", "c NSI Full cnl")]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                                       ", "+ c EI SRM ms2")]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                                      ", "+ c NSI SRM ms2")]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                     ", "+ p NSI SRM ms2")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]             ", "+ c NSI SRM ms2 0@cid15.00")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", "+ c NSI SRM ms2")]
        public void TestGenericScanFilter(string filterText, string expectedResult)
        {
            var genericFilterResult = XRawFileIO.MakeGenericFinniganScanFilter(filterText);

            Console.WriteLine(filterText + " " + genericFilterResult);

            Assert.AreEqual(expectedResult, genericFilterResult);

        }

        [Test]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                                       ", "MS")]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                                       ", "HMS")]
        [TestCase("ITMS + p ESI d Z ms [579.00-589.00]                                                         ", "Zoom-MS")]
        [TestCase("ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]                                    ", "CID-MSn")]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                                     ", "PQD-MSn")]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                                    ", "HCD-HMSn")]
        [TestCase("ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]                                 ", "SA_ETD-MSn")]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]               ", "HCD-HMSn")]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                              ", "MSn")]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                                 ", "MSn")]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                                       ", "MSn")]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                                       ", "MSn")]
        [TestCase("ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [242.00-248.00, 285.00-291.00]         ", "CID-MSn")]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                                    ", "CID-MSn")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                          ", "CID-SRM")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", "CID-SRM")]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                            ", "Q1MS")]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                             ", "Q3MS")]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                                   ", "MRM_Full_NL")]
        [TestCase("FTMS + p NSI Full ms                                                                        ", "HMS")]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@cid30.00 [247.0000-2000.0000]                            ", "CID-MSn")]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@hcd30.00 [100.0000-2000.0000]                            ", "HCD-MSn")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]              ", "ETciD-MSn")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]              ", "EThcD-MSn")]
        [TestCase("FTMS + c NSI r d Full ms2 744.0129@cid30.00 [199.0000-2000.0000]                            ", "CID-HMSn")]
        [TestCase("FTMS + p NSI r d Full ms2 944.4316@hcd30.00 [100.0000-2000.0000]                            ", "HCD-HMSn")]
        [TestCase("FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]              ", "ETciD-HMSn")]
        [TestCase("FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]              ", "EThcD-HMSn")]
        public void TestScanTypeName(string filterText, string expectedResult)
        {
            var scanTypeName = XRawFileIO.GetScanTypeNameFromFinniganScanFilterText(filterText);

            Console.WriteLine(filterText + " " + scanTypeName);

            Assert.AreEqual(expectedResult, scanTypeName);

        }

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                             ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                          ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                                          ", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                                         ", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM, false)]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                        ", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                ", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.SRM, false)]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                  ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.MRMQMS, false)]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                   ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.MRMQMS, false)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                         ", true, 2, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.FullNL, false)]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                             ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                    ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- p NSI Full ms2 168.070 [300.000-1500.00]                                        ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                       ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                             ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]                       ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]                          ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]                          ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                           ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]                           ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + p ESI d Z ms [1108.00-1118.00]                                             ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, true)]
        [TestCase("ITMS + p ESI d Z ms [579.00-589.00]                                               ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, true)]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                          ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]     ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]    ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]    ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]                          ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]                       ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c NSI Full ms2 1083.000 [300.000-1500.00]                                       ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- p NSI Full ms2 247.060 [300.000-1500.00]                                        ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- c NSI d Full ms2 921.597 [300.000-1500.00]                                      ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 996.8542@etd120.55@cid20.00 [120.0000-2000.0000]     ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [242.00-248.00, 285.00-291.00]  ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI Full ms                                                              ", true, 1, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@cid30.00 [247.0000-2000.0000]                  ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@hcd30.00 [100.0000-2000.0000]                  ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI r d Full ms2 744.0129@cid30.00 [199.0000-2000.0000]                  ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI r d Full ms2 944.4316@hcd30.00 [100.0000-2000.0000]                  ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]    ", false, 0, false, FinniganFileReaderBaseClass.MRMScanTypeConstants.NotMRM, false)]
        public void ValidateMSScan(
            string filterText, 
            bool expectedIsValid, 
            int expectedMSLevel, 
            bool expectedIsSIMScan,
            FinniganFileReaderBaseClass.MRMScanTypeConstants expectedMRMScanType, bool expectedIsZoomScan)
        {
            int intMSLevel;
            bool isSIMScan;
            FinniganFileReaderBaseClass.MRMScanTypeConstants mrmScanType;
            bool zoomScan;

            var isValid = XRawFileIO.ValidateMSScan(filterText, out intMSLevel, out isSIMScan, out mrmScanType, out zoomScan);

            Console.WriteLine(filterText + "  -- ms" + intMSLevel + "; SIM=" + isSIMScan + "; MRMScanType=" + mrmScanType);

            Assert.AreEqual(expectedIsValid, isValid, "Validation mismatch");
            Assert.AreEqual(expectedMSLevel, intMSLevel, "MSLevel mismatch");
            Assert.AreEqual(expectedIsSIMScan, isSIMScan, "SIMScan mismatch");
            Assert.AreEqual(expectedMRMScanType, mrmScanType, "MRMScanType mismatch");
            Assert.AreEqual(expectedIsZoomScan, zoomScan, "ZoomScan mismatch");

        }
    }
}
