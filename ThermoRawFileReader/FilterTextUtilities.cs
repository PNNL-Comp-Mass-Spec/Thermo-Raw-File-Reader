﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PRISM.DataUtils;

namespace ThermoRawFileReader
{
    /// <summary>
    /// Methods for parsing scan filter values
    /// </summary>
    public static class FilterTextUtilities
    {
        // Ignore Spelling: A-Za-z, cnl, Exactive, MRM, msx, sa, Ss

        // ReSharper disable once CommentTypo

        // This RegEx matches Full ms2, Full ms3, ..., Full ms10, Full ms11, ...
        // It also matches p ms2 ('p' is not a scan mode, but is present here for compatibility with some old scan filter values)
        // It also matches SRM ms2
        // It also matches CRM ms3
        // It also matches Z ms3 (zoom scan)
        // It also matches Full msx ms2 (multiplexed parent ion selection, introduced with the Q-Exactive)
        // It also matches Full lock ms2 (see Q-Exactive HF dataset 20220226_CsI_4000TO20000_POSTCAL)
        // We're not including SIM, Q1MS, or Q3MS in this RegEx since those are checked for separately (see the MRM_ constants in class XRawFileIO)
        private const string MS2_REGEX = "(?<ScanMode> p|Full|SRM|CRM|Full msx|Full lock|Z) ms(?<MSLevel>[2-9]|[1-9][0-9]) ";

        private const string MASS_LIST_REGEX = @"\[[0-9.]+-[0-9.]+.*\]";

        private const string MASS_RANGES_REGEX = "(?<StartMass>[0-9.]+)-(?<EndMass>[0-9.]+)";

        // This RegEx matches text like 1312.95@45.00 or 756.98@cid35.00 or 902.5721@etd120.55@cid20.00
        // For a filter string of the form "ms3 533.1917@cid35.00 434.9481@hcd55.00", it matches "533.1917@cid35.00"
        private const string PARENT_ION_REGEX = "(?<ParentMZ>[0-9.]+)@(?<CollisionMode1>[a-z]*)(?<CollisionEnergy1>[0-9.]+)(@(?<CollisionMode2>[a-z]+)(?<CollisionEnergy2>[0-9.]+))?";

        // This RegEx is used to extract parent ion m/z from a filter string that does not contain msx
        // ${ParentMZ} will hold the last parent ion m/z found
        // For example, 756.71 in FTMS + p NSI d Full ms3 850.70@cid35.00 756.71@cid35.00 [195.00-2000.00]
        private const string PARENT_ION_ONLY_NON_MSX_REGEX = @"[Mm][Ss]\d*[^\[\r\n]* (?<ParentMZ>[0-9.]+)@?[A-Za-z]*\d*\.?\d*(\[[^\]\r\n]\])?";

        // This RegEx is used to extract parent ion m/z from a filter string that does contain msx
        // ${ParentMZ} will hold the first parent ion m/z found (the first parent ion m/z corresponds to the highest peak)
        // For example, 636.04 in FTMS + p NSI Full msx ms2 636.04@hcd28.00 641.04@hcd28.00 654.05@hcd28.00 [88.00-1355.00]
        private const string PARENT_ION_ONLY_MSX_REGEX = @"[Mm][Ss]\d* (?<ParentMZ>[0-9.]+)@?[A-Za-z]*\d*\.?\d*[^\[\r\n]*(\[[^\]\r\n]+\])?";

        // This RegEx looks for "sa" prior to Full ms"
        private const string SA_REGEX = " sa Full ms";

        private const string MSX_REGEX = " Full msx ";

