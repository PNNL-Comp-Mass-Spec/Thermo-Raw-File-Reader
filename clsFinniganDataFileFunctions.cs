using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MSFileReaderLib;

// These functions utilize MSFileReader.XRawfile2.dll to extract scan header info and
// raw mass spectrum info from Finnigan LCQ, LTQ, and LTQ-FT files
// 
// Required Dlls: fileio.dll, fregistry.dll, and MSFileReader.XRawfile2.dll
// DLLs obtained from: Thermo software named "MSFileReader2.2"
// Download link: http://sjsupport.thermofinnigan.com/public/detail.asp?id=703
//
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
//
// Switched from XRawFile2.dll to MSFileReader.XRawfile2.dll in March 2012 (that DLL comes with ProteoWizard)
//
// If having troubles reading files, install MS File Reader 3.0 SP3
// Download link: https://thermo.flexnetoperations.com/control/thmo/login

namespace ThermoRawFileReader
{

    public class XRawFileIO : FinniganFileReaderBaseClass, IDisposable
    {

        #region "Constants and Enums"

        // Note that each of these strings has a space at the end; this is important to avoid matching inappropriate text in the filter string
        private const string MS_ONLY_C_TEXT = " c ms ";
        private const string MS_ONLY_P_TEXT = " p ms ";

        private const string MS_ONLY_P_NSI_TEXT = " p NSI ms ";
        private const string MS_ONLY_PZ_TEXT = " p Z ms ";			// Likely a zoom scan			
        private const string MS_ONLY_DZ_TEXT = " d Z ms ";			// Dependent zoom scan			
        private const string MS_ONLY_PZ_MS2_TEXT = " d Z ms2 ";		// Dependent MS2 zoom scan			
        private const string MS_ONLY_Z_TEXT = " NSI Z ms ";			// Likely a zoom scan

        private const string FULL_MS_TEXT = "Full ms ";
        private const string FULL_PR_TEXT = "Full pr ";				// TSQ: Full Parent Scan, Product Mass
        private const string SIM_MS_TEXT = "SIM ms ";
        private const string FULL_LOCK_MS_TEXT = "Full lock ms ";	// Lock mass scan

        private const string MRM_Q1MS_TEXT = "Q1MS ";
        private const string MRM_Q3MS_TEXT = "Q3MS ";
        private const string MRM_SRM_TEXT = "SRM ms2";
        private const string MRM_FullNL_TEXT = "Full cnl ";			// MRM neutral loss			
        private const string MRM_SIM_PR_TEXT = "SIM pr ";			// TSQ: Isolated and fragmented parent, monitor multiple product ion ranges; e.g., Biofilm-1000pg-std-mix_06Dec14_Smeagol-3

        // This RegEx matches Full ms2, Full ms3, ..., Full ms10, Full ms11, ...
        // It also matches p ms2
        // It also matches SRM ms2
        // It also matches CRM ms3
        // It also matches Full msx ms2 (multiplexed parent ion selection, introduced with the Q-Exactive)
        private const string MS2_REGEX = "(?<ScanMode> p|Full|SRM|CRM|Full msx) ms(?<MSLevel>[2-9]|[1-9][0-9]) ";

        private const string IONMODE_REGEX = "[+-]";

        private const string MASSLIST_REGEX = "\\[[0-9.]+-[0-9.]+.*\\]";

        private const string MASSRANGES_REGEX = "(?<StartMass>[0-9.]+)-(?<EndMass>[0-9.]+)";

        // This RegEx matches text like 1312.95@45.00 or 756.98@cid35.00 or 902.5721@etd120.55@cid20.00
        private const string PARENTION_REGEX = "(?<ParentMZ>[0-9.]+)@(?<CollisionMode1>[a-z]*)(?<CollisionEnergy1>[0-9.]+)(@(?<CollisionMode2>[a-z]+)(?<CollisionEnergy2>[0-9.]+))?";

        // This RegEx looks for "sa" prior to Full ms"
        private const string SA_REGEX = " sa Full ms";
        private const string MSX_REGEX = " Full msx ";

        private const string COLLISION_SPEC_REGEX = "(?<MzValue> [0-9.]+)@";

        private const string MZ_WITHOUT_COLLISION_ENERGY = "ms[2-9](?<MzValue> [0-9.]+)$";

        // Used with .GetSeqRowSampleType()
        public enum SampleTypeConstants
        {
            Unknown = 0,
            Blank = 1,
            QC = 2,
            StandardClear_None = 3,
            StandardUpdate_None = 4,
            StandardBracket_Open = 5,
            StandardBracketStart_MultipleBrackets = 6,
            StandardBracketEnd_multipleBrackets = 7
        }

        // Used with .SetController()
        public enum ControllerTypeConstants
        {
            NoDevice = -1,
            MS = 0,
            Analog = 1,
            AD_Card = 2,
            PDA = 3,
            UV = 4
        }

        // Used with .GetMassListXYZ()
        public enum IntensityCutoffTypeConstants
        {
            None = 0,
            // AllValuesReturned
            AbsoluteIntensityUnits = 1,
            RelativeToBasePeak = 2
        }

        public class InstFlags
        {
            public const string TIM = "Total Ion Map";
            public const string NLM = "Neutral Loss Map";
            public const string PIM = "Parent Ion Map";
            public const string DDZMap = "Data Dependent ZoomScan Map";
        }

        #endregion

