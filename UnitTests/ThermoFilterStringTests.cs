using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThermoRawFileReader;

namespace RawFileReaderTests
{
    [TestFixture]
    public class ThermoFilterStringTests
    {

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                             ", MRMScanTypeConstants.NotMRM)]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                          ", MRMScanTypeConstants.NotMRM)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                                          ", MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                                         ", MRMScanTypeConstants.SRM)]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                        ", MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                ", MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", MRMScanTypeConstants.SRM)]
        [TestCase("+ c NSI SIM pr 95.099 [105.099-105.101, 150.099-150.101, 370.199-370.201, 208.099-208.101, 300.199-300.201, 304.199-304.201]", MRMScanTypeConstants.SRM)]
        [TestCase("FTMS - p NSI w SIM ms [817.00-917.00]                                             ", MRMScanTypeConstants.SIM)]
        [TestCase("FTMS + c NSI d SIM ms [782.00-792.00]                                             ", MRMScanTypeConstants.SIM)]
        [TestCase("ITMS + c NSI SIM ms [286.50-289.50]                                               ", MRMScanTypeConstants.SIM)]
        [TestCase("+ c EI SRM ms2 160.000 [72.999-73.001]                                            ", MRMScanTypeConstants.SRM)]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                  ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                   ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("- p NSI Q3MS [807.100-810.100, 851.100-854.100]                                   ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("+ c NSI Q3MS [380.000-1350.000]                                                   ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("+ c CI Q1MS [50.000-600.000]                                                      ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("+ c EI Q1MS [50.000-600.000]                                                      ", MRMScanTypeConstants.MRMQMS)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                         ", MRMScanTypeConstants.FullNL)]
        public void DetermineMRMScanType(string filterText, MRMScanTypeConstants expectedResult)
        {
            var mrmScanType = XRawFileIO.DetermineMRMScanType(filterText);

            Console.WriteLine(filterText + " " + mrmScanType);

            Assert.AreEqual(expectedResult, mrmScanType);
        }

        [Test]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                             ", IonModeConstants.Positive)]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                             ", IonModeConstants.Positive)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                          ", IonModeConstants.Positive)]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                    ", IonModeConstants.Positive)]
        [TestCase("- p NSI Full ms2 168.070 [300.000-1500.00]                        ", IonModeConstants.Negative)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                         ", IonModeConstants.Unknown)]
        public void DetermineIonizationMode(string filterText, IonModeConstants expectedResult)
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
        [TestCase("+ c NSI SIM pr 95.099 [105.099-105.101, 150.099-150.101, 370.199-370.201, 208.099-208.101]", "105.099-105.101; 150.099-150.101; 370.199-370.201; 208.099-208.101")]
        [TestCase("FTMS - p NSI w SIM ms [817.00-917.00]                                ", "817.00-917.00")]
        [TestCase("FTMS + c NSI d SIM ms [782.00-792.00]                                ", "782.00-792.00")]
        [TestCase("ITMS + c NSI SIM ms [286.50-289.50]                                  ", "286.50-289.50")]
        public void ExtractMRMMasses(string filterText, string expectedMassList)
        {
            MRMInfo udtMRMInfo;

            var mrmScanType = XRawFileIO.DetermineMRMScanType(filterText);
            XRawFileIO.ExtractMRMMasses(filterText, mrmScanType, out udtMRMInfo);

            Console.WriteLine(filterText + " -- " + udtMRMInfo.MRMMassList.Count + " mass ranges");

            if (string.IsNullOrWhiteSpace(expectedMassList))
            {
                Assert.AreEqual(0, udtMRMInfo.MRMMassList.Count, "Mass range count mismatch");
                return;
            }

            var expectedMassRanges = expectedMassList.Split(';');
            Assert.AreEqual(expectedMassRanges.Length, udtMRMInfo.MRMMassList.Count, "Mass range count mismatch");

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
            int msLevel;

            var success = XRawFileIO.ExtractMSLevel(filterText, out msLevel, out parentIonMZ);

            Console.WriteLine(filterText + " -- ms" + msLevel + ", " + parentIonMZ);

            if (string.IsNullOrEmpty(expectedMzText))
            {
                Assert.AreEqual(false, success, "ExtractMSLevel returned true; expected false");
                return;
            }

            Assert.AreEqual(true, success, "ExtractMSLevel returned false");

            Assert.AreEqual(expectedMSLevel, msLevel, "MS level mismatch");
            Assert.AreEqual(expectedMzText, parentIonMZ, "mzText mismatch");

        }

        /// <summary>
        /// Test ExtractParentIonMZFromFilterText(string, out double, out int, out string, out list of udtParentIonInfoType)
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
        [TestCase("+ c NSI SRM ms2 400.576 [376.895-376.897, 459.772-459.774, 516.314-516.316]      ", "400.576", 2, "")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", "748.371", 2, "")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]               ", "501.56", 2, "cid")]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                       ", "1025.250", 2, "")]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]    ", "712.85!, 407.92", 2, "hcd")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]   ", "1073.48", 2, "ETciD")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]   ", "1073.48", 2, "EThcD")]
        [TestCase("FTMS + p NSI Full ms                                                             ", "0", 1, "")]
        [TestCase("0                                                                                                                            ", "0", 1, "")]
        [TestCase("ITMS + p NSI d Z ms [351.00-361.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + c NSI Full ms [150.00-700.00]                                                                                         ", "0", 1, "")]
        [TestCase("ITMS + p NSI SIM ms [329.10-335.10]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [240.00-250.00, 309.00-319.00]                                          ", "332.14, 288.10!", 3, "cid")]
        [TestCase("ITMS + p NSI SRM ms2 332.00@cid35.00 [283.00-293.00]                                                                         ", "332.00", 2, "cid")]
        [TestCase("ITMS + c NSI Full ms2 195.00@cid35.00 [50.00-300.00]                                                                         ", "195.00", 2, "cid")]
        [TestCase("ITMS + p NSI CRM ms4 332.00@cid50.00 288.00@cid50.00 245.00@cid0.00 [200.00-300.00]                                          ", "332.00, 288.00, 245.00!", 4, "cid")]
        [TestCase("FTMS + p NSI d Full ms2 819.00@cid35.00 [215.00-830.00]                                                                      ", "819.00", 2, "cid")]
        [TestCase("FTMS + p NSI d Full ms2 819.00@etd20.00 [100.00-830.00]                                                                      ", "819.00", 2, "etd")]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS - p NSI d Full ms [100.00-300.00]                                                                                       ", "0", 1, "")]
        [TestCase("ITMS - p NSI Full ms [100.00-300.00]                                                                                         ", "0", 1, "")]
        [TestCase("FTMS + p NSI d Full ms2 1161.19@pqd35.00 [50.00-2000.00]                                                                     ", "1161.19", 2, "pqd")]
        [TestCase("ITMS + c NSI d Full ms2 370.98@cid35.00 [90.00-755.00]                                                                       ", "370.98", 2, "cid")]
        [TestCase("ITMS + c NSI d Full ms3 346.00@cid35.00 258.07@cid35.00 [60.00-530.00]                                                       ", "346.00, 258.07!", 3, "cid")]
        [TestCase("ITMS + c NSI d Full ms4 205.09@cid35.00 177.09@cid35.00 131.08@cid35.00 [50.00-275.00]                                       ", "205.09, 177.09, 131.08!", 4, "cid")]
        [TestCase("ITMS + c NSI Full ms3 332.10@cid35.00 288.10@cid35.00 [75.00-350.00]                                                         ", "332.10, 288.10!", 3, "cid")]
        [TestCase("ITMS + c NSI SIM ms [286.50-289.50]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + p ESI Full ms [300.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS + c ESI d w Full ms2 323.44@cid35.00 [75.00-660.00]                                                                     ", "323.44", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms [400.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS - c NSI Full ms [400.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS + p NSI d Full ms2 901.47@cid30.00 [235.00-1815.00]                                                                     ", "901.47", 2, "cid")]
        [TestCase("FTMS - p NSI Full ms2 652.20@cid0.00 [175.00-1200.00]                                                                        ", "652.20", 2, "cid")]
        [TestCase("FTMS - c NSI Full ms2 223.03@hcd32.00 [50.00-475.00]                                                                         ", "223.03", 2, "hcd")]
        [TestCase("ITMS - p NSI d Full ms2 145.01@cid35.00 [50.00-305.00]                                                                       ", "145.01", 2, "cid")]
        [TestCase("FTMS - p NSI Full ms [70.00-910.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS - p NSI d Full ms3 347.50@cid35.00 317.88@cid35.00 [75.00-650.00]                                                       ", "347.50, 317.88!", 3, "cid")]
        [TestCase("FTMS + p NSI Full ms2 480.30@hcd28.00 [66.67-1000.00]                                                                        ", "480.30", 2, "hcd")]
        [TestCase("FTMS + p NSI d Full ms2 613.39@hcd32.00 [100.00-1240.00]                                                                     ", "613.39", 2, "hcd")]
        [TestCase("ITMS + p NSI Full ms [400.00-2000.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 413.27@hcd30.00 [100.00-865.00]                                                                      ", "413.27", 2, "hcd")]
        [TestCase("ITMS + c NSI d w Full ms2 371.10@cid35.00 [90.00-385.00]                                                                     ", "371.10", 2, "cid")]
        [TestCase("FTMS + c NSI d Full ms3 437.26@cid35.00 654.47@hcd45.00 [100.00-2000.00]                                                     ", "437.26, 654.47!", 3, "hcd")] // ms2 is cid
        [TestCase("FTMS + p NSI sid=30.00  Full ms [500.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("FTMS + c NSI sid=30.00  d Full ms2 859.56@hcd30.00 [100.00-2000.00]                                                          ", "859.56", 2, "hcd")]
        [TestCase("ITMS - c NSI d Full ms2 191.87@cid35.00 [50.00-780.00]                                                                       ", "191.87", 2, "cid")]
        [TestCase("ITMS + c NSI r d Full ms2 421.8336@hcd35.00 [110.0000-854.0000]                                                              ", "421.8336", 2, "hcd")]
        [TestCase("FTMS - p ESI Full ms [50.00-500.00]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS - c ESI d Full ms2 112.99@cid35.00 [50.00-350.00]                                                                       ", "112.99", 2, "cid")]
        [TestCase("FTMS - c ESI d Full ms3 226.98@cid35.00 117.02@cid35.00 [50.00-365.00]                                                       ", "226.98, 117.02!", 3, "cid")]
        [TestCase("- p NSI Full ms2 218.110 [50.100-250.000]                                                                                    ", "218.110", 2, "")]
        [TestCase("FTMS + p NSI sa Full ms2 968.57@etd10.00 [200.00-2000.00]                                                                    ", "968.57", 2, "sa_etd")]
        [TestCase("ITMS + p NSI d Full ms2 430.89@etd150.00 [50.00-875.00]                                                                      ", "430.89", 2, "etd")]
        [TestCase("+ p NSI Full pr 150.000 [150.100-400.000]                                                                                    ", "0", 1, "")]
        [TestCase("+ p NSI Q1MS [150.100-400.000]                                                                                               ", "0", 1, "")]
        [TestCase("FTMS + p NSI Full ms2 1100.00@cid0.00 [300.00-2000.00]                                                                       ", "1100.00", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms2 808.41@hcd28.00 [100.00-2000.00]                                                                       ", "808.41", 2, "hcd")]
        [TestCase("ITMS + p NSI sid=35.00  Full ms [210.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("FTMS + p NSI Full msx ms2 636.04@hcd28.00 641.04@hcd28.00 654.05@hcd28.00 [88.00-1355.00]                                    ", "636.04, 641.04, 654.05", 2, "hcd")]
        [TestCase("FTMS + c NSI Full msx ms2 818.12@hcd27.00 863.14@hcd27.00 838.13@hcd27.00 638.04@hcd27.00 [88.33-1785.00]                    ", "818.12, 863.14, 838.13, 638.04", 2, "hcd")]
        [TestCase("FTMS + p NSI SIM ms [521.90-523.90]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 1303.24@cid35.00 [345.00-2000.00]                                                                    ", "1303.24", 2, "cid")]
        [TestCase("FTMS + p NSI Full ms2 881.00@etd200.00 [240.00-2000.00]                                                                      ", "881.00", 2, "etd")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@cid35.00 [270.00-2000.00]                                                            ", "994.00", 2, "cid")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@etd100.00 [400.00-2000.00]                                                           ", "994.00", 2, "etd")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@hcd1.00 [500.00-2000.00]                                                             ", "994.00", 2, "hcd")]
        [TestCase("ITMS + c NSI sid=35.00  d Full ms2 445.12@cid35.00 [110.00-460.00]                                                           ", "445.12", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms2 612.78@cid35.00 [165.00-2000.00]                                                                       ", "612.78", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms3 559.30@cid35.00 421.51@cid35.00 [115.00-2000.00]                                                       ", "559.30, 421.51!", 3, "cid")]
        [TestCase("FTMS {0,0}  - p NSI Full ms [250.00-2400.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI r d Full ms2 394.7961@cid30.00 [103.0000-800.0000]                                                              ", "394.7961", 2, "cid")]
        [TestCase("ITMS + c NSI r d Full ms2 626.4867@etd30.14 [120.0000-2000.0000]                                                             ", "626.4867", 2, "etd")]
        [TestCase("ITMS + c NSI r d sa Full ms2 437.5289@etd53.58@cid20.00 [120.0000-1323.0000]                                                 ", "437.5289", 2, "ETciD")]
        [TestCase("ITMS + c NSI r d sa Full ms2 437.5289@etd53.58@hcd20.00 [120.0000-1323.0000]                                                 ", "437.5289", 2, "EThcD")]
        [TestCase("- c NSI Q3MS [65.000-900.000]                                                                                                ", "0", 1, "")]
        [TestCase("ITMS + c NSI sid=5.00  Full ms [150.00-2000.00]                                                                              ", "0", 1, "")]
        [TestCase("ITMS - c NSI Full ms [75.00-1500.00]                                                                                         ", "0", 1, "")]
        [TestCase("ITMS + p NSI Z ms [150.00-2000.00]                                                                                           ", "0", 1, "")]
        [TestCase("+ c NSI Full ms2 856.930 [300.000-1200.000]                                                                                  ", "856.930", 2, "")]
        [TestCase("ITMS + c NSI d Full ms2 916.13@pqd28.50 [50.00-2000.00]                                                                      ", "916.13", 2, "pqd")]
        [TestCase("ITMS + c NSI d sa Full ms2 377.19@etd100.00 [50.00-1145.00]                                                                  ", "377.19", 2, "sa_etd")]
        [TestCase("+ c NSI Full cnl 162.053 [300.000-1200.000]                                                                                  ", "0", 1, "")]
        [TestCase("- c NSI Full pr 225.007 [300.000-1200.000]                                                                                   ", "0", 1, "")]
        [TestCase("- c NSI Q1MS [600.000-1000.000]                                                                                              ", "0", 1, "")]
        [TestCase("- c NSI d Full ms2 671.207 [10.000-676.207]                                                                                  ", "671.207", 2, "")]
        [TestCase("- p NSI Q3MS [807.100-810.100, 851.100-854.100]                                                                              ", "0", 1, "")]
        [TestCase("FTMS + p EI Full ms [50.00-600.00]                                                                                           ", "0", 1, "")]
        [TestCase("FTMS + p EI Full lock ms [50.00-600.00]                                                                                      ", "0", 1, "")]
        [TestCase("FTMS + p NSI sps d Full ms3 432.8866@hcd30.00 149.0201@hcd65.00 [110.0000-500.0000]                                          ", "432.8866, 149.0201!", 3, "hcd")]
        [TestCase("FTMS + c ESI d Full ms2 391.29@hcd30.00 [90.00-405.00]                                                                       ", "391.29", 2, "hcd")]
        [TestCase("ITMS + c ESI d Full ms2 391.29@cid35.00 [65.00-405.00]                                                                       ", "391.29", 2, "cid")]
        [TestCase("FTMS + p NSI d SIM ms [613.00-623.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 632.55@etd100.00 [100.00-2000.00]                                                                    ", "632.55", 2, "etd")]
        [TestCase("FTMS + p NSI d sa Full ms2 858.56@etd33.33 [100.00-2000.00]                                                                  ", "858.56", 2, "sa_etd")]
        [TestCase("FTMS + p NSI Full ms3 1395.00@cid40.00 769.00@cid26.00 [210.00-2000.00]                                                      ", "1395.00, 769.00!", 3, "cid")]
        [TestCase("FTMS + p NSI d sa Full ms2 686.2169@etd10.00@hcd10.00 [110.0000-2000.0000]                                                   ", "686.2169", 2, "EThcD")]
        [TestCase("FTMS + p NSI d sa Full ms2 686.2169@etd10.00@cid10.00 [110.0000-2000.0000]                                                   ", "686.2169", 2, "ETciD")]
        [TestCase("FTMS + c NSI d sa Full ms2 430.89@etd50.00 [100.00-1735.00]                                                                  ", "430.89", 2, "sa_etd")]
        [TestCase("ITMS + c NSI d Full ms2 327.01@etd500.00 [50.00-1320.00]                                                                     ", "327.01", 2, "etd")]
        [TestCase("FTMS - c ESI d Full ms2 255.23@hcd30.00 [90.00-525.00]                                                                       ", "255.23", 2, "hcd")]
        [TestCase("ITMS - c ESI d Full ms2 255.23@cid35.00 [50.00-525.00]                                                                       ", "255.23", 2, "cid")]
        [TestCase("FTMS {1,1}  - p ESI Full ms [150.00-1800.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS - p NSI Full ms2 988.50@cid0.00 [270.00-1000.00]                                                                        ", "988.50", 2, "cid")]
        [TestCase("FTMS {1,1}  - p NSI Full ms [150.00-1000.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI r d w Full ms2 255.27@cid35.00 [60.00-2000.00]                                                                  ", "255.27", 2, "cid")]
        [TestCase("ITMS - c ESI Full ms [65.00-800.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + c ESI Full ms [65.00-800.00]                                                                                          ", "0", 1, "")]
        [TestCase("+ c NSI Full ms [400.00-2000.00]                                                                                             ", "0", 1, "")]
        [TestCase("+ c NSI d Full ms2 550.74@cid35.00 [140.00-1115.00]                                                                          ", "550.74", 2, "cid")]
        [TestCase("FTMS + p ESI d Full ms2 112.89@cid35.00 [50.00-240.00]                                                                       ", "112.89", 2, "cid")]
        [TestCase("+ c NSI Q3MS [380.000-1350.000]                                                                                              ", "0", 1, "")]
        [TestCase("FTMS + c NSI d SIM ms [782.00-792.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS {0,0}  + p NSI Full ms [400.00-2000.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI d SIM ms [428.00-438.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS {1,1}  + p NSI Full ms2 1000.00@hcd25.00 [300.00-2000.00]                                                               ", "1000.00", 2, "hcd")]
        [TestCase("FTMS {1,1}  + p NSI sid=35.00  Full ms [300.00-2000.00]                                                                      ", "0", 1, "")]
        [TestCase("FTMS + c NSI d sa Full ms2 738.2079@etd19.29@hcd20.00 [120.0000-2000.0000]                                                   ", "738.2079", 2, "EThcD")]
        [TestCase("FTMS + c NSI k d Full ms3 978.7619@hcd30.00 256.2410@hcd65.00 [110.0000-500.0000]                                            ", "978.7619, 256.2410!", 3, "hcd")]
        [TestCase("FTMS - p NSI d Full ms2 1082.08@hcd30.00 [148.67-2230.00]                                                                    ", "1082.08", 2, "hcd")]
        [TestCase("FTMS + c NSI sid=30.00  d Full ms2 453.79@cid35.00 [110.00-2000.00]                                                          ", "453.79", 2, "cid")]
        [TestCase("+ c EI SRM ms2 160.000 [72.999-73.001]                                                                                       ", "160.000", 2, "")]
        [TestCase("FTMS + p NSI d Full ms3 850.70@cid35.00 756.71@cid35.00 [195.00-2000.00]                                                     ", "850.70, 756.71!", 3, "cid")]
        [TestCase("FTMS + p NSI sid=35.00  d Full ms2 501.11@cid35.00 [125.00-2000.00]                                                          ", "501.11", 2, "cid")]
        [TestCase("+ c EI Q1MS [50.000-600.000]                                                                                                 ", "0", 1, "")]
        [TestCase("+ c CI Q1MS [50.000-600.000]                                                                                                 ", "0", 1, "")]
        [TestCase("FTMS - p ESI d Full ms2 87.01@hcd45.00 [50.00-185.00]                                                                        ", "87.01", 2, "hcd")]
        [TestCase("FTMS + p ESI d Full ms2 445.12@hcd60.00 [100.00-460.00]                                                                      ", "445.12", 2, "hcd")]
        [TestCase("ITMS + c NSI t d Full ms2 815.2148@cid35.00 [219.0000-1641.0000]                                                             ", "815.2148", 2, "cid")]
        [TestCase("FTMS + p NSI sps d Full ms3 815.2148@cid35.00 708.8861@hcd65.00 [120.0000-500.0000]                                          ", "815.2148, 708.8861!", 3, "hcd")] // ms2 is cid
        [TestCase("+ c NSI SIM pr 95.099 [105.099-105.101, 150.099-150.101, 370.199-370.201, 208.099-208.101, 300.199-300.201, 304.199-304.201] ", "0", 1, "")]
        [TestCase("+ c NSI Full pr 1000.000 [150.000-1500.000]                                                                                  ", "0", 1, "")]
        [TestCase("+ c NSI d Full ms2 150.100 [10.000-155.100]                                                                                  ", "150.100", 2, "")]
        [TestCase("+ c NSI Q1MS [150.000-1500.000]                                                                                              ", "0", 1, "")]
        [TestCase("ITMS + c NSI d Z ms [428.00-438.00]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + c NSI Full ms2 779.60@etd25.00 [210.00-2000.00]                                                                       ", "779.60", 2, "etd")]
        [TestCase("ITMS + c NSI r d Full ms3 617.4266@cid35.00 277.2909@cid35.00 [71.0000-842.0000]                                             ", "617.4266, 277.2909!", 3, "cid")]
        [TestCase("FTMS + c NSI d sa Full ms2 963.2524@etd120.55@cid10.00 [120.0000-1937.0000]                                                  ", "963.2524", 2, "ETciD")]
        [TestCase("+ p NSI Full ms2 713.950 [200.070-1500.000]                                                                                  ", "713.950", 2, "")]
        [TestCase("+ p NSI Q3MS [200.070-1300.000]                                                                                              ", "0", 1, "")]
        [TestCase("- p NSI Q3MS [150.000-1500.000]                                                                                              ", "0", 1, "")]
        [TestCase("FTMS + p NSI sid=15.00  d Full ms2 820.19@hcd25.00 [400.00-2000.00]                                                          ", "820.19", 2, "hcd")]
        [TestCase("ITMS - p NSI sid=15.00  Full ms [500.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("+ c ESI Full ms [400.00-2000.00]                                                                                             ", "0", 1, "")]
        [TestCase("+ c d Full ms2 1013.32@cid45.00 [265.00-2000.00]                                                                             ", "1013.32", 2, "cid")]
        [TestCase("FTMS - p NSI SIM ms [330.00-380.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + p NSI Full ms2 1155.10@cid10.00 [315.00-2000.00]                                                                      ", "1155.10", 2, "cid")]
        public void ExtractParentIonMZFromFilterText(string filterText, string expectedParentIons, int expectedMSLevel, string expectedCollisionMode)
        {

            double parentIonMZ;
            int msLevel;
            string collisionMode;
            List<udtParentIonInfoType> actualParentIons;

            var success = XRawFileIO.ExtractParentIonMZFromFilterText(filterText, out parentIonMZ, out msLevel, out collisionMode, out actualParentIons);

            Console.WriteLine(filterText + " -- ms" + msLevel + ", " + parentIonMZ.ToString("0.00") + " " + collisionMode);

            if (expectedMSLevel == 1)
            {
                Assert.AreEqual(false, success, "ExtractParentIonMZFromFilterText returned true; should have returned false");
                return;
            }

            Assert.AreEqual(true, success, "ExtractParentIonMZFromFilterText returned false");

            var expectedParentIonList = expectedParentIons.Split(',');
            var expectedParentIonMZ = double.Parse(expectedParentIonList[0].Replace("!", ""));
            foreach (var parentIon in expectedParentIonList)
            {
                if (parentIon.Contains('!'))
                    expectedParentIonMZ = double.Parse(parentIon.Replace("!", ""));
            }

            Assert.AreEqual(expectedMSLevel, msLevel, "MS level mismatch");
            Assert.AreEqual(expectedParentIonList.Length, actualParentIons.Count, "Parent ion count mismatch");

            Assert.AreEqual(expectedParentIonMZ, parentIonMZ, 0.001, "Parent ion m/z mismatch");

            Assert.AreEqual(expectedCollisionMode, collisionMode, "Collision mode mismatch");

            if (expectedParentIonList.Length > 0)
            {
                for (var i = 0; i < expectedParentIonList.Length; i++)
                {
                    var expectedParentIonMz = double.Parse(expectedParentIonList[i].Replace("!", ""));
                    var actualParentIonMz = actualParentIons[i].ParentIonMZ;

                    Assert.AreEqual(expectedParentIonMz, actualParentIonMz, .001,
                                    "Parent ion mismatch, ion " + (i + 1));
                }
            }
        }

        /// <summary>
        /// Test ExtractParentIonMZFromFilterText(string, out double)
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
        [TestCase("+ c NSI SRM ms2 400.576 [376.895-376.897, 459.772-459.774, 516.314-516.316]      ", "400.576", 2, "")]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", "748.371", 2, "")]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]               ", "501.56", 2, "cid")]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                       ", "1025.250", 2, "")]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]    ", "712.85!, 407.92", 2, "hcd")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]   ", "1073.48", 2, "ETciD")]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]   ", "1073.48", 2, "EThcD")]
        [TestCase("FTMS + p NSI Full ms                                                             ", "0", 1, "")]
        [TestCase("0                                                                                                                            ", "0", 1, "")]
        [TestCase("ITMS + p NSI d Z ms [351.00-361.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + c NSI Full ms [150.00-700.00]                                                                                         ", "0", 1, "")]
        [TestCase("ITMS + p NSI SIM ms [329.10-335.10]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [240.00-250.00, 309.00-319.00]                                          ", "332.14, 288.10!", 3, "cid")]
        [TestCase("ITMS + p NSI SRM ms2 332.00@cid35.00 [283.00-293.00]                                                                         ", "332.00", 2, "cid")]
        [TestCase("ITMS + c NSI Full ms2 195.00@cid35.00 [50.00-300.00]                                                                         ", "195.00", 2, "cid")]
        [TestCase("ITMS + p NSI CRM ms4 332.00@cid50.00 288.00@cid50.00 245.00@cid0.00 [200.00-300.00]                                          ", "332.00, 288.00, 245.00!", 4, "cid")]
        [TestCase("FTMS + p NSI d Full ms2 819.00@cid35.00 [215.00-830.00]                                                                      ", "819.00", 2, "cid")]
        [TestCase("FTMS + p NSI d Full ms2 819.00@etd20.00 [100.00-830.00]                                                                      ", "819.00", 2, "etd")]
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS - p NSI d Full ms [100.00-300.00]                                                                                       ", "0", 1, "")]
        [TestCase("ITMS - p NSI Full ms [100.00-300.00]                                                                                         ", "0", 1, "")]
        [TestCase("FTMS + p NSI d Full ms2 1161.19@pqd35.00 [50.00-2000.00]                                                                     ", "1161.19", 2, "pqd")]
        [TestCase("ITMS + c NSI d Full ms2 370.98@cid35.00 [90.00-755.00]                                                                       ", "370.98", 2, "cid")]
        [TestCase("ITMS + c NSI d Full ms3 346.00@cid35.00 258.07@cid35.00 [60.00-530.00]                                                       ", "346.00, 258.07!", 3, "cid")]
        [TestCase("ITMS + c NSI d Full ms4 205.09@cid35.00 177.09@cid35.00 131.08@cid35.00 [50.00-275.00]                                       ", "205.09, 177.09, 131.08!", 4, "cid")]
        [TestCase("ITMS + c NSI Full ms3 332.10@cid35.00 288.10@cid35.00 [75.00-350.00]                                                         ", "332.10, 288.10!", 3, "cid")]
        [TestCase("ITMS + c NSI SIM ms [286.50-289.50]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + p ESI Full ms [300.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS + c ESI d w Full ms2 323.44@cid35.00 [75.00-660.00]                                                                     ", "323.44", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms [400.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS - c NSI Full ms [400.00-1700.00]                                                                                        ", "0", 1, "")]
        [TestCase("ITMS + p NSI d Full ms2 901.47@cid30.00 [235.00-1815.00]                                                                     ", "901.47", 2, "cid")]
        [TestCase("FTMS - p NSI Full ms2 652.20@cid0.00 [175.00-1200.00]                                                                        ", "652.20", 2, "cid")]
        [TestCase("FTMS - c NSI Full ms2 223.03@hcd32.00 [50.00-475.00]                                                                         ", "223.03", 2, "hcd")]
        [TestCase("ITMS - p NSI d Full ms2 145.01@cid35.00 [50.00-305.00]                                                                       ", "145.01", 2, "cid")]
        [TestCase("FTMS - p NSI Full ms [70.00-910.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS - p NSI d Full ms3 347.50@cid35.00 317.88@cid35.00 [75.00-650.00]                                                       ", "347.50, 317.88!", 3, "cid")]
        [TestCase("FTMS + p NSI Full ms2 480.30@hcd28.00 [66.67-1000.00]                                                                        ", "480.30", 2, "hcd")]
        [TestCase("FTMS + p NSI d Full ms2 613.39@hcd32.00 [100.00-1240.00]                                                                     ", "613.39", 2, "hcd")]
        [TestCase("ITMS + p NSI Full ms [400.00-2000.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 413.27@hcd30.00 [100.00-865.00]                                                                      ", "413.27", 2, "hcd")]
        [TestCase("ITMS + c NSI d w Full ms2 371.10@cid35.00 [90.00-385.00]                                                                     ", "371.10", 2, "cid")]
        [TestCase("FTMS + c NSI d Full ms3 437.26@cid35.00 654.47@hcd45.00 [100.00-2000.00]                                                     ", "437.26, 654.47!", 3, "hcd")] // ms2 is cid
        [TestCase("FTMS + p NSI sid=30.00  Full ms [500.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("FTMS + c NSI sid=30.00  d Full ms2 859.56@hcd30.00 [100.00-2000.00]                                                          ", "859.56", 2, "hcd")]
        [TestCase("ITMS - c NSI d Full ms2 191.87@cid35.00 [50.00-780.00]                                                                       ", "191.87", 2, "cid")]
        [TestCase("ITMS + c NSI r d Full ms2 421.8336@hcd35.00 [110.0000-854.0000]                                                              ", "421.8336", 2, "hcd")]
        [TestCase("FTMS - p ESI Full ms [50.00-500.00]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS - c ESI d Full ms2 112.99@cid35.00 [50.00-350.00]                                                                       ", "112.99", 2, "cid")]
        [TestCase("FTMS - c ESI d Full ms3 226.98@cid35.00 117.02@cid35.00 [50.00-365.00]                                                       ", "226.98, 117.02!", 3, "cid")]
        [TestCase("- p NSI Full ms2 218.110 [50.100-250.000]                                                                                    ", "218.110", 2, "")]
        [TestCase("FTMS + p NSI sa Full ms2 968.57@etd10.00 [200.00-2000.00]                                                                    ", "968.57", 2, "sa_etd")]
        [TestCase("ITMS + p NSI d Full ms2 430.89@etd150.00 [50.00-875.00]                                                                      ", "430.89", 2, "etd")]
        [TestCase("+ p NSI Full pr 150.000 [150.100-400.000]                                                                                    ", "0", 1, "")]
        [TestCase("+ p NSI Q1MS [150.100-400.000]                                                                                               ", "0", 1, "")]
        [TestCase("FTMS + p NSI Full ms2 1100.00@cid0.00 [300.00-2000.00]                                                                       ", "1100.00", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms2 808.41@hcd28.00 [100.00-2000.00]                                                                       ", "808.41", 2, "hcd")]
        [TestCase("ITMS + p NSI sid=35.00  Full ms [210.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("FTMS + p NSI Full msx ms2 636.04@hcd28.00 641.04@hcd28.00 654.05@hcd28.00 [88.00-1355.00]                                    ", "636.04, 641.04, 654.05", 2, "hcd")]
        [TestCase("FTMS + c NSI Full msx ms2 818.12@hcd27.00 863.14@hcd27.00 838.13@hcd27.00 638.04@hcd27.00 [88.33-1785.00]                    ", "818.12, 863.14, 838.13, 638.04", 2, "hcd")]
        [TestCase("FTMS + p NSI SIM ms [521.90-523.90]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 1303.24@cid35.00 [345.00-2000.00]                                                                    ", "1303.24", 2, "cid")]
        [TestCase("FTMS + p NSI Full ms2 881.00@etd200.00 [240.00-2000.00]                                                                      ", "881.00", 2, "etd")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@cid35.00 [270.00-2000.00]                                                            ", "994.00", 2, "cid")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@etd100.00 [400.00-2000.00]                                                           ", "994.00", 2, "etd")]
        [TestCase("FTMS + p NSI sid=35.00  Full ms2 994.00@hcd1.00 [500.00-2000.00]                                                             ", "994.00", 2, "hcd")]
        [TestCase("ITMS + c NSI sid=35.00  d Full ms2 445.12@cid35.00 [110.00-460.00]                                                           ", "445.12", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms2 612.78@cid35.00 [165.00-2000.00]                                                                       ", "612.78", 2, "cid")]
        [TestCase("FTMS + c NSI Full ms3 559.30@cid35.00 421.51@cid35.00 [115.00-2000.00]                                                       ", "559.30, 421.51!", 3, "cid")]
        [TestCase("FTMS {0,0}  - p NSI Full ms [250.00-2400.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI r d Full ms2 394.7961@cid30.00 [103.0000-800.0000]                                                              ", "394.7961", 2, "cid")]
        [TestCase("ITMS + c NSI r d Full ms2 626.4867@etd30.14 [120.0000-2000.0000]                                                             ", "626.4867", 2, "etd")]
        [TestCase("ITMS + c NSI r d sa Full ms2 437.5289@etd53.58@cid20.00 [120.0000-1323.0000]                                                 ", "437.5289", 2, "ETciD")]
        [TestCase("ITMS + c NSI r d sa Full ms2 437.5289@etd53.58@hcd20.00 [120.0000-1323.0000]                                                 ", "437.5289", 2, "EThcD")]
        [TestCase("- c NSI Q3MS [65.000-900.000]                                                                                                ", "0", 1, "")]
        [TestCase("ITMS + c NSI sid=5.00  Full ms [150.00-2000.00]                                                                              ", "0", 1, "")]
        [TestCase("ITMS - c NSI Full ms [75.00-1500.00]                                                                                         ", "0", 1, "")]
        [TestCase("ITMS + p NSI Z ms [150.00-2000.00]                                                                                           ", "0", 1, "")]
        [TestCase("+ c NSI Full ms2 856.930 [300.000-1200.000]                                                                                  ", "856.930", 2, "")]
        [TestCase("ITMS + c NSI d Full ms2 916.13@pqd28.50 [50.00-2000.00]                                                                      ", "916.13", 2, "pqd")]
        [TestCase("ITMS + c NSI d sa Full ms2 377.19@etd100.00 [50.00-1145.00]                                                                  ", "377.19", 2, "sa_etd")]
        [TestCase("+ c NSI Full cnl 162.053 [300.000-1200.000]                                                                                  ", "0", 1, "")]
        [TestCase("- c NSI Full pr 225.007 [300.000-1200.000]                                                                                   ", "0", 1, "")]
        [TestCase("- c NSI Q1MS [600.000-1000.000]                                                                                              ", "0", 1, "")]
        [TestCase("- c NSI d Full ms2 671.207 [10.000-676.207]                                                                                  ", "671.207", 2, "")]
        [TestCase("- p NSI Q3MS [807.100-810.100, 851.100-854.100]                                                                              ", "0", 1, "")]
        [TestCase("FTMS + p EI Full ms [50.00-600.00]                                                                                           ", "0", 1, "")]
        [TestCase("FTMS + p EI Full lock ms [50.00-600.00]                                                                                      ", "0", 1, "")]
        [TestCase("FTMS + p NSI sps d Full ms3 432.8866@hcd30.00 149.0201@hcd65.00 [110.0000-500.0000]                                          ", "432.8866, 149.0201!", 3, "hcd")]
        [TestCase("FTMS + c ESI d Full ms2 391.29@hcd30.00 [90.00-405.00]                                                                       ", "391.29", 2, "hcd")]
        [TestCase("ITMS + c ESI d Full ms2 391.29@cid35.00 [65.00-405.00]                                                                       ", "391.29", 2, "cid")]
        [TestCase("FTMS + p NSI d SIM ms [613.00-623.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS + c NSI d Full ms2 632.55@etd100.00 [100.00-2000.00]                                                                    ", "632.55", 2, "etd")]
        [TestCase("FTMS + p NSI d sa Full ms2 858.56@etd33.33 [100.00-2000.00]                                                                  ", "858.56", 2, "sa_etd")]
        [TestCase("FTMS + p NSI Full ms3 1395.00@cid40.00 769.00@cid26.00 [210.00-2000.00]                                                      ", "1395.00, 769.00!", 3, "cid")]
        [TestCase("FTMS + p NSI d sa Full ms2 686.2169@etd10.00@hcd10.00 [110.0000-2000.0000]                                                   ", "686.2169", 2, "EThcD")]
        [TestCase("FTMS + p NSI d sa Full ms2 686.2169@etd10.00@cid10.00 [110.0000-2000.0000]                                                   ", "686.2169", 2, "ETciD")]
        [TestCase("FTMS + c NSI d sa Full ms2 430.89@etd50.00 [100.00-1735.00]                                                                  ", "430.89", 2, "sa_etd")]
        [TestCase("ITMS + c NSI d Full ms2 327.01@etd500.00 [50.00-1320.00]                                                                     ", "327.01", 2, "etd")]
        [TestCase("FTMS - c ESI d Full ms2 255.23@hcd30.00 [90.00-525.00]                                                                       ", "255.23", 2, "hcd")]
        [TestCase("ITMS - c ESI d Full ms2 255.23@cid35.00 [50.00-525.00]                                                                       ", "255.23", 2, "cid")]
        [TestCase("FTMS {1,1}  - p ESI Full ms [150.00-1800.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS - p NSI Full ms2 988.50@cid0.00 [270.00-1000.00]                                                                        ", "988.50", 2, "cid")]
        [TestCase("FTMS {1,1}  - p NSI Full ms [150.00-1000.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI r d w Full ms2 255.27@cid35.00 [60.00-2000.00]                                                                  ", "255.27", 2, "cid")]
        [TestCase("ITMS - c ESI Full ms [65.00-800.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + c ESI Full ms [65.00-800.00]                                                                                          ", "0", 1, "")]
        [TestCase("+ c NSI Full ms [400.00-2000.00]                                                                                             ", "0", 1, "")]
        [TestCase("+ c NSI d Full ms2 550.74@cid35.00 [140.00-1115.00]                                                                          ", "550.74", 2, "cid")]
        [TestCase("FTMS + p ESI d Full ms2 112.89@cid35.00 [50.00-240.00]                                                                       ", "112.89", 2, "cid")]
        [TestCase("+ c NSI Q3MS [380.000-1350.000]                                                                                              ", "0", 1, "")]
        [TestCase("FTMS + c NSI d SIM ms [782.00-792.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS {0,0}  + p NSI Full ms [400.00-2000.00]                                                                                 ", "0", 1, "")]
        [TestCase("ITMS + c NSI d SIM ms [428.00-438.00]                                                                                        ", "0", 1, "")]
        [TestCase("FTMS {1,1}  + p NSI Full ms2 1000.00@hcd25.00 [300.00-2000.00]                                                               ", "1000.00", 2, "hcd")]
        [TestCase("FTMS {1,1}  + p NSI sid=35.00  Full ms [300.00-2000.00]                                                                      ", "0", 1, "")]
        [TestCase("FTMS + c NSI d sa Full ms2 738.2079@etd19.29@hcd20.00 [120.0000-2000.0000]                                                   ", "738.2079", 2, "EThcD")]
        [TestCase("FTMS + c NSI k d Full ms3 978.7619@hcd30.00 256.2410@hcd65.00 [110.0000-500.0000]                                            ", "978.7619, 256.2410!", 3, "hcd")]
        [TestCase("FTMS - p NSI d Full ms2 1082.08@hcd30.00 [148.67-2230.00]                                                                    ", "1082.08", 2, "hcd")]
        [TestCase("FTMS + c NSI sid=30.00  d Full ms2 453.79@cid35.00 [110.00-2000.00]                                                          ", "453.79", 2, "cid")]
        [TestCase("+ c EI SRM ms2 160.000 [72.999-73.001]                                                                                       ", "160.000", 2, "")]
        [TestCase("FTMS + p NSI d Full ms3 850.70@cid35.00 756.71@cid35.00 [195.00-2000.00]                                                     ", "850.70, 756.71!", 3, "cid")]
        [TestCase("FTMS + p NSI sid=35.00  d Full ms2 501.11@cid35.00 [125.00-2000.00]                                                          ", "501.11", 2, "cid")]
        [TestCase("+ c EI Q1MS [50.000-600.000]                                                                                                 ", "0", 1, "")]
        [TestCase("+ c CI Q1MS [50.000-600.000]                                                                                                 ", "0", 1, "")]
        [TestCase("FTMS - p ESI d Full ms2 87.01@hcd45.00 [50.00-185.00]                                                                        ", "87.01", 2, "hcd")]
        [TestCase("FTMS + p ESI d Full ms2 445.12@hcd60.00 [100.00-460.00]                                                                      ", "445.12", 2, "hcd")]
        [TestCase("ITMS + c NSI t d Full ms2 815.2148@cid35.00 [219.0000-1641.0000]                                                             ", "815.2148", 2, "cid")]
        [TestCase("FTMS + p NSI sps d Full ms3 815.2148@cid35.00 708.8861@hcd65.00 [120.0000-500.0000]                                          ", "815.2148, 708.8861!", 3, "hcd")] // ms2 is cid
        [TestCase("+ c NSI SIM pr 95.099 [105.099-105.101, 150.099-150.101, 370.199-370.201, 208.099-208.101, 300.199-300.201, 304.199-304.201] ", "0", 1, "")]
        [TestCase("+ c NSI Full pr 1000.000 [150.000-1500.000]                                                                                  ", "0", 1, "")]
        [TestCase("+ c NSI d Full ms2 150.100 [10.000-155.100]                                                                                  ", "150.100", 2, "")]
        [TestCase("+ c NSI Q1MS [150.000-1500.000]                                                                                              ", "0", 1, "")]
        [TestCase("ITMS + c NSI d Z ms [428.00-438.00]                                                                                          ", "0", 1, "")]
        [TestCase("FTMS + c NSI Full ms2 779.60@etd25.00 [210.00-2000.00]                                                                       ", "779.60", 2, "etd")]
        [TestCase("ITMS + c NSI r d Full ms3 617.4266@cid35.00 277.2909@cid35.00 [71.0000-842.0000]                                             ", "617.4266, 277.2909!", 3, "cid")]
        [TestCase("FTMS + c NSI d sa Full ms2 963.2524@etd120.55@cid10.00 [120.0000-1937.0000]                                                  ", "963.2524", 2, "ETciD")]
        [TestCase("+ p NSI Full ms2 713.950 [200.070-1500.000]                                                                                  ", "713.950", 2, "")]
        [TestCase("+ p NSI Q3MS [200.070-1300.000]                                                                                              ", "0", 1, "")]
        [TestCase("- p NSI Q3MS [150.000-1500.000]                                                                                              ", "0", 1, "")]
        [TestCase("FTMS + p NSI sid=15.00  d Full ms2 820.19@hcd25.00 [400.00-2000.00]                                                          ", "820.19", 2, "hcd")]
        [TestCase("ITMS - p NSI sid=15.00  Full ms [500.00-2000.00]                                                                             ", "0", 1, "")]
        [TestCase("+ c ESI Full ms [400.00-2000.00]                                                                                             ", "0", 1, "")]
        [TestCase("+ c d Full ms2 1013.32@cid45.00 [265.00-2000.00]                                                                             ", "1013.32", 2, "cid")]
        [TestCase("FTMS - p NSI SIM ms [330.00-380.00]                                                                                          ", "0", 1, "")]
        [TestCase("ITMS + p NSI Full ms2 1155.10@cid10.00 [315.00-2000.00]                                                                      ", "1155.10", 2, "cid")]
        public void ExtractParentIonMZOnlyFromFilterText(string filterText, string expectedParentIons, int expectedMSLevel, string expectedCollisionMode)
        {
            double parentIonMZ;
            var msLevel = 0;
            var collisionMode = string.Empty;

            var success = XRawFileIO.ExtractParentIonMZFromFilterText(filterText, out parentIonMZ);
            Console.WriteLine(filterText + " -- ms" + msLevel + ", " + parentIonMZ.ToString("0.00") + " " + collisionMode);

            var expectedParentIonList = expectedParentIons.Split(',');
            //var expectedParentIonMZ = double.Parse(expectedParentIonList.Last().Replace("!", ""));
            var expectedParentIonMZ = double.Parse(expectedParentIonList[0].Replace("!", ""));
            foreach (var parentIon in expectedParentIonList)
            {
                if (parentIon.Contains('!'))
                    expectedParentIonMZ = double.Parse(parentIon.Replace("!", ""));
            }
            Assert.AreEqual(expectedParentIonMZ, parentIonMZ, 0.001, "Parent ion m/z mismatch");
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
        [TestCase("FTMS + p NSI Full ms [400.00-2000.00]                                             ", true, 1, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]                          ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c EI SRM ms2 247.000 [300.000-1500.00]                                          ", true, 2, false, MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 965.958 [300.000-1500.00]                                         ", true, 2, false, MRMScanTypeConstants.SRM, false)]
        [TestCase("+ p NSI SRM ms2 1025.250 [300.000-1500.00]                                        ", true, 2, false, MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                ", true, 2, false, MRMScanTypeConstants.SRM, false)]
        [TestCase("+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]", true, 2, false, MRMScanTypeConstants.SRM, false)]
        [TestCase("+ p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                  ", true, 1, true, MRMScanTypeConstants.MRMQMS, false)]
        [TestCase("+ p NSI Q3MS [150.070-1500.000]                                                   ", true, 1, true, MRMScanTypeConstants.MRMQMS, false)]
        [TestCase("c NSI Full cnl 162.053 [300.000-1200.000]                                         ", true, 2, false, MRMScanTypeConstants.FullNL, false)]
        [TestCase("ITMS + c ESI Full ms [300.00-2000.00]                                             ", true, 1, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                    ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- p NSI Full ms2 168.070 [300.000-1500.00]                                        ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                       ", false, 3, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms10 421.76@35.00                                             ", false, 10, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]                       ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]                          ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]                          ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]                           ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]                           ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + p ESI d Z ms [1108.00-1118.00]                                             ", true, 1, false, MRMScanTypeConstants.NotMRM, true)]
        [TestCase("ITMS + p ESI d Z ms [579.00-589.00]                                               ", true, 1, false, MRMScanTypeConstants.NotMRM, true)]
        [TestCase("ITMS + p NSI Z ms [150.00-2000.00]                                                ", true, 1, false, MRMScanTypeConstants.NotMRM, true)]
        [TestCase("ITMS + p NSI d Z ms [351.00-361.00]                                               ", true, 1, false, MRMScanTypeConstants.NotMRM, true)]
        [TestCase("ITMS + c NSI d Z ms [428.00-438.00]                                               ", true, 1, false, MRMScanTypeConstants.NotMRM, true)]
        [TestCase("+ p ms2 777.00@cid30.00 [210.00-1200.00]                                          ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]     ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]    ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]    ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]                          ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]                       ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("+ c NSI Full ms2 1083.000 [300.000-1500.00]                                       ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- p NSI Full ms2 247.060 [300.000-1500.00]                                        ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("- c NSI d Full ms2 921.597 [300.000-1500.00]                                      ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d sa Full ms2 996.8542@etd120.55@cid20.00 [120.0000-2000.0000]     ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [242.00-248.00, 285.00-291.00]  ", false, 3, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI Full ms                                                              ", true, 1, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@cid30.00 [247.0000-2000.0000]                  ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("ITMS + c NSI r d Full ms2 916.3716@hcd30.00 [100.0000-2000.0000]                  ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI r d Full ms2 744.0129@cid30.00 [199.0000-2000.0000]                  ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + p NSI r d Full ms2 944.4316@hcd30.00 [100.0000-2000.0000]                  ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]    ", false, 2, false, MRMScanTypeConstants.NotMRM, false)]
        [TestCase("FTMS - p NSI w SIM ms [817.00-917.00]                                             ", true, 1, true, MRMScanTypeConstants.SIM, false)]
        [TestCase("FTMS + c NSI d SIM ms [782.00-792.00]                                             ", true, 1, true, MRMScanTypeConstants.SIM, false)]
        [TestCase("ITMS + c NSI SIM ms [286.50-289.50]                                               ", true, 1, true, MRMScanTypeConstants.SIM, false)]
        public void ValidateMSScan(
            string filterText, 
            bool expectedIsValidMS1orSIM, 
            int expectedMSLevel, 
            bool expectedIsSIMScan,
            MRMScanTypeConstants expectedMRMScanType, 
            bool expectedIsZoomScan)
        {
            int msLevel;
            bool isSIMScan;
            MRMScanTypeConstants mrmScanType;
            bool zoomScan;

            var isValid = XRawFileIO.ValidateMSScan(filterText, out msLevel, out isSIMScan, out mrmScanType, out zoomScan);

            Console.WriteLine(filterText + "  -- ms" + msLevel + "; SIM=" + isSIMScan + "; MRMScanType=" + mrmScanType);

            Assert.AreEqual(expectedIsValidMS1orSIM, isValid, "Mismatch for IsValidMS1orSIM");
            Assert.AreEqual(expectedMSLevel, msLevel, "MSLevel mismatch");
            Assert.AreEqual(expectedIsSIMScan, isSIMScan, "SIMScan mismatch");
            Assert.AreEqual(expectedMRMScanType, mrmScanType, "MRMScanType mismatch");
            Assert.AreEqual(expectedIsZoomScan, zoomScan, "ZoomScan mismatch");

        }
    }
}