        private static readonly Regex mFindMS = new(MS2_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mMassList = new(MASS_LIST_REGEX, RegexOptions.Compiled);

        private static readonly Regex mMassRanges = new(MASS_RANGES_REGEX, RegexOptions.Compiled);

        private static readonly Regex mFindParentIon = new(PARENT_ION_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mFindParentIonOnlyNonMsx = new(PARENT_ION_ONLY_NON_MSX_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mFindParentIonOnlyMsx = new(PARENT_ION_ONLY_MSX_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mFindSAFullMS = new(SA_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex mFindFullMSx = new(MSX_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parse out the MRM_QMS or SRM mass info from filterText
        /// </summary>
        /// <remarks>We do not parse mass information out for Full Neutral Loss scans</remarks>
        /// <param name="filterText">Thermo scan filter text</param>>
        /// <param name="mrmScanType">MRM scan type</param>
        /// <param name="mrmInfo">Output: MRM info class</param>
        public static void ExtractMRMMasses(string filterText, MRMScanTypeConstants mrmScanType, out MRMInfo mrmInfo)
        {
            // Parse out the MRM_QMS or SRM mass info from filterText
            // It should be of the form

            // SIM:              p NSI SIM ms [330.00-380.00]
            //                   p NSI SIM msx ms [475.0000-525.0000]
            // or
            // MRM_Q1MS_TEXT:    p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]
            // or
            // MRM_Q3MS_TEXT:    p NSI Q3MS [150.070-1500.000]
            // or
            // MRM_SRM_TEXT:     c NSI SRM ms2 489.270@cid17.00 [397.209-392.211, 579.289-579.291]

            // Note: we do not parse mass information out for Full Neutral Loss scans
            // MRM_FullNL_TEXT: c NSI Full cnl 162.053 [300.000-1200.000]

            mrmInfo = new MRMInfo();

            if (string.IsNullOrWhiteSpace(filterText))
            {
                return;
            }

            if (!(mrmScanType is MRMScanTypeConstants.SIM or MRMScanTypeConstants.MRMQMS or MRMScanTypeConstants.SRM))
            {
                // Unsupported MRM type
                return;
            }

            // Parse out the text between the square brackets
            var massListMatch = mMassList.Match(filterText);

            if (!massListMatch.Success)
            {
                return;
            }

            var massRangeMatch = mMassRanges.Match(massListMatch.Value);

            while (massRangeMatch.Success)
            {
                try
                {
                    // Note that group 0 is the full mass range (two mass values, separated by a dash)
                    // Group 1 is the first mass value
                    // Group 2 is the second mass value

                    var mrmMassRange = new MRMMassRangeType
                    {
                        StartMass = double.Parse(massRangeMatch.Groups["StartMass"].Value),
                        EndMass = double.Parse(massRangeMatch.Groups["EndMass"].Value)
                    };

                    var centralMass = mrmMassRange.StartMass + (mrmMassRange.EndMass - mrmMassRange.StartMass) / 2;
                    mrmMassRange.CentralMass = Math.Round(centralMass, 6);

                    mrmInfo.MRMMassList.Add(mrmMassRange);
                }
                catch (Exception)
                {
                    // Error parsing out the mass values; skip this group
                }

                massRangeMatch = massRangeMatch.NextMatch();
            }
        }

        /// <summary>
        /// Parse out the parent ion from filterText
        /// </summary>
        /// <remarks>
        /// <para>If multiple parent ion m/z values are listed, parentIonMz will have the last one</para>
        /// <para>However, if the filter text contains "Full msx", parentIonMz will have the first parent ion listed</para>
        /// <para>For MS3 spectra with ions listed as 1312.95@45.00 873.85@45.00, the last m/z value listed is the m/z of the first ion that was isolated</para>
        /// <para>For MS3 spectra with ions listed as 377.9027@cid35.00 478.3521@hcd55.00, the first m/z value listed is the m/z of the parent MS2 spectrum</para>
        /// </remarks>
        /// <remarks>
        /// <para>
        /// This method was created for use in other programs that only need the parent ion m/z, and no other functions from ThermoRawFileReader.
        /// Other projects that use this:
        ///   PHRPReader (https://github.com/PNNL-Comp-Mass-Spec/PHRP)
        /// </para>
        /// </remarks>
        /// <param name="filterText">Thermo scan filter text</param>>
        /// <param name="parentIonMz">Output: parent ion m/z</param>
        /// <returns>True if success</returns>
        public static bool ExtractParentIonMzFromFilterText(string filterText, out double parentIonMz)
        {
            Regex matcher;

            if (filterText.IndexOf("msx", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matcher = mFindParentIonOnlyMsx;
            }
            else
            {
                matcher = mFindParentIonOnlyNonMsx;
            }

            var match = matcher.Match(filterText);

            if (match.Success)
            {
                var parentIonMzText = match.Groups["ParentMZ"].Value;

                return double.TryParse(parentIonMzText, out parentIonMz);
            }

            parentIonMz = 0;
            return false;
        }

        /// <summary>
        /// Parse out the parent ion and collision energy from filterText
        /// </summary>
        /// <remarks>
        /// <para>If multiple parent ion m/z values are listed, parentIonMz will have the last one</para>
        /// <para>However, if the filter text contains "Full msx", parentIonMz will have the first parent ion listed</para>
        /// <para>For MS3 spectra with ions listed as 1312.95@45.00 873.85@45.00, the last m/z value listed is the m/z of the first ion that was isolated</para>
        /// <para>For MS3 spectra with ions listed as 377.9027@cid35.00 478.3521@hcd55.00, the first m/z value listed is the m/z of the parent MS2 spectrum</para>
        /// </remarks>
        /// <param name="filterText">Thermo scan filter text</param>>
        /// <param name="parentIonMz">Output: parent ion m/z</param>
        /// <param name="msLevel">Output: MS level (1 for MS1, 2 for MS2, etc.)</param>
        /// <param name="collisionMode">Output: collision mode</param>
        /// <returns>True if success</returns>
        public static bool ExtractParentIonMZFromFilterText(string filterText, out double parentIonMz, out int msLevel, out string collisionMode)
        {
            return ExtractParentIonMZFromFilterText(filterText, out parentIonMz, out msLevel, out collisionMode, out _);
        }

        /// <summary>
        /// Parse out the parent ion and collision energy from filterText
        /// </summary>
        /// <remarks>
        /// <para>If multiple parent ion m/z values are listed, parentIonMz will have the last one</para>
        /// <para>However, if the filter text contains "Full msx", parentIonMz will have the first parent ion listed</para>
        /// <para>For MS3 spectra with ions listed as 1312.95@45.00 873.85@45.00, the last m/z value listed is the m/z of the first ion that was isolated</para>
        /// <para>For MS3 spectra with ions listed as 377.9027@cid35.00 478.3521@hcd55.00, the first m/z value listed is the m/z of the parent MS2 spectrum</para>
        /// </remarks>
        /// <param name="filterText">Thermo scan filter text</param>>
        /// <param name="parentIonMz">Output: parent ion m/z</param>
        /// <param name="msLevel">Output: MS level (1 for MS1, 2 for MS2, etc.)</param>
        /// <param name="collisionMode">Output: collision mode</param>
        /// <param name="parentIons">Output: parent ion list</param>
        /// <returns>True if this is a ms2, ms3, Full ms, Z ms, etc. scan, otherwise false (returns false if ExtractMSLevel returns false)</returns>
        public static bool ExtractParentIonMZFromFilterText(
            string filterText,
            out double parentIonMz,
            out int msLevel,
            out string collisionMode,
            out List<ParentIonInfoType> parentIons)
        {
            // filterText should be similar to one of the following:
            // "+ c d Full ms2 1312.95@45.00 [ 350.00-2000.00]"
            // "+ c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
            // "ITMS + c NSI d Full ms10 421.76@35.00"
            // "ITMS + c NSI d sa Full ms2 467.16@etd100.00 [50.00-1880.00]"              ' Note: sa stands for "supplemental activation"
            // "ITMS + c NSI d Full ms2 467.16@etd100.00 [50.00-1880.00]"
            // "ITMS + c NSI d Full ms2 756.98@cid35.00 [195.00-2000.00]"
            // "ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]"
            // "ITMS + c ESI d Full ms2 342.90@cid35.00 [50.00-2000.00]"
            // "FTMS + p NSI Full ms [400.00-2000.00]"  (high res full MS)
            // "ITMS + c ESI Full ms [300.00-2000.00]"  (low res full MS)
            // "ITMS + p ESI d Z ms [1108.00-1118.00]"  (zoom scan)
            // "+ p ms2 777.00@cid30.00 [210.00-1200.00]
            // "+ c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]
            // "FTMS + p NSI d Full msx ms2 712.85@hcd28.00 407.92@hcd28.00  [100.00-1475.00]"
            // "FTMS + c NSI d Full ms3 533.1917@cid35.00 434.9481@hcd55.00 [110.0000-1054.0000]"
            // "ASTMS + c NSI d Full ms2 0@hcd15.71"
            // "MRTOF + c NSI d Full ms2 0@hcd15.71"
            // "ITMS + c NSI r d sa Full ms2 1073.4800@etd120.55@cid20.00 [120.0000-2000.0000]"
            // "+ c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"

            var bestParentIon = new ParentIonInfoType();
            bestParentIon.Clear();

            msLevel = 1;
            parentIonMz = 0;
            collisionMode = string.Empty;
            var matchFound = false;

            parentIons = new List<ParentIonInfoType>();

            try
            {
                var supplementalActivationEnabled = mFindSAFullMS.IsMatch(filterText);

                var multiplexedMSnEnabled = mFindFullMSx.IsMatch(filterText);

                var success = ExtractMSLevel(filterText, out msLevel, out var mzText);

                if (!success)
                {
                    return false;
                }

                // Use a RegEx to extract out the last parent ion mass listed
                // For example, grab 1312.95 out of "1312.95@45.00 [ 350.00-2000.00]"
                // or, grab 873.85 out of "1312.95@45.00 873.85@45.00 [ 350.00-2000.00]"
                // or, grab 756.98 out of "756.98@etd100.00 [50.00-2000.00]"
                // or, grab 748.371 out of "748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515"
                //
                // However, if using multiplex ms/ms (msx),  we return the first parent ion listed

                // For safety, remove any text after a square bracket
                var bracketIndex = mzText.IndexOf('[');

                if (bracketIndex > 0)
                {
                    // Remove ion ranges enclosed in square brackets
                    mzText = mzText.Substring(0, bracketIndex);
                }

                // Find all the parent ion m/z's present in mzText
                var startIndex = 0;

                do
                {
                    var parentIonMatch = mFindParentIon.Match(mzText, startIndex);

                    if (!parentIonMatch.Success)
                    {
                        // Match not found
                        // If mzText only contains a number, we will parse it out later in this function
                        break;
                    }

                    // Match found

                    parentIonMz = double.Parse(parentIonMatch.Groups["ParentMZ"].Value);

                    matchFound = true;

                    startIndex = parentIonMatch.Index + parentIonMatch.Length;

                    collisionMode = GetCapturedValue(parentIonMatch, "CollisionMode1");

                    var collisionEnergy = GetCapturedValue(parentIonMatch, "CollisionEnergy1");

                    var collisionEnergyValue = StringToValueUtils.CFloatSafe(collisionEnergy, 0);

                    var collisionMode2 = GetCapturedValue(parentIonMatch, "CollisionMode2");

                    float collisionEnergy2Value;

                    if (string.IsNullOrWhiteSpace(collisionMode2))
                    {
                        collisionEnergy2Value = 0;
                    }
                    else
                    {
                        var collisionEnergy2 = GetCapturedValue(parentIonMatch, "CollisionEnergy2");
                        collisionEnergy2Value = StringToValueUtils.CFloatSafe(collisionEnergy2, 0);
                    }

                    var allowSecondaryActivation = true;

                    if (string.Equals(collisionMode, "ETD", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(collisionMode2))
                    {
                        if (string.Equals(collisionMode2, "CID", StringComparison.OrdinalIgnoreCase))
                        {
                            collisionMode = "ETciD";
                            allowSecondaryActivation = false;
                        }
                        else if (string.Equals(collisionMode2, "HCD", StringComparison.OrdinalIgnoreCase))
                        {
                            collisionMode = "EThcD";
                            allowSecondaryActivation = false;
                        }
                    }

                    if (allowSecondaryActivation && !string.IsNullOrWhiteSpace(collisionMode))
                    {
                        if (supplementalActivationEnabled)
                        {
                            collisionMode = "sa_" + collisionMode;
                        }
                    }

                    var parentIonInfo = new ParentIonInfoType
                    {
                        MSLevel = msLevel,
                        ParentIonMZ = parentIonMz,
                        CollisionEnergy = collisionEnergyValue,
                        CollisionEnergy2 = collisionEnergy2Value
                    };

                    if (collisionMode != null)
                        parentIonInfo.CollisionMode = collisionMode;

                    if (collisionMode2 != null)
                        parentIonInfo.CollisionMode2 = collisionMode2;

                    parentIons.Add(parentIonInfo);

                    if (!multiplexedMSnEnabled || parentIons.Count == 1)
                    {
                        bestParentIon = parentIonInfo;
                    }
                } while (startIndex < mzText.Length - 1);

                if (matchFound)
                {
                    // Update the output values using bestParentIon
                    msLevel = bestParentIon.MSLevel;
                    parentIonMz = bestParentIon.ParentIonMZ;
                    collisionMode = bestParentIon.CollisionMode;

                    return true;
                }

                // Match not found using RegEx
                // Use manual text parsing instead

                var atIndex = mzText.LastIndexOf('@');

                if (atIndex > 0)
                {
                    mzText = mzText.Substring(0, atIndex);
                    var spaceIndex = mzText.LastIndexOf(' ');

                    if (spaceIndex > 0)
                    {
                        mzText = mzText.Substring(spaceIndex + 1);
                    }

                    try
                    {
                        parentIonMz = double.Parse(mzText);
                        matchFound = true;
                    }
                    catch (Exception)
                    {
                        parentIonMz = 0;
                    }

                    return matchFound;
                }

                if (mzText.Length == 0)
                    return false;

                // Find the longest contiguous number that mzText starts with

                var charIndex = -1;

                while (charIndex < mzText.Length - 1)
                {
                    if (char.IsNumber(mzText[charIndex + 1]) || mzText[charIndex + 1] == '.')
                    {
                        charIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (charIndex < 0)
                    return false;

                try
                {
                    parentIonMz = double.Parse(mzText.Substring(0, charIndex + 1));
                    matchFound = true;

                    var parentIonMzOnly = new ParentIonInfoType();
                    parentIonMzOnly.Clear();
                    parentIonMzOnly.MSLevel = msLevel;
                    parentIonMzOnly.ParentIonMZ = parentIonMz;

                    parentIons.Add(parentIonMzOnly);
                }
                catch (Exception)
                {
                    parentIonMz = 0;
                }

                return matchFound;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Extract the MS Level from the filter string
        /// </summary>
        /// <remarks>
        /// Looks for "Full ms2" or "Full ms3" or " p ms2" or "SRM ms2" in filterText
        /// Populates msLevel with the number after "ms" and mzText with the text after "ms2"
        /// </remarks>
        /// <param name="filterText">Thermo scan filter text</param>>
        /// <param name="msLevel">Output: MS level (1 for MS1, 2 for MS2, etc.)</param>
        /// <param name="mzText">Output: parent ion m/z</param>
        /// <returns>True if found and False if no match</returns>
        public static bool ExtractMSLevel(string filterText, out int msLevel, out string mzText)
        {
            int matchTextLength;

            var msMatch = mFindMS.Match(filterText);

            int charIndex;

            if (msMatch.Success)
            {
                msLevel = Convert.ToInt32(msMatch.Groups["MSLevel"].Value);
                charIndex = filterText.IndexOf(msMatch.ToString(), StringComparison.OrdinalIgnoreCase);
                matchTextLength = msMatch.Length;
            }
            else
            {
                msLevel = 1;
                charIndex = -1;
                matchTextLength = 0;
            }

            if (charIndex > 0)
            {
                // Copy the text after "Full ms2" or "Full ms3" in filterText to mzText
                mzText = filterText.Substring(charIndex + matchTextLength).Trim();
                return true;
            }

            mzText = string.Empty;
            return false;
        }

        private static string GetCapturedValue(Match match, string captureGroupName)
        {
            var capturedValue = match.Groups[captureGroupName];

            if (!string.IsNullOrWhiteSpace(capturedValue?.Value))
            {
                return capturedValue.Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Return the collision energy (or energies) for the given parent ion(s)
        /// </summary>
        /// <param name="parentIons">Parent ion list</param>
        public static List<double> GetCollisionEnergy(List<ParentIonInfoType> parentIons)
        {
            var collisionEnergies = new List<double>();

            try
            {
                foreach (var parentIon in parentIons)
                {
                    collisionEnergies.Add(parentIon.CollisionEnergy);

                    if (parentIon.CollisionEnergy2 > 0)
                    {
                        // Filter text is of the form: ITMS + c NSI r d sa Full ms2 1143.72@etd120.55@cid20.00 [120.00-2000.00]
                        // Data will be stored as
                        // parentIon.CollisionEnergy = 120.55
                        // parentIon.CollisionEnergy2 = 20.0
                        collisionEnergies.Add(parentIon.CollisionEnergy2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Exception in GetCollisionEnergy (for parent ions): " + ex.Message);
            }

            return collisionEnergies;
        }
    }
}