        #region "Structures"
        public struct udtParentIonInfoType
        {
            public int MSLevel;
            public double ParentIonMZ;
            public string CollisionMode;
            public string CollisionMode2;
            public float CollisionEnergy;
            public float CollisionEnergy2;
            public ActivationTypeConstants ActivationType;
            public void Clear()
            {
                MSLevel = 1;
                ParentIonMZ = 0;
                CollisionMode = string.Empty;
                CollisionMode2 = string.Empty;
                CollisionEnergy = 0;
                CollisionEnergy2 = 0;
                ActivationType = ActivationTypeConstants.Unknown;
            }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(CollisionMode))
                {
                    return "ms" + MSLevel + " " + ParentIonMZ.ToString("0.0#");
                }
                else
                {
                    return "ms" + MSLevel + " " + ParentIonMZ.ToString("0.0#") + "@" + CollisionMode + CollisionEnergy.ToString("0.00");
                }
            }

        }

        public struct udtMassPrecisionInfoType
        {
            public double Intensity;
            public double Mass;
            public double AccuracyMMU;
            public double AccuracyPPM;
            public double Resolution;
        }

        public struct udtFTLabelInfoType
        {
            public double Mass;
            public double Intensity;
            public float Resolution;
            public float Baseline;
            public float Noise;
            public int Charge;
        }
        #endregion

        #region "Classwide Variables"

        // Cached XRawFile object, for faster accessing
        private IXRawfile5 mXRawFile;

        private bool mCorruptMemoryEncountered;

        private static readonly Regex mFindMS = new Regex(MS2_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mIonMode = new Regex(IONMODE_REGEX, RegexOptions.Compiled);

        private static readonly Regex mMassList = new Regex(MASSLIST_REGEX, RegexOptions.Compiled);

        private static readonly Regex mMassRanges = new Regex(MASSRANGES_REGEX, RegexOptions.Compiled);
        private static readonly Regex mFindParentIon = new Regex(PARENTION_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex mFindSAFullMS = new Regex(SA_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mFindFullMSx = new Regex(MSX_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mCollisionSpecs = new Regex(COLLISION_SPEC_REGEX, RegexOptions.Compiled);

        private static readonly Regex mMzWithoutCE = new Regex(MZ_WITHOUT_COLLISION_ENERGY, RegexOptions.Compiled);
        #endregion


        private void CacheScanInfo(int scan, clsScanInfo scanInfo)
        {
            if (mCachedScanInfo.Count > MAX_SCANS_TO_CACHE_INFO)
            {
                // Remove the oldest entry in mCachedScanInfo
                var minimumScanNumber = -1;
                var dtMinimumCacheDate = DateTime.UtcNow;

                foreach (var cachedInfo in mCachedScanInfo.Values)
                {
                    if (minimumScanNumber < 0 || cachedInfo.CacheDateUTC < dtMinimumCacheDate)
                    {
                        minimumScanNumber = cachedInfo.ScanNumber;
                        dtMinimumCacheDate = cachedInfo.CacheDateUTC;
                    }
                }

                if (mCachedScanInfo.ContainsKey(minimumScanNumber))
                {
                    mCachedScanInfo.Remove(minimumScanNumber);
                }
            }

            if (mCachedScanInfo.ContainsKey(scan))
            {
                mCachedScanInfo.Remove(scan);
            }

            mCachedScanInfo.Add(scan, scanInfo);

        }

        private static string CapitalizeCollisionMode(string strCollisionMode)
        {

            if ((string.Equals(strCollisionMode, "EThcD", StringComparison.InvariantCultureIgnoreCase)))
            {
                return "EThcD";
            }

            if ((string.Equals(strCollisionMode, "ETciD", StringComparison.InvariantCultureIgnoreCase)))
            {
                return "ETciD";
            }

            return strCollisionMode.ToUpper();

        }

        static T[,] Cast2D<T>(object[,] input)
        {
            var rows = input.GetLength(0);
            var columns = input.GetLength(1);
            var ret = new T[rows, columns];
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    ret[i, j] = (T)input[i, j];
                }
            }
            return ret;
        }

        public override bool CheckFunctionality()
        {
            // I have a feeling this doesn't actually work, and will always return True
            try
            {
                // ReSharper disable once UnusedVariable
                var objXRawFile = new MSFileReader_XRawfile();

                // If we get here, all is fine
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]

        public override void CloseRawFile()
        {
            try
            {
                if ((mXRawFile != null))
                {
                    mXRawFile.Close();
                }
                mCorruptMemoryEncountered = false;

            }
            catch (AccessViolationException)
            {
                // Ignore this error
            }
            catch (Exception)
            {
                // Ignore any errors
            }
            finally
            {
                mXRawFile = null;
                mCachedFileName = string.Empty;
            }

        }

        private static bool ContainsAny(string stringToSearch, IEnumerable<string> itemsToFind, int indexSearchStart = 0)
        {

            return itemsToFind.Any(item => ContainsText(stringToSearch, item, indexSearchStart));

        }

        private static bool ContainsText(string stringToSearch, string textToFind, int indexSearchStart = 0)
        {

            // Note: need to append a space since many of the search keywords end in a space
            if ((stringToSearch + " ").IndexOf(textToFind, StringComparison.InvariantCultureIgnoreCase) >= indexSearchStart)
            {
                return true;
            }

            return false;

        }

        public static MRMScanTypeConstants DetermineMRMScanType(string strFilterText)
        {
            var eMRMScanType = MRMScanTypeConstants.NotMRM;

            if (string.IsNullOrWhiteSpace(strFilterText))
            {
                return eMRMScanType;
            }

            var mrmQMSTags = new List<string> {
				MRM_Q1MS_TEXT,
				MRM_Q3MS_TEXT
			};

            if (ContainsAny(strFilterText, mrmQMSTags, 1))
            {
                eMRMScanType = MRMScanTypeConstants.MRMQMS;
            }
            else if (ContainsText(strFilterText, MRM_SRM_TEXT, 1))
            {
                eMRMScanType = MRMScanTypeConstants.SRM;
            }
            else if (ContainsText(strFilterText, MRM_SIM_PR_TEXT, 1))
            {
                // This is not technically SRM, but the data looks very similar, so we'll track it like SRM data
                eMRMScanType = MRMScanTypeConstants.SRM;
            }
            else if (ContainsText(strFilterText, MRM_FullNL_TEXT, 1))
            {
                eMRMScanType = MRMScanTypeConstants.FullNL;

            }

            return eMRMScanType;
        }

        public static IonModeConstants DetermineIonizationMode(string strFiltertext)
        {

            // Determine the ion mode by simply looking for the first + or - sign
            var eIonMode = IonModeConstants.Unknown;


            if (!string.IsNullOrWhiteSpace(strFiltertext))
            {
                // For safety, remove any text after a square bracket
                var intCharIndex = strFiltertext.IndexOf('[');

                Match reMatch;
                if (intCharIndex > 0)
                {
                    reMatch = mIonMode.Match(strFiltertext.Substring(0, intCharIndex));
                }
                else
                {
                    reMatch = mIonMode.Match(strFiltertext);
                }

                if (reMatch.Success)
                {
                    switch (reMatch.Value)
                    {
                        case "+":
                            eIonMode = IonModeConstants.Positive;
                            break;
                        case "-":
                            eIonMode = IonModeConstants.Negative;
                            break;
                        default:
                            eIonMode = IonModeConstants.Unknown;
                            break;
                    }
                }

            }

            return eIonMode;

        }

        public static void ExtractMRMMasses(string strFilterText, MRMScanTypeConstants eMRMScanType, out udtMRMInfoType udtMRMInfo)
        {
            // Parse out the MRM_QMS or SRM mass info from strFilterText
            // It should be of the form 
            // MRM_Q1MS_TEXT:    p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]
            // or
            // MRM_Q3MS_TEXT:    p NSI Q3MS [150.070-1500.000]
            // or
            // MRM_SRM_TEXT:    c NSI SRM ms2 489.270@cid17.00 [397.209-392.211, 579.289-579.291]

            // Note: we do not parse mass information out for Full Neutral Loss scans
            // MRM_FullNL_TEXT: c NSI Full cnl 162.053 [300.000-1200.000]

            udtMRMInfo = InitializeMRMInfo();

            if (string.IsNullOrWhiteSpace(strFilterText))
            {
                return;
            }

            if (eMRMScanType == MRMScanTypeConstants.MRMQMS | eMRMScanType == MRMScanTypeConstants.SRM)
            {
                // Parse out the text between the square brackets
                var reMatch = mMassList.Match(strFilterText);

                if (!reMatch.Success)
                {
                    return;
                }

                reMatch = mMassRanges.Match(reMatch.Value);

                udtMRMInfo = InitializeMRMInfo();

                while (reMatch.Success)
                {
                    try
                    {
                        // Note that group 0 is the full mass range (two mass values, separated by a dash)
                        // Group 1 is the first mass value
                        // Group 2 is the second mass value

                        var mrmMassRange = new udtMRMMassRangeType
                        {
                            StartMass = double.Parse(reMatch.Groups["StartMass"].Value),
                            EndMass = double.Parse(reMatch.Groups["EndMass"].Value)
                        };

                        var centralMass = mrmMassRange.StartMass + (mrmMassRange.EndMass - mrmMassRange.StartMass) / 2;
                        mrmMassRange.CentralMass = Math.Round(centralMass, 6);

                        udtMRMInfo.MRMMassList.Add(mrmMassRange);

                    }
                    catch (Exception)
                    {
                        // Error parsing out the mass values; skip this group
                    }

                    reMatch = reMatch.NextMatch();
                }
            }
            else
            {
                // Unsupported MRM type
            }
        }

        /// <summary>
        /// Parse out the parent ion and collision energy from strFilterText
        /// </summary>
        /// <param name="strFilterText"></param>
        /// <param name="dblParentIonMZ">Parent ion m/z (output)</param>
        /// <param name="intMSLevel">MSLevel (output)</param>
        /// <param name="strCollisionMode">Collision mode (output)</param>
        /// <returns>True if success</returns>
        /// <remarks>If multiple parent ion m/z values are listed then dblParentIonMZ will have the last one.  However, if the filter text contains "Full msx" then dblParentIonMZ will have the first parent ion listed</remarks>
        public static bool ExtractParentIonMZFromFilterText(string strFilterText, out double dblParentIonMZ, out int intMSLevel, out string strCollisionMode)
        {

            List<udtParentIonInfoType> lstParentIons;

            return ExtractParentIonMZFromFilterText(strFilterText, out dblParentIonMZ, out intMSLevel, out strCollisionMode, out lstParentIons);

        }

        /// <summary>
        /// Parse out the parent ion and collision energy from strFilterText
        /// </summary>
        /// <param name="strFilterText"></param>
        /// <param name="dblParentIonMZ">Parent ion m/z (output)</param>
        /// <param name="intMSLevel">MSLevel (output)</param>
        /// <param name="strCollisionMode">Collision mode (output)</param>
        /// <param name="lstParentIons">Output: parent ion list</param>
        /// <returns>True if success</returns>
        /// <remarks>If multiple parent ion m/z values are listed then dblParentIonMZ will have the last one.  However, if the filter text contains "Full msx" then dblParentIonMZ will have the first parent ion listed</remarks>
        public static bool ExtractParentIonMZFromFilterText(
            string strFilterText, 
            out double dblParentIonMZ, 
            out int intMSLevel, 
            out string strCollisionMode, 
            out List<udtParentIonInfoType> lstParentIons)
        {

            // strFilterText should be of the form "+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]"
            // or "+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
            // or "ITMS + c NSI d Full ms10 421.76@35.00"
            // or "ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]"              ' Note: sa stands for "supplemental activation"
            // or "ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]" 
            // or "ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]"
            // or "ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]"
            // or "ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]"
            // or "FTMS + p NSI Full ms [400.00-2000.00]"  (high res full MS)
            // or "ITMS + c ESI Full ms [300.00-2000.00]"  (low res full MS)
            // or "ITMS + p ESI d Z ms [1108.00-1118.00]"  (zoom scan)
            // or "+ p ms2 777.00@cid30.00 [210.00-1200.00]
            // or "+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]
            // or "FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]"
            // or "ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]"
            // or "+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"

            var udtBestParentIon = new udtParentIonInfoType();
            udtBestParentIon.Clear();

            intMSLevel = 1;
            dblParentIonMZ = 0;
            strCollisionMode = string.Empty;
            var blnMatchFound = false;

            lstParentIons = new List<udtParentIonInfoType>();

            try
            {
                var blnSupplementalActivationEnabled = mFindSAFullMS.IsMatch(strFilterText);

                var blnMultiplexedMSnEnabled = mFindFullMSx.IsMatch(strFilterText);

                string strMZText;
                var blnSuccess = ExtractMSLevel(strFilterText, out intMSLevel, out strMZText);

                if (!blnSuccess)
                {
                    return false;
                }

                // Use a RegEx to extract out the last parent ion mass listed
                // For example, grab 1312.95 out of "1312.95@45.00 [ 350.00-2000.00]"
                // or, grab 873.85 out of "1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
                // or, grab 756.98 out of "756.98@etd100.00 [50.00-2000.00]"
                // or, grab 748.371 out of "748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"
                //
                // However, if using multiplex ms/ms (msx) then we return the first parent ion listed

                // For safety, remove any text after a square bracket
                var intCharIndex = strMZText.IndexOf('[');
                if (intCharIndex > 0)
                {
                    strMZText = strMZText.Substring(0, intCharIndex);
                }

                // Find all of the parent ion m/z's present in strMZText
                var intStartIndex = 0;
                do
                {
                    var reMatchParentIon = mFindParentIon.Match(strMZText, intStartIndex);

                    if (!reMatchParentIon.Success)
                    {
                        // Match not found
                        // If strMZText only contains a number, we will parse it out later in this function
                        break;
                    }

                    // Match found

                    dblParentIonMZ = double.Parse(reMatchParentIon.Groups["ParentMZ"].Value);
                    strCollisionMode = string.Empty;
                    float sngCollisionEngergy = 0;

                    blnMatchFound = true;

                    intStartIndex = reMatchParentIon.Index + reMatchParentIon.Length;

                    strCollisionMode = GetCapturedValue(reMatchParentIon, "CollisionMode1");

                    var strCollisionEnergy = GetCapturedValue(reMatchParentIon, "CollisionEnergy1");
                    if (!string.IsNullOrEmpty(strCollisionEnergy))
                    {
                        float.TryParse(strCollisionEnergy, out sngCollisionEngergy);
                    }

                    float sngCollisionEngergy2 = 0;
                    var strCollisionMode2 = GetCapturedValue(reMatchParentIon, "CollisionMode2");

                    if (!string.IsNullOrEmpty(strCollisionMode2))
                    {
                        var strCollisionEnergy2 = GetCapturedValue(reMatchParentIon, "CollisionEnergy2");
                        float.TryParse(strCollisionEnergy2, out sngCollisionEngergy2);
                    }

                    var allowSecondaryActivation = true;
                    if (string.Equals(strCollisionMode, "ETD", StringComparison.InvariantCultureIgnoreCase) & !string.IsNullOrEmpty(strCollisionMode2))
                    {
                        if (string.Equals(strCollisionMode2, "CID", StringComparison.InvariantCultureIgnoreCase))
                        {
                            strCollisionMode = "ETciD";
                            allowSecondaryActivation = false;
                        }
                        else if (string.Equals(strCollisionMode2, "HCD", StringComparison.InvariantCultureIgnoreCase))
                        {
                            strCollisionMode = "EThcD";
                            allowSecondaryActivation = false;
                        }
                    }

                    if (allowSecondaryActivation && !string.IsNullOrEmpty(strCollisionMode))
                    {
                        if (blnSupplementalActivationEnabled)
                        {
                            strCollisionMode = "sa_" + strCollisionMode;
                        }
                    }

                    var udtParentIonInfo = new udtParentIonInfoType
                    {
                        MSLevel = intMSLevel,
                        ParentIonMZ = dblParentIonMZ,
                        CollisionEnergy = sngCollisionEngergy,
                        CollisionEnergy2 = sngCollisionEngergy2
                    };

                    if (strCollisionMode != null)
                        udtParentIonInfo.CollisionMode = string.Copy(strCollisionMode);

                    if (strCollisionMode2 != null)
                        udtParentIonInfo.CollisionMode2 = string.Copy(strCollisionMode2);

                    lstParentIons.Add(udtParentIonInfo);

                    if (!blnMultiplexedMSnEnabled || (lstParentIons.Count == 1))
                    {
                        udtBestParentIon = udtParentIonInfo;
                    }

                } while (intStartIndex < strMZText.Length - 1);

                if (blnMatchFound)
                {
                    // Update the output values using udtBestParentIon
                    intMSLevel = udtBestParentIon.MSLevel;
                    dblParentIonMZ = udtBestParentIon.ParentIonMZ;
                    strCollisionMode = udtBestParentIon.CollisionMode;

                    return true;
                }

                // Match not found using RegEx
                // Use manual text parsing instead

                intCharIndex = strMZText.LastIndexOf('@');
                if (intCharIndex > 0)
                {
                    strMZText = strMZText.Substring(0, intCharIndex);
                    intCharIndex = strMZText.LastIndexOf(' ');
                    if (intCharIndex > 0)
                    {
                        strMZText = strMZText.Substring(intCharIndex + 1);
                    }

                    try
                    {
                        dblParentIonMZ = double.Parse(strMZText);
                        blnMatchFound = true;
                    }
                    catch (Exception)
                    {
                        dblParentIonMZ = 0;
                    }

                }
                else if (strMZText.Length > 0)
                {
                    // Find the longest contiguous number that strMZText starts with

                    intCharIndex = -1;
                    while (intCharIndex < strMZText.Length - 1)
                    {
                        if (char.IsNumber(strMZText[intCharIndex + 1]) || strMZText[intCharIndex + 1] == '.')
                        {
                            intCharIndex += 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (intCharIndex >= 0)
                    {
                        try
                        {
                            dblParentIonMZ = double.Parse(strMZText.Substring(0, intCharIndex + 1));
                            blnMatchFound = true;

                            var udtParentIonMzOnly = new udtParentIonInfoType();
                            udtParentIonMzOnly.Clear();
                            udtParentIonMzOnly.MSLevel = intMSLevel;
                            udtParentIonMzOnly.ParentIonMZ = dblParentIonMZ;

                            lstParentIons.Add(udtParentIonMzOnly);

                        }
                        catch (Exception)
                        {
                            dblParentIonMZ = 0;
                        }
                    }
                }

            }
            catch (Exception)
            {
                blnMatchFound = false;
            }

            return blnMatchFound;

        }

        public static bool ExtractMSLevel(string strFilterText, out int intMSLevel, out string strMZText)
        {
            // Looks for "Full ms2" or "Full ms3" or " p ms2" or "SRM ms2" in strFilterText
            // Returns True if found and False if no match

            // Populates intMSLevel with the number after "ms" and strMZText with the text after "ms2"

            var intMatchTextLength = 0;

            intMSLevel = 1;
            var intCharIndex = 0;

            var reMatchMS = mFindMS.Match(strFilterText);

            if (reMatchMS.Success)
            {
                intMSLevel = Convert.ToInt32(reMatchMS.Groups["MSLevel"].Value);
                intCharIndex = strFilterText.IndexOf(reMatchMS.ToString(), StringComparison.InvariantCultureIgnoreCase);
                intMatchTextLength = reMatchMS.Length;
            }

            if (intCharIndex > 0)
            {
                // Copy the text after "Full ms2" or "Full ms3" in strFilterText to strMZText
                strMZText = strFilterText.Substring(intCharIndex + intMatchTextLength).Trim();
                return true;
            }

            strMZText = string.Empty;
            return false;
        }

        protected override bool FillFileInfo()
        {
            // Populates the mFileInfo structure
            // Function returns True if no error, False if an error

            var intResult = 0;

            var intMethodCount = 0;

            try
            {
                if (mXRawFile == null)
                    return false;

                // Make sure the MS controller is selected
                if (!SetMSController())
                    return false;

                mFileInfo.Clear();

                mFileInfo.CreationDate = DateTime.MinValue;
                mXRawFile.GetCreationDate(ref mFileInfo.CreationDate);
                mXRawFile.IsError(ref intResult);

                // Unfortunately, .IsError() always returns 0, even if an error occurred
                if (intResult != 0)
                    return false;

                mFileInfo.CreatorID = null;
                mXRawFile.GetCreatorID(ref mFileInfo.CreatorID);

                mFileInfo.InstFlags = null;
                mXRawFile.GetInstFlags(ref mFileInfo.InstFlags);

                mFileInfo.InstHardwareVersion = null;
                mXRawFile.GetInstHardwareVersion(ref mFileInfo.InstHardwareVersion);

                mFileInfo.InstSoftwareVersion = null;
                mXRawFile.GetInstSoftwareVersion(ref mFileInfo.InstSoftwareVersion);

                mFileInfo.InstMethods.Clear();

                if (mLoadMSMethodInfo)
                {

                    mXRawFile.GetNumInstMethods(ref intMethodCount);

                    for (var intIndex = 0; intIndex < intMethodCount; intIndex++)
                    {
                        string strMethod = null;
                        mXRawFile.GetInstMethod(intIndex, ref strMethod);
                        if (!string.IsNullOrWhiteSpace(strMethod))
                        {
                            mFileInfo.InstMethods.Add(strMethod);
                        }

                    }
                }

                mFileInfo.InstModel = null;
                mFileInfo.InstName = null;
                mFileInfo.InstrumentDescription = null;
                mFileInfo.InstSerialNumber = null;

                mXRawFile.GetInstModel(ref mFileInfo.InstModel);
                mXRawFile.GetInstName(ref mFileInfo.InstName);
                mXRawFile.GetInstrumentDescription(ref mFileInfo.InstrumentDescription);
                mXRawFile.GetInstSerialNumber(ref mFileInfo.InstSerialNumber);

                mXRawFile.GetVersionNumber(ref mFileInfo.VersionNumber);
                mXRawFile.GetMassResolution(ref mFileInfo.MassResolution);

                mXRawFile.GetFirstSpectrumNumber(ref mFileInfo.ScanStart);
                mXRawFile.GetLastSpectrumNumber(ref mFileInfo.ScanEnd);

                mFileInfo.AcquisitionDate = null;
                mFileInfo.AcquisitionFilename = null;
                mFileInfo.Comment1 = null;
                mFileInfo.Comment2 = null;
                mFileInfo.SampleName = null;
                mFileInfo.SampleComment = null;

                // Note that the following are typically blank
                mXRawFile.GetAcquisitionDate(ref mFileInfo.AcquisitionDate);
                mXRawFile.GetAcquisitionFileName(ref mFileInfo.AcquisitionFilename);
                mXRawFile.GetComment1(ref mFileInfo.Comment1);
                mXRawFile.GetComment2(ref mFileInfo.Comment2);
                mXRawFile.GetSeqRowSampleName(ref mFileInfo.SampleName);
                mXRawFile.GetSeqRowComment(ref mFileInfo.SampleComment);

                mFileInfo.TuneMethods = new List<udtTuneMethodType>();

                if (mLoadMSTuneInfo)
                {
                    GetTuneData();
                }

            }
            catch (Exception ex)
            {
                var strError = "Error: Exception in FillFileInfo: " + ex.Message;
                RaiseErrorMessage(strError);
                return false;
            }

            return true;

        }

        private ActivationTypeConstants GetActivationType(int scan, int msLevel)
        {


            try
            {
                var activationTypeCode = 0;

                mXRawFile.GetActivationTypeForScanNum(scan, msLevel, ref activationTypeCode);

                ActivationTypeConstants activationType;

                if (!Enum.TryParse(activationTypeCode.ToString(), out activationType))
                {
                    activationType = ActivationTypeConstants.Unknown;
                }

                return activationType;

            }
            catch (Exception ex)
            {
                var strError = "Error: Exception in GetActivationType: " + ex.Message;
                RaiseWarningMessage(strError);
                return ActivationTypeConstants.Unknown;
            }

        }

        private static string GetCapturedValue(Match reMatch, string captureGroupName)
        {
            var capturedValue = reMatch.Groups[captureGroupName];

            if ((capturedValue != null))
            {
                if (!string.IsNullOrEmpty(capturedValue.Value))
                {
                    return capturedValue.Value;
                }
            }

            return string.Empty;

        }

        public List<double> GetCollisionEnergy(int scan)
        {

            var intNumMSOrders = 0;
            var lstCollisionEnergies = new List<double>();

            try
            {
                if (mXRawFile == null)
                    return lstCollisionEnergies;

                mXRawFile.GetNumberOfMSOrdersFromScanNum(scan, ref intNumMSOrders);

                for (var intMSOrder = 1; intMSOrder <= intNumMSOrders; intMSOrder++)
                {
                    double dblCollisionEnergy = 0;
                    mXRawFile.GetCollisionEnergyForScanNum(scan, intMSOrder, ref dblCollisionEnergy);

                    if ((dblCollisionEnergy > 0))
                    {
                        lstCollisionEnergies.Add(dblCollisionEnergy);
                    }
                }

            }
            catch (Exception ex)
            {
                var strError = "Error: Exception in GetCollisionEnergy: " + ex.Message;
                RaiseErrorMessage(strError);
            }

            return lstCollisionEnergies;

        }

        public override int GetNumScans()
        {
            // Returns the number of scans, or -1 if an error

            var intResult = 0;
            var intScanCount = 0;

            try
            {
                if (mXRawFile == null)
                    return -1;

                mXRawFile.GetNumSpectra(ref intScanCount);
                mXRawFile.IsError(ref intResult);
                // Unfortunately, .IsError() always returns 0, even if an error occurred
                if (intResult == 0)
                {
                    return intScanCount;
                }
                
                return -1;
            }
            catch (Exception)
            {
                return -1;
            }

        }

        /// <summary>
        /// Get the header info for the specified scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="udtScanInfo">Scan header info struct</param>
        /// <returns>True if no error, False if an error</returns>
        /// <remarks></remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public override bool GetScanInfo(int scan, out udtScanHeaderInfoType udtScanInfo)
        {

            clsScanInfo scanInfo;
            var success = GetScanInfo(scan, out scanInfo);

            if (success)
            {
                udtScanInfo = ScanInfoClassToStruct(scanInfo);
            }
            else
            {
                udtScanInfo = new udtScanHeaderInfoType();
            }

            return success;
        }

        /// <summary>
        /// Get the header info for the specified scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="scanInfo">Scan header info class</param>
        /// <returns>True if no error, False if an error</returns>
        /// <remarks></remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public override bool GetScanInfo(int scan, out clsScanInfo scanInfo)
        {

            // Check for the scan in the cache
            if (mCachedScanInfo.TryGetValue(scan, out scanInfo))
            {
                return true;
            }

            if (scan < mFileInfo.ScanStart)
            {
                scan = mFileInfo.ScanStart;
            }
            else if (scan > mFileInfo.ScanEnd)
            {
                scan = mFileInfo.ScanEnd;
            }

            scanInfo = new clsScanInfo(scan);


            try
            {
                if (mXRawFile == null)
                    return false;

                // Make sure the MS controller is selected
                if (!SetMSController())
                    return false;

                // Initialize the values that will be populated using GetScanHeaderInfoForScanNum()
                scanInfo.NumPeaks = 0;
                scanInfo.TotalIonCurrent = 0;
                scanInfo.SIMScan = false;
                scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM;
                scanInfo.ZoomScan = false;
                scanInfo.CollisionMode = string.Empty;
                scanInfo.FilterText = string.Empty;
                scanInfo.IonMode = IonModeConstants.Unknown;

                var numPeaks = 0;
                double retentionTime = 0;
                double lowMass = 0;
                double highMass = 0;
                double totalIonCurrent = 0;
                double basePeakMZ = 0;
                double basePeakIntensity = 0;
                var numChannels = 0;
                double frequency = 0;

                var intBooleanVal = 0;
                var intResult = 0;

                mXRawFile.GetScanHeaderInfoForScanNum(
                    scan, ref numPeaks, ref retentionTime, ref lowMass, ref highMass,
                    ref totalIonCurrent, ref basePeakMZ, ref basePeakIntensity, ref numChannels, intBooleanVal, ref frequency);

                scanInfo.NumPeaks = numPeaks;
                scanInfo.RetentionTime = retentionTime;
                scanInfo.LowMass = lowMass;
                scanInfo.HighMass = highMass;
                scanInfo.TotalIonCurrent = totalIonCurrent;
                scanInfo.BasePeakMZ = basePeakMZ;
                scanInfo.BasePeakIntensity = basePeakIntensity;
                scanInfo.NumChannels = numChannels;
                scanInfo.Frequency = frequency;

                mXRawFile.IsError(ref intResult);
                // Unfortunately, .IsError() always returns 0, even if an error occurred

                if (intResult != 0)
                {
                    CacheScanInfo(scan, scanInfo);
                    return false;
                }

                scanInfo.UniformTime = Convert.ToBoolean(intBooleanVal);

                intBooleanVal = 0;
                mXRawFile.IsCentroidScanForScanNum(scan, intBooleanVal);

                scanInfo.IsCentroided = Convert.ToBoolean(intBooleanVal);

                var intArrayCount = 0;
                object objLabels = null;
                object objValues = null;

                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        // Retrieve the additional parameters for this scan (including Scan Event)
                        mXRawFile.GetTrailerExtraForScanNum(scan, ref objLabels, ref objValues, ref intArrayCount);
                    }
                }
                catch (AccessViolationException ex)
                {
                    var strWarningMessage = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(strWarningMessage);
                    intArrayCount = 0;

                }
                catch (Exception ex)
                {
                    var strWarningMessage = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(strWarningMessage);
                    intArrayCount = 0;

                    if (ex.Message.ToLower().Contains("memory is corrupt"))
                    {
                        mCorruptMemoryEncountered = true;
                    }
                }

                scanInfo.EventNumber = 1;
                if (intArrayCount > 0 && objLabels != null && objValues != null)
                {

                    var scanEventNames =
                        ((IEnumerable)objLabels).Cast<object>()
                            .Select(x => x.ToString())
                            .ToArray();

                    var scanEventValues =
                         ((IEnumerable)objValues).Cast<object>()
                            .Select(x => x.ToString())
                            .ToArray();

                    scanInfo.StoreScanEvents(scanEventNames, scanEventValues);

                    // Look for the entry in strLabels named "Scan Event:"
                    // Entries for the LCQ are:
                    //   Wideband Activation
                    //   Micro Scan Count
                    //   Ion Injection Time (ms)
                    //   Scan Segment
                    //   Scan Event
                    //   Elapsed Scan Time (sec)
                    //   API Source CID Energy
                    //   Resolution
                    //   Average Scan by Inst
                    //   BackGd Subtracted by Inst
                    //   Charge State

                    foreach (var scanEvent in from item in scanInfo.ScanEvents where item.Key.ToLower().StartsWith("scan event") select item)
                    {
                        try
                        {
                            scanInfo.EventNumber = Convert.ToInt32(scanEvent.Value);
                        }
                        catch (Exception)
                        {
                            // Ignore errors here
                        }
                        break;
                    }

                }

                // Lookup the filter text for this scan
                // Parse out the parent ion m/z for fragmentation scans
                // Must set strFilterText to Nothing prior to calling .GetFilterForScanNum()
                string strFilterText = null;
                mXRawFile.GetFilterForScanNum(scan, ref strFilterText);

                scanInfo.FilterText = string.Copy(strFilterText);

                scanInfo.IsFTMS = ScanIsFTMS(strFilterText);

                if (string.IsNullOrWhiteSpace(scanInfo.FilterText))
                    scanInfo.FilterText = string.Empty;

                if (scanInfo.EventNumber <= 1)
                {
                    int intMSLevel;

                    // XRaw periodically mislabels a scan as .EventNumber = 1 when it's really an MS/MS scan; check for this
                    string strMZText;
                    if (ExtractMSLevel(scanInfo.FilterText, out intMSLevel, out strMZText))
                    {
                        scanInfo.EventNumber = intMSLevel;
                    }
                }

                if (scanInfo.EventNumber > 1)
                {
                    // MS/MS data
                    scanInfo.MSLevel = 2;

                    if (string.IsNullOrWhiteSpace(scanInfo.FilterText))
                    {
                        // FilterText is empty; this indicates a problem with the .Raw file
                        // This is rare, but does happen (see scans 2 and 3 in QC_Shew_08_03_pt5_1_MAXPRO_27Oct08_Raptor_08-01-01.raw)
                        // We'll set the Parent Ion to 0 m/z and the collision mode to CID
                        scanInfo.ParentIonMZ = 0;
                        scanInfo.CollisionMode = "cid";
                        if (scanInfo.ActivationType == ActivationTypeConstants.Unknown)
                        {
                            scanInfo.ActivationType = ActivationTypeConstants.CID;
                        }
                        scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM;
                    }
                    else
                    {
                        double dblParentIonMZ;
                        int intMSLevel;
                        string strCollisionMode;

                        // Parse out the parent ion and collision energy from .FilterText
                        if (ExtractParentIonMZFromFilterText(scanInfo.FilterText, out dblParentIonMZ, out intMSLevel, out strCollisionMode))
                        {
                            scanInfo.ParentIonMZ = dblParentIonMZ;
                            scanInfo.CollisionMode = strCollisionMode;

                            if (intMSLevel > 2)
                            {
                                scanInfo.MSLevel = intMSLevel;
                            }

                            // Check whether this is an SRM MS2 scan
                            scanInfo.MRMScanType = DetermineMRMScanType(scanInfo.FilterText);
                        }
                        else
                        {
                            // Could not find "Full ms2" in .FilterText
                            // XRaw periodically mislabels a scan as .EventNumber > 1 when it's really an MS scan; check for this

                            int msLevel;
                            bool simScan;
                            MRMScanTypeConstants eMRMScanType;
                            bool zoomScan;

                            if (ValidateMSScan(scanInfo.FilterText, out msLevel, out simScan, out eMRMScanType, out zoomScan))
                            {
                                // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                                scanInfo.MSLevel = msLevel;
                                scanInfo.SIMScan = simScan;
                                scanInfo.MRMScanType = eMRMScanType;
                                scanInfo.ZoomScan = zoomScan;
                            }
                            else
                            {
                                // Unknown format for .FilterText; return an error
                                RaiseErrorMessage("Unknown format for Scan Filter: " + scanInfo.FilterText);
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    // MS1 data
                    // Make sure .FilterText contains one of the known MS1, SIM or MRM tags

                    if (scanInfo.FilterText == string.Empty)
                    {
                        // FilterText is empty; this indicates a problem with the .Raw file
                        // This is rare, but does happen (see scans 2 and 3 in QC_Shew_08_03_pt5_1_MAXPRO_27Oct08_Raptor_08-01-01.raw)
                        scanInfo.MSLevel = 1;
                        scanInfo.SIMScan = false;
                        scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM;

                    }
                    else
                    {

                        int msLevel;
                        bool simScan;
                        MRMScanTypeConstants eMRMScanType;
                        bool zoomScan;

                        if (ValidateMSScan(scanInfo.FilterText, out msLevel, out simScan, out eMRMScanType, out zoomScan))
                        {
                            // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                            scanInfo.MSLevel = msLevel;
                            scanInfo.SIMScan = simScan;
                            scanInfo.MRMScanType = eMRMScanType;
                            scanInfo.ZoomScan = zoomScan;
                        }
                        else
                        {
                            // Unknown format for .FilterText; return an error
                            RaiseErrorMessage("Unknown format for Scan Filter: " + scanInfo.FilterText);
                            return false;
                        }
                    }

                }

                scanInfo.IonMode = DetermineIonizationMode(scanInfo.FilterText);

                // Now that we know MSLevel we can lookup the activation type (aka activation method)
                scanInfo.ActivationType = GetActivationType(scan, scanInfo.MSLevel);

                udtMRMInfoType newMRMInfo;

                if (scanInfo.MRMScanType != MRMScanTypeConstants.NotMRM)
                {
                    // Parse out the MRM_QMS or SRM information for this scan
                    ExtractMRMMasses(scanInfo.FilterText, scanInfo.MRMScanType, out newMRMInfo);
                }
                else
                {
                    newMRMInfo = InitializeMRMInfo();
                }

                scanInfo.MRMInfo = newMRMInfo;

                // Retrieve the Status Log for this scan using the following
                // The Status Log includes numerous instrument parameters, including voltages, temperatures, pressures, turbo pump speeds, etc. 
                intArrayCount = 0;
                objLabels = null;
                objValues = null;

                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        double dblStatusLogRT = 0;

                        mXRawFile.GetStatusLogForScanNum(scan, dblStatusLogRT, ref objLabels, ref objValues, ref intArrayCount);
                    }
                }
                catch (AccessViolationException ex)
                {
                    var strWarningMessage = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(strWarningMessage);
                    intArrayCount = 0;

                }
                catch (Exception ex)
                {
                    var strWarningMessage = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(strWarningMessage);
                    intArrayCount = 0;

                    if (ex.Message.ToLower().Contains("memory is corrupt"))
                    {
                        mCorruptMemoryEncountered = true;
                    }
                }

                if (intArrayCount > 0)
                {
                    var logNames =
                        ((IEnumerable)objLabels).Cast<object>()
                            .Select(x => x.ToString())
                            .ToArray();

                    var logValues =
                         ((IEnumerable)objValues).Cast<object>()
                            .Select(x => x.ToString())
                            .ToArray();

                    scanInfo.StoreStatusLog(logNames, logValues);

                }


            }
            catch (Exception ex)
            {
                var strError = "Error: Exception in GetScanInfo: " + ex.Message;
                RaiseWarningMessage(strError);
                CacheScanInfo(scan, scanInfo);
                return false;
            }

            CacheScanInfo(scan, scanInfo);

            return true;

        }

        public static string GetScanTypeNameFromFinniganScanFilterText(string strFilterText)
        {

            // Examines strFilterText to determine what the scan type is
            // Examples:
            // Given                                                                ScanTypeName
            // ITMS + c ESI Full ms [300.00-2000.00]                                MS
            // FTMS + p NSI Full ms [400.00-2000.00]                                HMS
            // ITMS + p ESI d Z ms [579.00-589.00]                                  Zoom-MS
            // ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]             CID-MSn
            // ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]              PQD-MSn
            // FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]             HCD-HMSn
            // ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]          SA_ETD-MSn

            // FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]         HCD-HMSn using multiplexed MSn (introduced with the Q-Exactive)

            // + c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                                       MSn
            // + c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]                          MSn
            // ITMS + c NSI d Full ms10 421.76@35.00                                                MSn
            // ITMS + p NSI CRM ms3 332.14@cid35.00 288.10@cid35.00 [242.00-248.00, 285.00-291.00]  CID-MSn

            // + p ms2 777.00@cid30.00 [210.00-1200.00]                                             CID-MSn
            // + c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]                   CID-SRM
            // + c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]    CID-SRM
            // + p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]                     Q1MS
            // + p NSI Q3MS [150.070-1500.000]                                                      Q3MS
            // c NSI Full cnl 162.053 [300.000-1200.000]                                            MRM_Full_NL

            // Lumos scan filter examples
            // FTMS + p NSI Full ms                                                                 HMS
            // ITMS + c NSI r d Full ms2 916.3716@cid30.00 [247.0000-2000.0000]                     CID-MSn
            // ITMS + c NSI r d Full ms2 916.3716@hcd30.00 [100.0000-2000.0000]                     HCD-MSn

            // ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]       ETciD-MSn  (ETD fragmentation, then further fragmented by CID in the ion trap; detected with the ion trap)
            // ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]       EThcD-MSn  (ETD fragmentation, then further fragmented by HCD in the ion routing multipole; detected with the ion trap)

            // FTMS + c NSI r d Full ms2 744.0129@cid30.00 [199.0000-2000.0000]                     CID-HMSn
            // FTMS + p NSI r d Full ms2 944.4316@hcd30.00 [100.0000-2000.0000]                     HCD-HMSn

            // FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]       ETciD-HMSn  (ETD fragmentation, then further fragmented by CID in the ion trap; detected with orbitrap)
            // FTMS + c NSI r d sa Full ms2 1073.4800@etd120.55@hcd30.00 [120.0000-2000.0000]       EThcD-HMSn  (ETD fragmentation, then further fragmented by HCD in the ion routing multipole; detected with orbitrap)

            var strScanTypeName = "MS";

            try
            {
                var blnValidScanFilter = true;
                int intMSLevel;
                var strCollisionMode = string.Empty;
                MRMScanTypeConstants eMRMScanType;
                var blnSIMScan = false;
                var blnZoomScan = false;

                if (strFilterText.Length == 0)
                {
                    strScanTypeName = "MS";
                    return strScanTypeName;
                }

                string strMZText;

                if (!ExtractMSLevel(strFilterText, out intMSLevel, out strMZText))
                {
                    // Assume this is an MS scan
                    intMSLevel = 1;
                }

                if (intMSLevel > 1)
                {
                    // Parse out the parent ion and collision energy from strFilterText

                    double dblParentIonMZ;
                    if (ExtractParentIonMZFromFilterText(strFilterText, out dblParentIonMZ, out intMSLevel, out strCollisionMode))
                    {
                        // Check whether this is an SRM MS2 scan
                        eMRMScanType = DetermineMRMScanType(strFilterText);
                    }
                    else
                    {
                        // Could not find "Full ms2" in strFilterText
                        // XRaw periodically mislabels a scan as .EventNumber > 1 when it's really an MS scan; check for this
                        if (ValidateMSScan(strFilterText, out intMSLevel, out blnSIMScan, out eMRMScanType, out blnZoomScan))
                        {
                            // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                        }
                        else
                        {
                            // Unknown format for strFilterText; return an error
                            blnValidScanFilter = false;
                        }
                    }
                }
                else
                {
                    // MSLevel is 1
                    // Make sure .FilterText contains one of the known MS1, SIM or MRM tags
                    if (ValidateMSScan(strFilterText, out intMSLevel, out blnSIMScan, out eMRMScanType, out blnZoomScan))
                    {
                        // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                    }
                    else
                    {
                        // Unknown format for strFilterText; return an error
                        blnValidScanFilter = false;
                    }
                }


                if (blnValidScanFilter)
                {
                    if (eMRMScanType == MRMScanTypeConstants.NotMRM)
                    {
                        if (blnSIMScan)
                        {
                            strScanTypeName = SIM_MS_TEXT.Trim();
                        }
                        else if (blnZoomScan)
                        {
                            strScanTypeName = "Zoom-MS";

                        }
                        else
                        {
                            // Normal, plain MS or MSn scan

                            if (intMSLevel > 1)
                            {
                                strScanTypeName = "MSn";
                            }
                            else
                            {
                                strScanTypeName = "MS";
                            }

                            if (ScanIsFTMS(strFilterText))
                            {
                                // HMS or HMSn scan
                                strScanTypeName = "H" + strScanTypeName;
                            }

                            if (intMSLevel > 1 && strCollisionMode.Length > 0)
                            {
                                strScanTypeName = CapitalizeCollisionMode(strCollisionMode) + "-" + strScanTypeName;
                            }

                        }
                    }
                    else
                    {
                        // This is an MRM or SRM scan

                        switch (eMRMScanType)
                        {
                            case MRMScanTypeConstants.MRMQMS:
                                if (ContainsText(strFilterText, MRM_Q1MS_TEXT, 1))
                                {
                                    strScanTypeName = MRM_Q1MS_TEXT.Trim();

                                }
                                else if (ContainsText(strFilterText, MRM_Q3MS_TEXT, 1))
                                {
                                    strScanTypeName = MRM_Q3MS_TEXT.Trim();
                                }
                                else
                                {
                                    // Unknown QMS mode
                                    strScanTypeName = "MRM QMS";
                                }

                                break;
                            case MRMScanTypeConstants.SRM:
                                if (strCollisionMode.Length > 0)
                                {
                                    strScanTypeName = strCollisionMode.ToUpper() + "-SRM";
                                }
                                else
                                {
                                    strScanTypeName = "CID-SRM";
                                }

                                break;

                            case MRMScanTypeConstants.FullNL:
                                strScanTypeName = "MRM_Full_NL";

                                break;
                            default:
                                strScanTypeName = "MRM";
                                break;
                        }

                    }


                }


            }
            catch (Exception)
            {
                // Ignore errors here
            }

            return strScanTypeName;

        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        private void GetTuneData()
        {
            var intNumTuneData = 0;
            mXRawFile.GetNumTuneData(ref intNumTuneData);

            for (var intIndex = 0; intIndex <= intNumTuneData - 1; intIndex++)
            {
                var intTuneLabelCount = 0;
                object objLabels = null;
                object objValues = null;

                string strWarningMessage;
                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        mXRawFile.GetTuneData(intIndex, ref objLabels, ref objValues, ref intTuneLabelCount);
                    }

                }
                catch (AccessViolationException)
                {
                    strWarningMessage = "Unable to load tune data; possibly a corrupt .Raw file";
                    RaiseWarningMessage(strWarningMessage);
                    break;

                }
                catch (Exception ex)
                {
                    // Exception getting TuneData
                    strWarningMessage = "Warning: Exception calling mXRawFile.GetTuneData for Index " + intIndex + ": " + ex.Message;
                    RaiseWarningMessage(strWarningMessage);
                    intTuneLabelCount = 0;

                    if (ex.Message.ToLower().Contains("memory is corrupt"))
                    {
                        mCorruptMemoryEncountered = true;
                        break;
                    }
                }


                if (intTuneLabelCount > 0)
                {
                    strWarningMessage = string.Empty;
                    if (objLabels == null)
                    {
                        // .GetTuneData returned a non-zero count, but no parameter names; unable to continue
                        strWarningMessage = "Warning: the GetTuneData function returned a positive tune parameter count but no parameter names";
                    }
                    else if (objValues == null)
                    {
                        // .GetTuneData returned parameter names, but objValues is nothing; unable to continue
                        strWarningMessage = "Warning: the GetTuneData function returned tune parameter names but no tune values";
                    }

                    if (strWarningMessage.Length > 0)
                    {
                        strWarningMessage += " (Tune Method " + (intIndex + 1) + ")";
                        RaiseWarningMessage(strWarningMessage);
                        intTuneLabelCount = 0;
                    }

                }

                if (intTuneLabelCount <= 0 || objLabels == null || objValues == null)
                {
                    continue;
                }

                var newTuneMethod = new udtTuneMethodType();
                newTuneMethod.Clear();


                var strTuneSettingNames =
                    ((IEnumerable)objLabels).Cast<object>()
                        .Select(x => x.ToString())
                        .ToArray();

                var strTuneSettingValues =
                    ((IEnumerable)objValues).Cast<object>()
                        .Select(x => x.ToString())
                        .ToArray();

                // Step through the names and store in the .Setting() arrays
                var strTuneCategory = "General";
                for (var intSettingIndex = 0; intSettingIndex <= intTuneLabelCount - 1; intSettingIndex++)
                {
                    if (strTuneSettingValues[intSettingIndex].Length == 0 && !strTuneSettingNames[intSettingIndex].EndsWith(":"))
                    {
                        // New category
                        if (strTuneSettingNames[intSettingIndex].Length > 0)
                        {
                            strTuneCategory = string.Copy(strTuneSettingNames[intSettingIndex]);
                        }
                        else
                        {
                            strTuneCategory = "General";
                        }
                    }
                    else
                    {
                        var tuneMethodSetting = new udtTuneMethodSetting()
                        {
                            Category = string.Copy(strTuneCategory),
                            Name = strTuneSettingNames[intSettingIndex].TrimEnd(':'),
                            Value = string.Copy(strTuneSettingValues[intSettingIndex])
                        };

                        newTuneMethod.Settings.Add(tuneMethodSetting);
                    }

                }

                if (mFileInfo.TuneMethods.Count == 0)
                    mFileInfo.TuneMethods.Add(newTuneMethod);
                else
                {
                    // Compare this tune method to the previous one; if identical, then don't keep it
                    if (!TuneMethodsMatch(mFileInfo.TuneMethods.Last(), newTuneMethod))
                    {
                        mFileInfo.TuneMethods.Add(newTuneMethod);
                    }
                }
            }

        }

        public static string MakeGenericFinniganScanFilter(string strFilterText)
        {

            // Will make a generic version of the FilterText in strFilterText
            // Examples:
            // From                                                                 To
            // ITMS + c ESI Full ms [300.00-2000.00]                                ITMS + c ESI Full ms
            // FTMS + p NSI Full ms [400.00-2000.00]                                FTMS + p NSI Full ms
            // ITMS + p ESI d Z ms [579.00-589.00]                                  ITMS + p ESI d Z ms
            // ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]             ITMS + c ESI d Full ms2 0@cid35.00
            // ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]              ITMS + c NSI d Full ms2 0@pqd27.00
            // FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]             FTMS + c NSI d Full ms2 0@hcd40.00
            // ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]          ITMS + c NSI d sa Full ms2 0@etd100.00

            // + c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                       + c d Full ms2 0@45.00
            // + c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]          + c d Full ms3 0@45.00 0@45.00
            // ITMS + c NSI d Full ms10 421.76@35.00                                ITMS + c NSI d Full ms10 0@35.00

            // + p ms2 777.00@cid30.00 [210.00-1200.00]                             + p ms2 0@cid30.00
            // + c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]   + c NSI SRM ms2 0@cid15.00
            // + c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]    + c NSI SRM ms2
            // + p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]     + p NSI Q1MS
            // + p NSI Q3MS [150.070-1500.000]                                      + p NSI Q3MS
            // c NSI Full cnl 162.053 [300.000-1200.000]                            c NSI Full cnl

            var strGenericScanFilterText = "MS";

            try
            {
                if (!string.IsNullOrWhiteSpace(strFilterText))
                {

                    strGenericScanFilterText = string.Copy(strFilterText);

                    // First look for and remove numbers between square brackets
                    var intCharIndex = strGenericScanFilterText.IndexOf('[');
                    if (intCharIndex > 0)
                    {
                        strGenericScanFilterText = strGenericScanFilterText.Substring(0, intCharIndex).TrimEnd(' ');
                    }
                    else
                    {
                        strGenericScanFilterText = strGenericScanFilterText.TrimEnd(' ');
                    }

                    intCharIndex = strGenericScanFilterText.IndexOf(MRM_FullNL_TEXT, StringComparison.InvariantCultureIgnoreCase);
                    if (intCharIndex > 0)
                    {
                        // MRM neutral loss
                        // Remove any text after MRM_FullNL_TEXT
                        strGenericScanFilterText = strGenericScanFilterText.Substring(0, intCharIndex + MRM_FullNL_TEXT.Length).Trim();
                        return strGenericScanFilterText;
                    }

                    // Replace any digits before any @ sign with a 0
                    if (strGenericScanFilterText.IndexOf('@') > 0)
                    {
                        strGenericScanFilterText = mCollisionSpecs.Replace(strGenericScanFilterText, " 0@");
                    }
                    else
                    {
                        // No @ sign; look for text of the form "ms2 748.371"
                        var reMatch = mMzWithoutCE.Match(strGenericScanFilterText);
                        if (reMatch.Success)
                        {
                            strGenericScanFilterText = strGenericScanFilterText.Substring(0, reMatch.Groups["MzValue"].Index)
                            ;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return strGenericScanFilterText;

        }

        private static bool ScanIsFTMS(string strFilterText)
        {

            return ContainsText(strFilterText, "FTMS");

        }

        private udtScanHeaderInfoType ScanInfoClassToStruct(clsScanInfo scanInfo)
        {

            var udtScanInfo = new udtScanHeaderInfoType
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
                ScanEventValues = new string[scanInfo.ScanEvents.Count]
            };

            for (var i = 0; i < scanInfo.ScanEvents.Count; i++)
            {
                udtScanInfo.ScanEventNames[i] = scanInfo.ScanEvents[i].Key;
                udtScanInfo.ScanEventValues[i] = scanInfo.ScanEvents[i].Value;
            }

            udtScanInfo.StatusLogNames = new string[scanInfo.StatusLog.Count];
            udtScanInfo.StatusLogValues = new string[scanInfo.StatusLog.Count];

            for (var i = 0; i < scanInfo.StatusLog.Count; i++)
            {
                udtScanInfo.StatusLogNames[i] = scanInfo.StatusLog[i].Key;
                udtScanInfo.StatusLogValues[i] = scanInfo.StatusLog[i].Value;
            }

            return udtScanInfo;

        }

        private bool SetMSController()
        {
            // A controller is typically the MS, UV, analog, etc.
            // See ControllerTypeConstants

            var intResult = 0;

            mXRawFile.SetCurrentController((int)ControllerTypeConstants.MS, 1);
            mXRawFile.IsError(ref intResult);
            // Unfortunately, .IsError() always returns 0, even if an error occurred

            if (intResult == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Examines strFilterText to validate that it is a supported MS1 scan type (MS, SIM, or MRMQMS, or SRM scan)
        /// </summary>
        /// <param name="strFilterText"></param>
        /// <param name="intMSLevel"></param>
        /// <param name="blnSIMScan"></param>
        /// <param name="eMRMScanType"></param>
        /// <param name="blnZoomScan"></param>
        /// <returns>True if strFilterText contains a known MS scan type</returns>
        /// <remarks>Returns false for MSn scans (like ms2 or ms3)</remarks>
        public static bool ValidateMSScan(string strFilterText, out int intMSLevel, out bool blnSIMScan, out MRMScanTypeConstants eMRMScanType, out bool blnZoomScan)
        {

            intMSLevel = 0;
            blnSIMScan = false;
            eMRMScanType = MRMScanTypeConstants.NotMRM;
            blnZoomScan = false;

            var ms1Tags = new List<string> {
				FULL_MS_TEXT,
				MS_ONLY_C_TEXT,
				MS_ONLY_P_TEXT,
				MS_ONLY_P_NSI_TEXT,
				FULL_PR_TEXT,
				FULL_LOCK_MS_TEXT
			};

            var zoomTags = new List<string> {
				MS_ONLY_Z_TEXT,
				MS_ONLY_PZ_TEXT,
				MS_ONLY_DZ_TEXT
			};

            if (ContainsAny(strFilterText, ms1Tags, 1))
            {
                // This is really a Full MS scan
                intMSLevel = 1;
                blnSIMScan = false;
                return true;
            }

            if (ContainsText(strFilterText, SIM_MS_TEXT, 1))
            {
                // This is really a SIM MS scan
                intMSLevel = 1;
                blnSIMScan = true;
                return true;
            }

            if (ContainsAny(strFilterText, zoomTags, 1))
            {
                intMSLevel = 1;
                blnZoomScan = true;
                return true;
            }

            if (ContainsText(strFilterText, MS_ONLY_PZ_MS2_TEXT, 1))
            {
                // Technically, this should have MSLevel = 2, but that would cause a bunch of problems elsewhere in MASIC
                // Thus, we'll pretend it's MS1
                intMSLevel = 1;
                blnZoomScan = true;
                return true;
            }

            eMRMScanType = DetermineMRMScanType(strFilterText);
            switch (eMRMScanType)
            {
                case MRMScanTypeConstants.MRMQMS:
                    intMSLevel = 1;
                    // ToDo: Add support for TSQ MRMQMS data
                    return true;
                case MRMScanTypeConstants.SRM:
                    intMSLevel = 2;
                    // ToDo: Add support for TSQ SRM data
                    return true;
                case MRMScanTypeConstants.FullNL:
                    intMSLevel = 2;
                    // ToDo: Add support for TSQ Full NL data
                    return true;
                default:
                    return false;
            }

        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMZList"></param>
        /// <param name="dblIntensityList"></param>
        /// <param name="udtScanInfo">Unused; parameter retained for compatibility reasons</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")]
        public int GetScanData(int scan, out double[] dblMZList, out double[] dblIntensityList, ref udtScanHeaderInfoType udtScanInfo)
        {
            const int intMaxNumberOfPeaks = 0;
            const bool blnCentroid = false;
            return GetScanData(scan, out dblMZList, out dblIntensityList, intMaxNumberOfPeaks, blnCentroid);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMZList"></param>
        /// <param name="dblIntensityList"></param>
        /// <param name="udtScanInfo">Unused; parameter retained for compatibility reasons</param>
        /// <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")]
        public int GetScanData(int scan, out double[] dblMZList, out double[] dblIntensityList, ref udtScanHeaderInfoType udtScanInfo, bool blnCentroid)
        {
            const int intMaxNumberOfPeaks = 0;
            return GetScanData(scan, out dblMZList, out dblIntensityList, intMaxNumberOfPeaks, blnCentroid);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMZList"></param>
        /// <param name="dblIntensityList"></param>
        /// <param name="udtScanInfo">Unused; parameter retained for compatibility reasons</param>
        /// <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")]
        public int GetScanData(int scan, out double[] dblMZList, out double[] dblIntensityList, out udtScanHeaderInfoType udtScanInfo, int intMaxNumberOfPeaks)
        {
            const bool blnCentroid = false;
            udtScanInfo = new udtScanHeaderInfoType();
            return GetScanData(scan, out dblMZList, out dblIntensityList, intMaxNumberOfPeaks, blnCentroid);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMZList"></param>
        /// <param name="dblIntensityList"></param>
        /// <param name="udtScanInfo">Unused; parameter retained for compatibility reasons</param>
        /// <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [Obsolete("This method is deprecated, use GetScanData that does not use udtScanHeaderInfo")]
        public int GetScanData(int scan, out double[] dblMZList, out double[] dblIntensityList, out udtScanHeaderInfoType udtScanInfo, int intMaxNumberOfPeaks, bool blnCentroid)
        {
            udtScanInfo = new udtScanHeaderInfoType();
            return GetScanData(scan, out dblMZList, out dblIntensityList, intMaxNumberOfPeaks, blnCentroid);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scanNumber">Scan number</param>
        /// <param name="mzList">Output array of mass values</param>
        /// <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        public override int GetScanData(int scanNumber, out double[] mzList, out double[] intensityList)
        {
            const int intMaxNumberOfPeaks = 0;
            const bool blnCentroid = false;
            return GetScanData(scanNumber, out mzList, out intensityList, intMaxNumberOfPeaks, blnCentroid);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <param name="scanNumber">Scan number</param>
        /// <param name="mzList">Output array of mass values</param>
        /// <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        /// <param name="maxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        public override int GetScanData(int scanNumber, out double[] mzList, out double[] intensityList, int maxNumberOfPeaks)
        {
            const bool centroid = false;
            return GetScanData(scanNumber, out mzList, out intensityList, maxNumberOfPeaks, centroid);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="dblMZList">Output array of mass values</param>
        /// <param name="dblIntensityList">Output array of intensity values (parallel to mzList)</param>
        /// <param name="intMaxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        public int GetScanData(int scan, out double[] dblMZList, out double[] dblIntensityList, int intMaxNumberOfPeaks, bool blnCentroid)
        {

            double[,] dblMassIntensityPairs;

            var dataCount = GetScanData2D(scan, out dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid);

            try
            {
                if (dataCount <= 0)
                {
                    dblMZList = new double[0];
                    dblIntensityList = new double[0];
                    return 0;
                }

                if (dblMassIntensityPairs.GetUpperBound(1) + 1 < dataCount)
                {
                    dataCount = dblMassIntensityPairs.GetUpperBound(1) + 1;
                }

                dblMZList = new double[dataCount];
                dblIntensityList = new double[dataCount];
                var sortRequired = false;

                for (var intIndex = 0; intIndex <= dataCount - 1; intIndex++)
                {
                    dblMZList[intIndex] = dblMassIntensityPairs[0, intIndex];
                    dblIntensityList[intIndex] = dblMassIntensityPairs[1, intIndex];

                    // Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z, 
                    // we have observed a few cases in certain scans of certain datasets that points with 
                    // similar m/z values are swapped and ths slightly out of order
                    // The following if statement checks for this
                    if ((intIndex > 0 && dblMZList[intIndex] < dblMZList[intIndex - 1]))
                    {
                        sortRequired = true;
                    }

                }

                if (sortRequired)
                {
                    Array.Sort(dblMZList, dblIntensityList);
                }

            }
            catch
            {
                dblMZList = new double[0];
                dblIntensityList = new double[0];
                dataCount = -1;
            }

            return dataCount;

        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        public int GetScanData2D(int scan, out double[,] dblMassIntensityPairs)
        {
            return GetScanData2D(scan, out dblMassIntensityPairs, intMaxNumberOfPeaks: 0, blnCentroid: false);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <param name="udtScanInfo">Unused; parameter retained for compatibility reasons</param>
        /// <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [Obsolete("This method is deprecated, use GetScanData2D that does not use udtScanHeaderInfo")]
        public int GetScanData2D(int scan, out double[,] dblMassIntensityPairs, ref udtScanHeaderInfoType udtScanInfo, int intMaxNumberOfPeaks)
        {
            return GetScanData2D(scan, out dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid: false);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        public int GetScanData2D(int scan, out double[,] dblMassIntensityPairs, int intMaxNumberOfPeaks)
        {
            return GetScanData2D(scan, out dblMassIntensityPairs, intMaxNumberOfPeaks, blnCentroid: false);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="dblMassIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <param name="intMaxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <param name="blnCentroid">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>If intMaxNumberOfPeaks is 0 (or negative), then returns all data; set intMaxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public int GetScanData2D(int scan, out double[,] dblMassIntensityPairs, int intMaxNumberOfPeaks, bool blnCentroid)
        {

            // Note that we're using function attribute HandleProcessCorruptedStateExceptions
            // to force .NET to properly catch critical errors thrown by the XRawfile DLL

            var dataCount = 0;

            if (scan < mFileInfo.ScanStart)
            {
                scan = mFileInfo.ScanStart;
            }
            else if (scan > mFileInfo.ScanEnd)
            {
                scan = mFileInfo.ScanEnd;
            }

            clsScanInfo scanInfo;

            if (!GetScanInfo(scan, out scanInfo))
            {
                throw new Exception("Cannot retrieve ScanInfo from cache for scan " + scan + "; cannot retrieve scan data");
            }

            try
            {
                if (mXRawFile == null)
                {
                    dblMassIntensityPairs = new double[0, 0];
                    return -1;
                }

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    dblMassIntensityPairs = new double[0, 0];
                    return -1;
                }

                var strFilter = string.Empty;
                // Could use this to filter the data returned from the scan; must use one of the filters defined in the file (see .GetFilters())
                var intIntensityCutoffValue = 0;

                if (intMaxNumberOfPeaks < 0)
                    intMaxNumberOfPeaks = 0;

                if (blnCentroid && scanInfo.IsCentroided)
                {
                    // The scan data is already centroided; don't try to re-centroid
                    blnCentroid = false;
                }

                if (blnCentroid && scanInfo.IsFTMS)
                {
                    // Centroiding is enabled, and the dataset was acquired on an Orbitrap, Exactive, or FTMS instrument 

                    object massIntensityLabels = null;
                    object labelFlags = null;

                    mXRawFile.GetLabelData(ref massIntensityLabels, ref labelFlags, scan);

                    //var massIntensityLabels2D = (object[,])massIntensityLabels;
                    //double[,] dblMassIntensityLabels = Cast2D<double>(massIntensityLabels2D);
                    var dblMassIntensityLabels = (double[,])massIntensityLabels;

                    dataCount = dblMassIntensityLabels.GetLength(1);

                    if (dataCount > 0)
                    {
                        dblMassIntensityPairs = new double[2, dataCount];

                        for (var i = 0; i <= dataCount - 1; i++)
                        {
                            dblMassIntensityPairs[0, i] = dblMassIntensityLabels[0, i];
                            // m/z
                            dblMassIntensityPairs[1, i] = dblMassIntensityLabels[1, i];
                            // Intensity
                        }

                    }
                    else
                    {
                        dblMassIntensityPairs = new double[0, 0];
                    }

                    // Dim byteFlags As Byte(,)
                    // byteFlags = CType(labelFlags, Byte(,))

                }
                else
                {
                    // Warning: The masses reported by GetMassListFromScanNum when centroiding are not properly calibrated and thus could be off by 0.3 m/z or more
                    //          That is why we use mXRawFile.GetLabelData() when centroiding profile-mode FTMS data (see ~25 lines above this comment)
                    //
                    //          For example, in scan 8101 of dataset RAW_Franc_Salm_IMAC_0h_R1A_18Jul13_Frodo_13-04-15, we see these values:
                    //           Profile m/z         Centroid m/z	Delta_PPM
                    //			112.051 			112.077			232
                    //			652.3752			652.4645		137
                    //			1032.56495			1032.6863		118
                    //			1513.7252			1513.9168		127

                    int intCentroidResult;
                    double dblCentroidPeakWidth = 0;

                    if (blnCentroid)
                    {
                        intCentroidResult = 1;
                    }
                    else
                    {
                        intCentroidResult = 0;
                    }

                    object massIntensityPairsList = null;
                    object peakList = null;

                    mXRawFile.GetMassListFromScanNum(ref scan, strFilter, (int)IntensityCutoffTypeConstants.None, intIntensityCutoffValue, intMaxNumberOfPeaks, intCentroidResult, dblCentroidPeakWidth, ref massIntensityPairsList, ref peakList, ref dataCount);

                    if (dataCount > 0)
                    {
                        dblMassIntensityPairs = (double[,])massIntensityPairsList;
                    }
                    else
                    {
                        dblMassIntensityPairs = new double[0, 0];
                    }

                }

                return dataCount;

            }
            catch (AccessViolationException)
            {
                var strError = "Unable to load data for scan " + scan + "; possibly a corrupt .Raw file";
                RaiseWarningMessage(strError);


            }
            catch (Exception ex)
            {
                var strError = "Unable to load data for scan " + scan + ": " + ex.Message + "; possibly a corrupt .Raw file";
                RaiseErrorMessage(strError);

            }

            dblMassIntensityPairs = new double[0, 0];
            return -1;

        }

        /// <summary>
        /// Gets the scan label data for an FTMS-tagged scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="ftLabelData">List of mass, intensity, resolution, baseline intensity, noise floor, and charge for each data point</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks></remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public int GetScanLabelData(int scan, out udtFTLabelInfoType[] ftLabelData)
        {

            // Note that we're using function attribute HandleProcessCorruptedStateExceptions
            // to force .NET to properly catch critical errors thrown by the XRawfile DLL

            if (scan < mFileInfo.ScanStart)
            {
                scan = mFileInfo.ScanStart;
            }
            else if (scan > mFileInfo.ScanEnd)
            {
                scan = mFileInfo.ScanEnd;
            }

            clsScanInfo scanInfo;

            if (!GetScanInfo(scan, out scanInfo))
            {
                throw new Exception("Cannot retrieve ScanInfo from cache for scan " + scan + "; cannot retrieve scan data");
            }

            try
            {
                if (mXRawFile == null)
                {
                    ftLabelData = new udtFTLabelInfoType[0];
                    return -1;
                }

                if (!scanInfo.IsFTMS)
                {
                    var strWarningMessage = "Scan " + scan + " is not an FTMS scan; function GetScanLabelData cannot be used with this scan";
                    RaiseWarningMessage(strWarningMessage);
                    ftLabelData = new udtFTLabelInfoType[0];
                    return -1;
                }

                object labelData = null;
                object labelFlags = null;

                mXRawFile.GetLabelData(ref labelData, ref labelFlags, ref scan);

                //var labelData2D = (object[,])labelData;
                //double[,] labelDataArray = Cast2D<double>(labelData2D);
                var labelDataArray = (double[,])labelData;

                var dataCount = labelDataArray.GetLength(1);
                var maxColIndex = labelDataArray.GetLength(0) - 1;

                if (dataCount > 0)
                {
                    ftLabelData = new udtFTLabelInfoType[dataCount];

                    for (var i = 0; i <= dataCount - 1; i++)
                    {
                        var labelInfo = new udtFTLabelInfoType
                        {
                            Mass = labelDataArray[0, i],
                            Intensity = labelDataArray[1, i]
                        };

                        if (maxColIndex >= 2)
                        {
                            labelInfo.Resolution = Convert.ToSingle(labelDataArray[2, i]);
                        }

                        if (maxColIndex >= 3)
                        {
                            labelInfo.Baseline = Convert.ToSingle(labelDataArray[3, i]);
                        }

                        if (maxColIndex >= 4)
                        {
                            labelInfo.Noise = Convert.ToSingle(labelDataArray[4, i]);
                        }

                        if (maxColIndex >= 5)
                        {
                            labelInfo.Charge = Convert.ToInt32(labelDataArray[5, i]);
                        }

                        ftLabelData[i] = labelInfo;
                    }

                }
                else
                {
                    ftLabelData = new udtFTLabelInfoType[0];
                }

                return dataCount;

            }
            catch (AccessViolationException)
            {
                var strError = "Unable to load data for scan " + scan + "; possibly a corrupt .Raw file";
                RaiseWarningMessage(strError);


            }
            catch (Exception ex)
            {
                var strError = "Unable to load data for scan " + scan + ": " + ex.Message + "; possibly a corrupt .Raw file";
                RaiseErrorMessage(strError);

            }

            ftLabelData = new udtFTLabelInfoType[0];
            return -1;

        }

        /// <summary>
        /// Gets scan precision data for FTMS data (resolution of each data point)
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="massResolutionData">List of Intensity, Mass, AccuracyMMU, AccuracyPPM, and Resolution for each data point</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        /// <remarks>This returns a subset of the data thatGetScanLabelData does, but with 2 additional fields.</remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public int GetScanPrecisionData(int scan, out udtMassPrecisionInfoType[] massResolutionData)
        {

            // Note that we're using function attribute HandleProcessCorruptedStateExceptions
            // to force .NET to properly catch critical errors thrown by the XRawfile DLL

            var dataCount = 0;

            if (scan < mFileInfo.ScanStart)
            {
                scan = mFileInfo.ScanStart;
            }
            else if (scan > mFileInfo.ScanEnd)
            {
                scan = mFileInfo.ScanEnd;
            }

            clsScanInfo scanInfo;

            if (!GetScanInfo(scan, out scanInfo))
            {
                throw new Exception("Cannot retrieve ScanInfo from cache for scan " + scan + "; cannot retrieve scan data");
            }

            try
            {
                if (mXRawFile == null)
                {
                    massResolutionData = new udtMassPrecisionInfoType[0];
                    return -1;
                }

                if (!scanInfo.IsFTMS)
                {
                    var strWarningMessage = "Scan " + scan + " is not an FTMS scan; function GetScanLabelData cannot be used with this scan";
                    RaiseWarningMessage(strWarningMessage);
                    massResolutionData = new udtMassPrecisionInfoType[0];
                    return -1;
                }

                object massResolutionDataList = null;

                mXRawFile.GetMassPrecisionEstimate(scan, ref massResolutionDataList, ref dataCount);

                //var massResolutionDataList2D = (object[,])massResolutionDataList;
                //double[,] massPrecisionArray = Cast2D<double>(massResolutionDataList2D);
                var massPrecisionArray = (double[,])massResolutionDataList;

                dataCount = massPrecisionArray.GetLength(1);

                if (dataCount > 0)
                {
                    massResolutionData = new udtMassPrecisionInfoType[dataCount];

                    for (var i = 0; i <= dataCount - 1; i++)
                    {
                        var massPrecisionInfo = new udtMassPrecisionInfoType
                        {
                            Intensity = massPrecisionArray[0, i],
                            Mass = massPrecisionArray[1, i],
                            AccuracyMMU = massPrecisionArray[2, i],
                            AccuracyPPM = massPrecisionArray[3, i],
                            Resolution = massPrecisionArray[4, i]
                        };

                        massResolutionData[i] = massPrecisionInfo;
                    }

                }
                else
                {
                    massResolutionData = new udtMassPrecisionInfoType[0];
                }

                return dataCount;

            }
            catch (AccessViolationException)
            {
                var strError = "Unable to load data for scan " + scan + "; possibly a corrupt .Raw file";
                RaiseWarningMessage(strError);


            }
            catch (Exception ex)
            {
                var strError = "Unable to load data for scan " + scan + ": " + ex.Message + "; possibly a corrupt .Raw file";
                RaiseErrorMessage(strError);

            }

            massResolutionData = new udtMassPrecisionInfoType[0];
            return -1;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scanFirst"></param>
        /// <param name="scanLast"></param>
        /// <param name="dblMassIntensityPairs"></param>
        /// <param name="intMaxNumberOfPeaks"></param>
        /// <param name="blnCentroid"></param>
        /// <returns>The number of data points</returns>
        /// <remarks></remarks>
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions()]
        public int GetScanDataSumScans(int scanFirst, int scanLast, out double[,] dblMassIntensityPairs, int intMaxNumberOfPeaks, bool blnCentroid)
        {

            // Note that we're using function attribute HandleProcessCorruptedStateExceptions
            // to force .NET to properly catch critical errors thrown by the XRawfile DLL

            double dblCentroidPeakWidth = 0;
            var dataCount = 0;

            try
            {
                if (mXRawFile == null)
                {
                    dblMassIntensityPairs = new double[0, 0];
                    return -1;
                }

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    dblMassIntensityPairs = new double[0, 0];
                    return -1;
                }

                if (scanFirst < mFileInfo.ScanStart)
                {
                    scanFirst = mFileInfo.ScanStart;
                }
                else if (scanFirst > mFileInfo.ScanEnd)
                {
                    scanFirst = mFileInfo.ScanEnd;
                }

                if (scanLast < scanFirst)
                    scanLast = scanFirst;

                if (scanLast < mFileInfo.ScanStart)
                {
                    scanLast = mFileInfo.ScanStart;
                }
                else if (scanLast > mFileInfo.ScanEnd)
                {
                    scanLast = mFileInfo.ScanEnd;
                }

                var strFilter = string.Empty;
                // Could use this to filter the data returned from the scan; must use one of the filters defined in the file (see .GetFilters())
                var intIntensityCutoffValue = 0;

                if (intMaxNumberOfPeaks < 0)
                    intMaxNumberOfPeaks = 0;

                // Warning: the masses reported by GetAverageMassList when centroiding are not properly calibrated and thus could be off by 0.3 m/z or more
                // For an example, see function GetScanData2D above

                int intCentroidResult;
                if (blnCentroid)
                {
                    intCentroidResult = 1;
                    // Set to 1 to indicate that peaks should be centroided (only appropriate for profile data)
                }
                else
                {
                    intCentroidResult = 0;
                    // Return the data as-is
                }

                var backgroundScan1First = 0;
                var backgroundScan1Last = 0;
                var backgroundScan2First = 0;
                var backgroundScan2Last = 0;

                object massIntensityPairsList = null;
                object peakList = null;

                mXRawFile.GetAverageMassList(ref scanFirst, ref scanLast, ref  backgroundScan1First, ref backgroundScan1Last, ref backgroundScan2First, ref backgroundScan2Last, strFilter, (int)IntensityCutoffTypeConstants.None, intIntensityCutoffValue, intMaxNumberOfPeaks,
                intCentroidResult, ref dblCentroidPeakWidth, ref massIntensityPairsList, ref peakList, ref dataCount);

                if (dataCount > 0)
                {
                    var massIntensityPairs2D = (object[,])massIntensityPairsList;
                    dblMassIntensityPairs = Cast2D<double>(massIntensityPairs2D);
                }
                else
                {
                    dblMassIntensityPairs = new double[0, 0];
                }

                return dataCount;

            }
            catch (AccessViolationException)
            {
                var strError = "Unable to load data summing scans " + scanFirst + " to " + scanLast + "; possibly a corrupt .Raw file";
                RaiseWarningMessage(strError);


            }
            catch (Exception ex)
            {
                var strError = "Unable to load data summing scans " + scanFirst + " to " + scanLast + ": " + ex.Message + "; possibly a corrupt .Raw file";
                RaiseErrorMessage(strError);

            }

            dblMassIntensityPairs = new double[0, 0];
            return -1;

        }

        public static udtMRMInfoType InitializeMRMInfo()
        {
            var udtMRMInfo = new udtMRMInfoType();
            udtMRMInfo.Clear();

            return udtMRMInfo;
        }

        [Obsolete("Use parameterless function InitializeMRMInfo instead")]
        public static void InitializeMRMInfo(out udtMRMInfoType udtMRMInfo, int intInitialMassCountCapacity)
        {
            udtMRMInfo = InitializeMRMInfo();
        }

        public sealed override bool OpenRawFile(string FileName)
        {
            var intResult = 0;
            var blnSuccess = false;


            try
            {
                // Make sure any existing open files are closed
                CloseRawFile();

                mCachedScanInfo.Clear();

                if (mXRawFile == null)
                {
                    mXRawFile = (IXRawfile5)new MSFileReader_XRawfile();
                }

                mXRawFile.Open(FileName);
                mXRawFile.IsError(ref intResult);
                // Unfortunately, .IsError() always returns 0, even if an error occurred

                if (intResult == 0)
                {
                    mCachedFileName = FileName;
                    if (FillFileInfo())
                    {

                        if (mFileInfo.ScanStart == 0 && mFileInfo.ScanEnd == 0 && mFileInfo.VersionNumber == 0 && Math.Abs(mFileInfo.MassResolution - 0) < double.Epsilon && mFileInfo.InstModel == null)
                        {
                            // File actually didn't load correctly, since these shouldn't all be blank
                            blnSuccess = false;
                        }
                        else
                        {
                            blnSuccess = true;
                        }
                    }
                    else
                    {
                        blnSuccess = false;
                    }
                }
                else
                {
                    blnSuccess = false;
                }

            }
            catch (Exception)
            {
                blnSuccess = false;
            }
            finally
            {
                if (!blnSuccess)
                {
                    mCachedFileName = string.Empty;
                }
            }

            return blnSuccess;

        }

        private bool TuneMethodsMatch(udtTuneMethodType udtMethod1, udtTuneMethodType udtMethod2)
        {

            if (udtMethod1.Settings.Count != udtMethod2.Settings.Count)
            {
                // Different segment number of setting count; the methods don't match
                return false;
            }

            for (var intIndex = 0; intIndex <= udtMethod1.Settings.Count - 1; intIndex++)
            {
                if (udtMethod1.Settings[intIndex].Category != udtMethod2.Settings[intIndex].Category ||
                    udtMethod1.Settings[intIndex].Name != udtMethod2.Settings[intIndex].Name ||
                    udtMethod1.Settings[intIndex].Value != udtMethod2.Settings[intIndex].Value)
                {
                    // Different segment data; the methods don't match
                    return false;
                }
            }

            return true;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks></remarks>
        public XRawFileIO()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks></remarks>
        public XRawFileIO(string rawFilePath)
        {
            mCachedScanInfo = new Dictionary<int, clsScanInfo>();

            if (!(string.IsNullOrWhiteSpace(rawFilePath)))
            {
                OpenRawFile(rawFilePath);
            }
        }

        /// <summary>
        /// Dispose the reader
        /// </summary>
        /// <remarks></remarks>
        public void Dispose()
        {
            CloseRawFile();
        }
    }
}
