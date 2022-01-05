using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PRISM;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.MassPrecisionEstimator;
using ThermoFisher.CommonCore.RawFileReader;
using ThermoFisher.CommonCore.BackgroundSubtraction;
using ThermoFisher.CommonCore.Data.FilterEnums;

[assembly: CLSCompliant(false)]

// The methods in this class use ThermoFisher.CommonCore.RawFileReader.dll
// and related DLLs to extract scan header info and mass spec data (m/z and intensity lists)
// from Thermo .Raw files (LTQ, LTQ-FT, Orbitrap, Exactive, TSQ, etc.)
//
// For more information about the ThermoFisher.CommonCore DLLs,
// see the RawFileReaderLicense.doc file in the solution directory;
// see also http://planetorbitrap.com/rawfilereader#.W5BAoOhKjdM
// For questions, contact Jim Shofstahl at ThermoFisher.com

// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Bryson Gibbons for the Department of Energy (PNNL, Richland, WA)
// Originally used XRawfile2.dll (in November 2004)
// Switched to MSFileReader.XRawfile2.dll in March 2012
// Switched to ThermoFisher.CommonCore DLLs in 2018
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause
//
// Copyright 2018 Battelle Memorial Institute

// ReSharper disable UnusedMember.Global

namespace ThermoRawFileReader
{
    /// <summary>
    /// Class for reading Thermo .raw files
    /// </summary>
    [CLSCompliant(true)]
    public class XRawFileIO : EventNotifier, IDisposable
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: Biofilm, Bryson, centroided, centroiding, cid, cnl, ETciD, EThcD, Jup
        // Ignore Spelling: mrm, msx, multipole, Raptor, sa, Shofstahl, sizeof, Smeagol, struct, Subtractor, Wideband
        // Ignore Spelling: Exactive, Lumos, Orbitrap

        // ReSharper restore CommentTypo

        // Note that each of these strings has a space at the end; this is important to avoid matching inappropriate text in the filter string
        private const string MS_ONLY_C_TEXT = " c ms ";
        private const string MS_ONLY_P_TEXT = " p ms ";

        private const string MS_ONLY_P_NSI_TEXT = " p NSI ms ";
        private const string MS_ONLY_PZ_TEXT = " p Z ms ";          // Likely a zoom scan
        private const string MS_ONLY_DZ_TEXT = " d Z ms ";          // Dependent zoom scan
        private const string MS_ONLY_PZ_MS2_TEXT = " d Z ms2 ";     // Dependent MS2 zoom scan
        private const string MS_ONLY_Z_TEXT = " NSI Z ms ";         // Likely a zoom scan

        private const string FULL_MS_TEXT = "Full ms ";
        private const string FULL_PR_TEXT = "Full pr ";             // TSQ: Full Parent Scan, Product Mass
        private const string SIM_MS_TEXT = "SIM ms ";
        private const string FULL_LOCK_MS_TEXT = "Full lock ms ";   // Lock mass scan

        private const string MRM_Q1MS_TEXT = "Q1MS ";
        private const string MRM_Q3MS_TEXT = "Q3MS ";
        private const string MRM_SRM_TEXT = "SRM ms2";
        private const string MRM_FullNL_TEXT = "Full cnl ";         // MRM neutral loss; yes, cnl starts with a c
        private const string MRM_SIM_PR_TEXT = "SIM pr ";           // TSQ: Isolated and fragmented parent, monitor multiple product ion ranges; e.g., Biofilm-1000pg-std-mix_06Dec14_Smeagol-3
        private const string MRM_SIM_MSX_TEXT = "SIM msx ";         // Q-Exactive Plus: Isolated and fragmented parent, monitor multiple product ion ranges; e.g., MM_unsorted_10ng_digestionTest_t-SIM_MDX_3_17Mar20_Oak_Jup-20-03-01

        private const string ION_MODE_REGEX = "[+-]";

        private const string COLLISION_SPEC_REGEX = "(?<MzValue> [0-9.]+)@";

        private const string MZ_WITHOUT_COLLISION_ENERGY = "ms[2-9](?<MzValue> [0-9.]+)$";

        /// <summary>
        /// Maximum size of the scan info cache
        /// </summary>
        private int mMaxScansToCacheInfo = 50000;

        /// <summary>
        /// The scan info cache
        /// </summary>
        private readonly Dictionary<int, clsScanInfo> mCachedScanInfo = new();

        /// <summary>
        /// This linked list tracks the scan numbers stored in mCachedScanInfo,
        /// allowing for quickly determining the oldest scan added to the cache when the cache limit is reached
        /// </summary>
        private readonly LinkedList<int> mCachedScans = new();

        /// <summary>
        /// Reader that implements ThermoFisher.CommonCore.Data.Interfaces.IRawDataPlus
        /// </summary>
        private IRawDataPlus mXRawFile;

        /// <summary>
        /// Cached file header
        /// </summary>
        private IFileHeader mXRawFileHeader;

        /// <summary>
        /// This is set to true if an exception is raised with the message "memory is corrupt"
        /// It is also set to true if the .raw file does not have any MS data
        /// </summary>
        private bool mCorruptMemoryEncountered;

        private static readonly Regex mIonMode = new(ION_MODE_REGEX, RegexOptions.Compiled);

        private static readonly Regex mCollisionSpecs = new(COLLISION_SPEC_REGEX, RegexOptions.Compiled);

        private static readonly Regex mMzWithoutCE = new(MZ_WITHOUT_COLLISION_ENERGY, RegexOptions.Compiled);

        /// <summary>
        /// File info for the currently loaded .raw file
        /// </summary>
        public RawFileInfo FileInfo { get; } = new();

        /// <summary>
        /// Thermo reader options
        /// </summary>
        public ThermoReaderOptions Options { get; }

        /// <summary>
        /// The full path to the currently loaded .raw file
        /// </summary>
        /// <remarks>This is changed to an empty string once the file is closed</remarks>
        public string RawFilePath { get; private set; }

        /// <summary>
        /// Maximum number of scan metadata cached; defaults to 50000
        /// </summary>
        /// <remarks>Set to 0 to disable caching</remarks>
        public int ScanInfoCacheMaxSize
        {
            get => mMaxScansToCacheInfo;
            set
            {
                mMaxScansToCacheInfo = value;

                if (mMaxScansToCacheInfo <= 0)
                {
                    mMaxScansToCacheInfo = 0;
                }

                if (mCachedScanInfo.Count == 0)
                    return;

                if (mMaxScansToCacheInfo == 0)
                {
                    mCachedScanInfo.Clear();
                    mCachedScans.Clear();
                }
                else
                {
                    RemoveCachedScanInfoOverLimit(mMaxScansToCacheInfo);
                }
            }
        }

        /// <summary>
        /// First scan number in the .Raw file
        /// </summary>
        public int ScanStart => FileInfo.ScanStart;

        /// <summary>
        /// Last scan number in the .Raw file
        /// </summary>
        public int ScanEnd => FileInfo.ScanEnd;

        /// <summary>
        /// When true, additional messages are reported via Debug events
        /// </summary>
        public bool TraceMode { get; set; }

        /// <summary>
        /// Report an error message to the error event handler
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex">Optional exception</param>
        private void RaiseErrorMessage(string message, Exception ex = null)
        {
            OnErrorEvent(message, ex);
        }

        /// <summary>
        /// Report a warning message to the warning event handler
        /// </summary>
        /// <param name="message"></param>
        private void RaiseWarningMessage(string message)
        {
            OnWarningEvent(message);
        }

        private void Options_OptionsUpdatedEvent(object sender)
        {
            UpdateReaderOptions();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public XRawFileIO()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Constructor with an options parameter
        /// </summary>
        /// <param name="options">Thermo reader options</param>
        public XRawFileIO(ThermoReaderOptions options)
            : this(string.Empty, options)
        {
        }

        /// <summary>
        /// Constructor with a file path parameter
        /// </summary>
        /// <param name="rawFilePath">Thermo .raw file to open (empty string to not open a file)</param>
        /// <param name="traceMode">When true, additional messages are reported via Debug events</param>
        public XRawFileIO(string rawFilePath, bool traceMode = false)
            : this(rawFilePath, new ThermoReaderOptions(), traceMode)
        {
        }

        /// <summary>
        /// Constructor with file path, options, and optionally a trace flag
        /// </summary>
        /// <param name="rawFilePath">Thermo .raw file to open (empty string to not open a file)</param>
        /// <param name="options">Thermo reader options</param>
        /// <param name="traceMode">When true, additional messages are reported via Debug events</param>
        public XRawFileIO(string rawFilePath, ThermoReaderOptions options, bool traceMode = false)
        {
            RawFilePath = string.Empty;
            TraceMode = traceMode;
            Options = options;

            Options.OptionsUpdatedEvent += Options_OptionsUpdatedEvent;

            if (!string.IsNullOrWhiteSpace(rawFilePath))
            {
                OpenRawFile(rawFilePath);
            }
        }

        private void CacheScanInfo(int scan, clsScanInfo scanInfo)
        {
            if (ScanInfoCacheMaxSize == 0)
            {
                return;
            }

            if (mCachedScanInfo.ContainsKey(scan))
            {
                // Updating an existing item
                mCachedScanInfo.Remove(scan);
                mCachedScans.Remove(scan);
            }

            RemoveCachedScanInfoOverLimit(mMaxScansToCacheInfo - 1);

            mCachedScanInfo.Add(scan, scanInfo);
            mCachedScans.AddLast(scan);
        }

        private void RemoveCachedScanInfoOverLimit(int limit)
        {
            if (mCachedScanInfo.Count <= limit)
                return;

            // Remove the oldest entry/entries in mCachedScanInfo
            while (mCachedScanInfo.Count > limit)
            {
                var scan = mCachedScans.First();
                mCachedScans.RemoveFirst();

                if (mCachedScanInfo.ContainsKey(scan))
                {
                    mCachedScanInfo.Remove(scan);
                }
            }
        }

        private static string CapitalizeCollisionMode(string collisionMode)
        {
            if (string.Equals(collisionMode, "EThcD", StringComparison.OrdinalIgnoreCase))
            {
                return "EThcD";
            }

            if (string.Equals(collisionMode, "ETciD", StringComparison.OrdinalIgnoreCase))
            {
                return "ETciD";
            }

            return collisionMode.ToUpper();
        }

        /// <summary>
        /// Close the .raw file
        /// </summary>
        public void CloseRawFile()
        {
            try
            {
                mXRawFile?.Dispose();
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
                RawFilePath = string.Empty;
                FileInfo.Clear();
            }
        }

        private static bool ContainsAny(string stringToSearch, IEnumerable<string> itemsToFind, int indexSearchStart = 0)
        {
            return itemsToFind.Any(item => ContainsText(stringToSearch, item, indexSearchStart));
        }

        private static bool ContainsText(string stringToSearch, string textToFind, int indexSearchStart = 0)
        {
            // Note: need to append a space since many of the search keywords end in a space
            return (stringToSearch + " ").IndexOf(textToFind, StringComparison.OrdinalIgnoreCase) >= indexSearchStart;
        }

        /// <summary>
        /// Determines the MRM scan type by parsing the scan filter string
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns>MRM scan type enum</returns>
        public static MRMScanTypeConstants DetermineMRMScanType(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                return MRMScanTypeConstants.NotMRM;
            }

            var mrmQMSTags = new List<string> {
                MRM_Q1MS_TEXT,
                MRM_Q3MS_TEXT
            };

            if (ContainsAny(filterText, mrmQMSTags, 1))
            {
                return MRMScanTypeConstants.MRMQMS;
            }

            if (ContainsText(filterText, MRM_SRM_TEXT, 1))
            {
                return MRMScanTypeConstants.SRM;
            }

            if (ContainsText(filterText, MRM_SIM_PR_TEXT, 1))
            {
                // This is not technically SRM, but the data looks very similar, so we'll track it like SRM data
                return MRMScanTypeConstants.SRM;
            }

            if (ContainsText(filterText, MRM_SIM_MSX_TEXT, 1))
            {
                return MRMScanTypeConstants.SIM;
            }

            if (ContainsText(filterText, MRM_FullNL_TEXT, 1))
            {
                return MRMScanTypeConstants.FullNL;
            }

            if (ContainsText(filterText, SIM_MS_TEXT, 1))
            {
                return MRMScanTypeConstants.SIM;
            }

            return MRMScanTypeConstants.NotMRM;
        }

        /// <summary>
        /// Determine the Ionization mode by parsing the scan filter string
        /// </summary>
        /// <param name="filterText"></param>
        public static IonModeConstants DetermineIonizationMode(string filterText)
        {
            // Determine the ion mode by simply looking for the first + or - sign

            if (string.IsNullOrWhiteSpace(filterText))
            {
                return IonModeConstants.Unknown;
            }

            // For safety, remove any text after a square bracket
            var charIndex = filterText.IndexOf('[');

            Match match;
            if (charIndex > 0)
            {
                match = mIonMode.Match(filterText.Substring(0, charIndex));
            }
            else
            {
                match = mIonMode.Match(filterText);
            }

            if (match.Success)
            {
                return match.Value switch
                {
                    "+" => IonModeConstants.Positive,
                    "-" => IonModeConstants.Negative,
                    _ => IonModeConstants.Unknown
                };
            }

            return IonModeConstants.Unknown;
        }

        /// <summary>
        /// Parse out the MRM_QMS or SRM mass info from filterText
        /// </summary>
        /// <remarks>We do not parse mass information out for Full Neutral Loss scans</remarks>
        /// <param name="filterText"></param>
        /// <param name="mrmScanType"></param>
        /// <param name="mrmInfo">Output: MRM info class</param>
        public static void ExtractMRMMasses(string filterText, MRMScanTypeConstants mrmScanType, out MRMInfo mrmInfo)
        {
            FilterTextUtilities.ExtractMRMMasses(filterText, mrmScanType, out mrmInfo);
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
        /// <param name="filterText"></param>
        /// <param name="parentIonMz">Parent ion m/z (output)</param>
        /// <returns>True if success</returns>
        public static bool ExtractParentIonMZFromFilterText(string filterText, out double parentIonMz)
        {
            return FilterTextUtilities.ExtractParentIonMzFromFilterText(filterText, out parentIonMz);
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
        /// <param name="filterText"></param>
        /// <param name="parentIonMz">Parent ion m/z (output)</param>
        /// <param name="msLevel">MSLevel (output)</param>
        /// <param name="collisionMode">Collision mode (output)</param>
        /// <returns>True if success</returns>
        public static bool ExtractParentIonMZFromFilterText(string filterText, out double parentIonMz, out int msLevel, out string collisionMode)
        {
            return FilterTextUtilities.ExtractParentIonMZFromFilterText(filterText, out parentIonMz, out msLevel, out collisionMode);
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
        /// <param name="filterText"></param>
        /// <param name="parentIonMz">Parent ion m/z (output)</param>
        /// <param name="msLevel">MSLevel (output)</param>
        /// <param name="collisionMode">Collision mode (output)</param>
        /// <param name="parentIons">Output: parent ion list</param>
        /// <returns>True if success</returns>
        public static bool ExtractParentIonMZFromFilterText(
            string filterText,
            out double parentIonMz,
            out int msLevel,
            out string collisionMode,
            out List<ParentIonInfoType> parentIons)
        {
            return FilterTextUtilities.ExtractParentIonMZFromFilterText(filterText, out parentIonMz, out msLevel, out collisionMode, out parentIons);
        }

        /// <summary>
        /// Extract the MS Level from the filter string
        /// </summary>
        /// <remarks>
        /// Looks for "Full ms2" or "Full ms3" or " p ms2" or "SRM ms2" in filterText
        /// Populates msLevel with the number after "ms" and mzText with the text after "ms2"
        /// </remarks>
        /// <param name="filterText"></param>
        /// <param name="msLevel"></param>
        /// <param name="mzText"></param>
        /// <returns>True if found and False if no match</returns>
        public static bool ExtractMSLevel(string filterText, out int msLevel, out string mzText)
        {
            return FilterTextUtilities.ExtractMSLevel(filterText, out msLevel, out mzText);
        }

        /// <summary>
        /// Populate mFileInfo
        /// </summary>
        /// <remarks>Called from OpenRawFile</remarks>
        /// <returns>True if successful, False if an error</returns>
        private bool FillFileInfo()
        {
            try
            {
                if (mXRawFile == null)
                    return false;

                FileInfo.Clear();

                if (TraceMode)
                    OnDebugEvent("Enumerating device data in the file");

                // Discover the devices with data in the .raw file
                foreach (var item in GetDeviceStats())
                {
                    if (item.Value == 0)
                        continue;

                    FileInfo.Devices.Add(item.Key, item.Value);
                }

                if (FileInfo.Devices.Count == 0)
                {
                    RaiseWarningMessage("File does not have data from any devices");
                }

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    FileInfo.CorruptFile = true;
                    return false;
                }

                FileInfo.CreationDate = DateTime.MinValue;
                FileInfo.CreationDate = mXRawFileHeader.CreationDate;

                if (TraceMode)
                    OnDebugEvent("Checking mXRawFile.IsError");

                if (mXRawFile.IsError)
                    return false;

                if (TraceMode)
                    OnDebugEvent("mXRawFile.IsError reports true");

                if (TraceMode)
                    OnDebugEvent("Accessing mXRawFileHeader.WhoCreatedId");

                FileInfo.CreatorID = mXRawFileHeader.WhoCreatedId;

                if (TraceMode)
                    OnDebugEvent("Accessing mXRawFile.GetInstrumentData");

                var instData = mXRawFile.GetInstrumentData();

                FileInfo.InstFlags = instData.Flags;

                FileInfo.InstHardwareVersion = instData.HardwareVersion;

                FileInfo.InstSoftwareVersion = instData.SoftwareVersion;

                FileInfo.InstMethods.Clear();

                if (Options.LoadMSMethodInfo)
                {
                    LoadMethodInfo();
                }

                if (TraceMode)
                    OnDebugEvent("Defining the model, name, description, and serial number");

                FileInfo.InstModel = instData.Model;
                FileInfo.InstName = instData.Name;
                FileInfo.InstrumentDescription = mXRawFileHeader.FileDescription;
                FileInfo.InstSerialNumber = instData.SerialNumber;

                FileInfo.VersionNumber = mXRawFileHeader.Revision;

                if (TraceMode)
                    OnDebugEvent("Accessing mXRawFile.RunHeaderEx");

                var runData = mXRawFile.RunHeaderEx;

                FileInfo.MassResolution = runData.MassResolution;

                FileInfo.ScanStart = runData.FirstSpectrum;
                FileInfo.ScanEnd = runData.LastSpectrum;

                FileInfo.AcquisitionFilename = string.Empty;

                // Note that the following are typically blank
                FileInfo.AcquisitionDate = mXRawFileHeader.CreationDate.ToString(CultureInfo.InvariantCulture);
                //mXRawFile.GetAcquisitionFileName(mFileInfo.AcquisitionFilename); // DEPRECATED
                FileInfo.Comment1 = runData.Comment1;
                FileInfo.Comment2 = runData.Comment2;

                var sampleInfo = mXRawFile.SampleInformation;

                FileInfo.SampleName = sampleInfo.SampleName;
                FileInfo.SampleComment = sampleInfo.Comment;

                if (Options.LoadMSTuneInfo)
                {
                    GetTuneData();
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseErrorMessage("Error: Exception in FillFileInfo: ", ex);
                return false;
            }
        }

        private ActivationTypeConstants GetActivationType(int scan)
        {
            try
            {
                var scanFilter = mXRawFile.GetFilterForScanNumber(scan);
                var reactions = scanFilter.MassCount;

                if (reactions <= 0)
                {
                    var msg = string.Format("Scan {0} has no precursor m/z values; this is unexpected for a MSn scan", scan);
                    RaiseWarningMessage(msg);
                    return ActivationTypeConstants.Unknown;
                }

                var index = reactions - 1;
                if (index > 0 && scanFilter.GetIsMultipleActivation(index))
                {
                    // The last activation is part of a ETciD/EThcD pair
                    index--;
                }

                var activationTypeCode = scanFilter.GetActivation(index);
                ActivationTypeConstants activationType;

                try
                {
                    activationType = (ActivationTypeConstants)(int)activationTypeCode;
                }
                catch
                {
                    activationType = ActivationTypeConstants.Unknown;
                }

                return activationType;
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetActivationType: " + ex.Message;
                RaiseWarningMessage(msg);
                return ActivationTypeConstants.Unknown;
            }
        }

        /// <summary>
        /// Get the list of intensity values, by scan, for the given device
        /// Use this method to retrieve scan-based values for LC devices stored in the .raw file
        /// </summary>
        /// <remarks>
        /// If the scan has multiple intensity values, they are summed
        /// Scans that have no data will still be present in the dictionary, but with an intensity of 0
        /// </remarks>
        /// <param name="deviceType">Device type</param>
        /// <param name="deviceNumber">Device number (1 based)</param>
        /// <param name="scanStart">Start scan, or 0 to use ScanStart</param>
        /// <param name="scanEnd">End scan, or 0 to use ScanEnd</param>
        /// <returns>Dictionary where keys are scan number and values are the intensity for the scan</returns>
        public Dictionary<int, double> GetChromatogramData(Device deviceType, int deviceNumber = 1, int scanStart = 0, int scanEnd = 0)
        {
            var chromatogramData = new Dictionary<int, double>();

            var chromatogramData2D = GetChromatogramData2D(deviceType, deviceNumber, scanStart, scanEnd);

            if (chromatogramData2D.Count == 0)
                return chromatogramData;

            // Return the sum of the intensities for each scan
            // LC data stored as an analog device will typically only have one data value per scan
            foreach (var scanItem in chromatogramData2D)
            {
                if (scanItem.Value.Count == 0)
                {
                    chromatogramData.Add(scanItem.Key, 0);
                }
                else
                {
                    chromatogramData.Add(scanItem.Key, scanItem.Value.Sum());
                }
            }

            return chromatogramData;
        }

        /// <summary>
        /// Get the intensities, by scan, for the given device
        /// </summary>
        /// <remarks>Scans that have no data will still be present in the dictionary, but with an empty list of doubles</remarks>
        /// <param name="deviceType">Device type</param>
        /// <param name="deviceNumber">Device number (1 based)</param>
        /// <param name="scanStart">Start scan, or 0 to use ScanStart</param>
        /// <param name="scanEnd">End scan, or 0 to use ScanEnd</param>
        /// <returns>Dictionary where keys are scan number and values are the list of intensities for that scan</returns>
        public Dictionary<int, List<double>> GetChromatogramData2D(Device deviceType, int deviceNumber = 1, int scanStart = 0, int scanEnd = 0)
        {
            var chromatogramData = new Dictionary<int, List<double>>();

            try
            {
                if (mXRawFile == null)
                    return chromatogramData;

                if (scanStart <= 0)
                    scanStart = ScanStart;

                if (scanEnd <= 0 || scanEnd < ScanStart)
                    scanEnd = ScanEnd;

                var warningMessage = ValidateAndSelectDevice(deviceType, deviceNumber);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    RaiseWarningMessage(warningMessage + "; cannot load chromatogram data");
                    return chromatogramData;
                }

                var lastScanWithData = -1;

                for (var scan = scanStart; scan <= scanEnd; scan++)
                {
                    try
                    {
                        var scanData = mXRawFile.GetSegmentedScanFromScanNumber(scan, null);

                        if (scanData.Intensities == null || scanData.Intensities.Length == 0)
                            continue;

                        if (lastScanWithData >= 0 && lastScanWithData < scan - 1)
                        {
                            // Insert empty lists for the scans that preceded this scan but did not have data
                            for (var scanToAdd = lastScanWithData + 1; scanToAdd < scan; scanToAdd++)
                            {
                                chromatogramData.Add(scanToAdd, new List<double>());
                            }
                        }

                        chromatogramData.Add(scan, scanData.Intensities.ToList());
                        lastScanWithData = scan;
                    }
                    catch (AccessViolationException)
                    {
                        RaiseWarningMessage(string.Format(
                            "Unable to load data for scan {0} in GetChromatogramData2D; possibly a corrupt .Raw file", scan));
                    }
                    catch (Exception ex)
                    {
                        RaiseErrorMessage(
                            string.Format(
                                "Unable to load data for scan {0} in GetChromatogramData2D: {1}; possibly a corrupt .Raw file",
                                scan, ex.Message),
                            ex);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetChromatogramData: " + ex.Message;
                RaiseErrorMessage(msg, ex);
            }

            SetMSController();
            return chromatogramData;
        }

        /// <summary>
        /// Return the collision energy (or energies) for the given scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        public List<double> GetCollisionEnergy(int scan)
        {
            try
            {
                if (mXRawFile == null)
                    return new List<double>();

                GetScanInfo(scan, out var scanInfo);

                ExtractParentIonMZFromFilterText(scanInfo.FilterText, out _, out _, out _, out var parentIons);

                return FilterTextUtilities.GetCollisionEnergy(parentIons);
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetCollisionEnergy (for scan): " + ex.Message;
                RaiseErrorMessage(msg, ex);
                return new List<double>();
            }
        }
        /// <summary>
        /// Return the collision energy (or energies) for the given parent ion(s)
        /// </summary>
        /// <param name="parentIons">Parent ion list</param>
        public List<double> GetCollisionEnergy(List<ParentIonInfoType> parentIons)
        {
            return FilterTextUtilities.GetCollisionEnergy(parentIons);
        }

        /// <summary>
        /// Get the list of scans that have this scan as the parent scan
        /// </summary>
        /// <param name="scanInfo"></param>
        /// <param name="dependentScans"></param>
        /// <returns>True if dependent</returns>
        private bool GetDependentScans(clsScanInfo scanInfo, out List<int> dependentScans)
        {
            dependentScans = new List<int>();

            // Note that .GetScanDependents does not use the second parameter (filterPrecisionDecimals), so the value provided does not matter
            var scanDependents = mXRawFile.GetScanDependents(scanInfo.ScanNumber, 3);

            if (scanDependents?.ScanDependentDetailArray == null || scanDependents.ScanDependentDetailArray.Length == 0)
                return false;

            foreach (var dependentScan in scanDependents.ScanDependentDetailArray.ToList())
            {
                var scanIndex = dependentScan.ScanIndex;

                // For files that start with scan 1, the scan number should be one more than the scan index
                // However, the scan index values returned by mXRawFile.GetScanDependents do not follow this rule
                // Inspect the ParentScan of both scan scanIndex and scan scanIndex+1 to determine which is the true dependent scan of this scan

                var scanNumber = scanIndex + mXRawFile.RunHeaderEx.FirstSpectrum;

                if (scanIndex == scanNumber)
                {
                    dependentScans.Add(scanNumber);
                }
                else
                {
                    if (GetScanInfo(scanIndex, out var infoByIndex, true, false) && infoByIndex.ParentScan == scanInfo.ScanNumber)
                    {
                        dependentScans.Add(infoByIndex.ScanNumber);
                    }
                    else if (GetScanInfo(scanNumber, out var infoByScanNumber, true, false) && infoByScanNumber.ParentScan == scanInfo.ScanNumber)
                    {
                        dependentScans.Add(infoByScanNumber.ScanNumber);
                    }
                }
            }

            return dependentScans.Count > 0;
        }

        /// <summary>
        /// Get the instrument information of the specified device
        /// </summary>
        public DeviceInfo GetDeviceInfo(Device deviceType, int deviceNumber)
        {
            var deviceInfo = new DeviceInfo(deviceType, deviceNumber);

            try
            {
                var warningMessage = ValidateAndSelectDevice(deviceType, deviceNumber);
                if (!string.IsNullOrEmpty(warningMessage))
                {
                    RaiseWarningMessage(warningMessage);
                    return new DeviceInfo(Device.None, 0);
                }

                var instData = mXRawFile.GetInstrumentData();

                deviceInfo.InstrumentName = instData.Name ?? string.Empty;
                deviceInfo.Model = instData.Model ?? string.Empty;
                deviceInfo.SerialNumber = instData.SerialNumber ?? string.Empty;
                deviceInfo.SoftwareVersion = instData.SoftwareVersion ?? string.Empty;
                deviceInfo.Units = instData.Units;

                deviceInfo.AxisLabelX = instData.AxisLabelX ?? string.Empty;
                deviceInfo.AxisLabelY = instData.AxisLabelY ?? string.Empty;
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetDeviceInfo: " + ex.Message;
                RaiseErrorMessage(msg, ex);
            }

            SetMSController();
            return deviceInfo;
        }

        /// <summary>
        /// Get a count of the number of instruments of each device type, as stored in the .raw file
        /// </summary>
        public Dictionary<Device, int> GetDeviceStats()
        {
            var devices = new Dictionary<Device, int>();

            try
            {
                if (mXRawFile == null)
                    return devices;

                foreach (var deviceType in Enum.GetValues(typeof(Device)).Cast<Device>())
                {
                    var countForDevice = mXRawFile.GetInstrumentCountOfType(deviceType);
                    devices.Add(deviceType, countForDevice);
                }
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetDeviceStats: " + ex.Message;
                RaiseErrorMessage(msg, ex);
            }

            return devices;
        }

        /// <summary>
        /// Number of scans in the .raw file
        /// </summary>
        /// <returns>The number of scans, or -1 if an error</returns>
        public int GetNumScans()
        {
            try
            {
                if (mXRawFile == null)
                    return -1;

                var runData = mXRawFile.RunHeaderEx;

                var scanCount = runData.SpectraCount;

                var errorCode = mXRawFile.IsError;

                if (!errorCode)
                {
                    return scanCount;
                }

                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Determine the parent scan for a MS2 or MS3 scan
        /// </summary>
        /// <remarks>
        /// <para>Looks for event "Master Scan Number" (or "Master Scan Number:") in scanInfo.ScanEvents</para>
        /// <para>Uses an alternative search for older .raw files that do not have event "Master Scan Number"</para>
        /// </remarks>
        /// <param name="scanInfo"></param>
        /// <param name="parentScanNumber">Parent scan number, or 0 if an MS1 scan (or unable to determine the parent)</param>
        /// <returns>True if successful, otherwise false</returns>
        private bool GetParentScanNumber(clsScanInfo scanInfo, out int parentScanNumber)
        {
            parentScanNumber = 0;

            try
            {
                var matchFound = int.TryParse(
                    scanInfo.ScanEvents.Find(x => x.Key.StartsWith("Master Scan Number", StringComparison.OrdinalIgnoreCase)).Value ??
                    string.Empty,
                    out parentScanNumber);

                if (matchFound)
                    return true;

                if (scanInfo.MSLevel <= 1)
                    return false;

                // This is an older .raw file that does not have "Master Scan Number"
                // Find the previous scan with an MSLevel one lower than the current one

                var previousScanNumber = scanInfo.ScanNumber - 1;
                var candidateParents = new List<clsScanInfo>();

                while (previousScanNumber >= 1)
                {
                    if (GetScanInfo(previousScanNumber, out var previousScan, false, false))
                    {
                        if (previousScan.MSLevel == scanInfo.MSLevel - 1)
                        {
                            candidateParents.Add(previousScan);
                        }

                        if (previousScan.MSLevel <= 1 || previousScan.MSLevel < scanInfo.MSLevel - 1)
                            break;
                    }

                    previousScanNumber--;
                }

                if (candidateParents.Count == 0)
                {
                    return false;
                }

                if (candidateParents.Count == 1)
                {
                    parentScanNumber = candidateParents[0].ScanNumber;
                    return true;
                }

                // Multiple candidates
                // Find the one with a parent ion that matches one of this scan's parent ions (preferably the first one)

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var parentIon in scanInfo.ParentIons)
                {
                    foreach (var candidateParent in candidateParents.Where(candidateParent => Math.Abs(candidateParent.ParentIonMZ - parentIon.ParentIonMZ) < 0.001))
                    {
                        parentScanNumber = candidateParent.ScanNumber;
                        return true;
                    }
                }

                // No match
                return false;
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetParentScanNumber: " + ex.Message;
                RaiseWarningMessage(msg);
                return false;
            }
        }

        /// <summary>
        /// Get the retention time for the specified scan. Use when searching for scans in a time range.
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="retentionTime">retention time</param>
        /// <returns>True if successful, False if an error</returns>
        public bool GetRetentionTime(int scan, out double retentionTime)
        {
            retentionTime = 0;
            try
            {
                if (mXRawFile == null)
                    return false;

                // Make sure the MS controller is selected
                if (!SetMSController())
                    return false;

                retentionTime = mXRawFile.RetentionTimeFromScanNumber(scan);

                return true;
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetRetentionTime: " + ex.Message;
                RaiseWarningMessage(msg);
                return false;
            }
        }

        /// <summary>
        /// Get the header info for the specified scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="scanInfo">Scan header info class</param>
        /// <returns>True if successful, False if an error</returns>
        public bool GetScanInfo(int scan, out clsScanInfo scanInfo)
        {
            return GetScanInfo(scan, out scanInfo, true, true);
        }

        /// <summary>
        /// Get the header info for the specified scan
        /// </summary>
        /// <param name="scan">Scan number</param>
        /// <param name="scanInfo">Scan header info class</param>
        /// <param name="getParentScan">
        /// When true, determine the parent scan for this scan.
        /// This is used by method GetParentScanNumber to prevent circular references.
        /// </param>
        /// <param name="getDependentScans">
        /// When true, determine the dependent scans for this scan.
        /// This is used by method GetDependentScans to prevent circular references.
        /// </param>
        /// <returns>True if successful, False if an error</returns>
        private bool GetScanInfo(
            int scan,
            out clsScanInfo scanInfo,
            bool getParentScan,
            bool getDependentScans)
        {
            // Check for the scan in the cache
            if (mCachedScanInfo.TryGetValue(scan, out scanInfo))
            {
                return true;
            }

            if (scan < FileInfo.ScanStart)
            {
                scan = FileInfo.ScanStart;
            }
            else if (scan > FileInfo.ScanEnd)
            {
                scan = FileInfo.ScanEnd;
            }

            scanInfo = new clsScanInfo(scan);

            try
            {
                if (mXRawFile == null)
                    return false;

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    CacheScanInfo(scan, scanInfo);
                    return false;
                }

                // Initialize the values that will be populated using GetScanHeaderInfoForScanNum()
                scanInfo.NumPeaks = 0;
                scanInfo.TotalIonCurrent = 0;
                scanInfo.SIMScan = false;
                scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM;
                scanInfo.ZoomScan = false;
                scanInfo.CollisionMode = string.Empty;
                scanInfo.FilterText = string.Empty;
                scanInfo.IonMode = IonModeConstants.Unknown;

                var scanStats = mXRawFile.GetScanStatsForScanNumber(scan);

                scanInfo.NumPeaks = scanStats.PacketCount;
                scanInfo.RetentionTime = scanStats.StartTime;
                scanInfo.LowMass = scanStats.LowMass;
                scanInfo.HighMass = scanStats.HighMass;
                scanInfo.TotalIonCurrent = scanStats.TIC;
                scanInfo.BasePeakMZ = scanStats.BasePeakMass;
                scanInfo.BasePeakIntensity = scanStats.BasePeakIntensity;
                scanInfo.NumChannels = scanStats.NumberOfChannels;
                scanInfo.Frequency = scanStats.Frequency;

                var errorCode = mXRawFile.IsError;

                if (errorCode)
                {
                    CacheScanInfo(scan, scanInfo);
                    return false;
                }

                scanInfo.UniformTime = scanStats.IsUniformTime;

                scanInfo.IsCentroided = scanStats.IsCentroidScan;

                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        // Retrieve the additional parameters for this scan (including Scan Event)
                        var data = mXRawFile.GetTrailerExtraInformation(scan);
                        var arrayCount = data.Length;
                        var scanEventLabels = data.Labels;
                        var scanEventValues = data.Values;

                        if (arrayCount > 0 && scanEventLabels != null && scanEventValues != null)
                        {
                            scanInfo.StoreScanEvents(scanEventLabels, scanEventValues);
                        }
                    }
                }
                catch (AccessViolationException ex)
                {
                    var msg = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(msg);
                }
                catch (Exception ex)
                {
                    var msg = "Warning: Exception calling mXRawFile.GetTrailerExtraForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(msg);

                    if (ex.Message.IndexOf("memory is corrupt", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mCorruptMemoryEncountered = true;
                    }
                }

                scanInfo.EventNumber = 1;

                // Look for the entry in scanInfo.ScanEvents named "Scan Event:"
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

                if (int.TryParse(
                    scanInfo.ScanEvents.Find(x => x.Key.StartsWith("scan event", StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty,
                    out var scanEventNumber))
                {
                    scanInfo.EventNumber = scanEventNumber;
                }

                if (double.TryParse(
                    scanInfo.ScanEvents.Find(x => x.Key.StartsWith("ion injection time (ms)", StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty,
                    out var ionInjectionTime))
                {
                    scanInfo.IonInjectionTime = ionInjectionTime;
                }

                // Lookup the filter text for this scan
                // Parse out the parent ion m/z for fragmentation scans
                var scanFilter = mXRawFile.GetFilterForScanNumber(scan);
                var filterText = scanFilter.ToString();

                scanInfo.FilterText = string.Copy(filterText);

                scanInfo.IsFTMS = scanFilter.MassAnalyzer == MassAnalyzerType.MassAnalyzerFTMS;

                if (string.IsNullOrWhiteSpace(scanInfo.FilterText))
                    scanInfo.FilterText = string.Empty;

                if (scanInfo.EventNumber <= 1)
                {
                    // XRaw periodically mislabels a scan as .EventNumber = 1 when it's really an MS/MS scan; check for this
                    if (ExtractMSLevel(scanInfo.FilterText, out var msLevel, out _))
                    {
                        scanInfo.EventNumber = msLevel;
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
                        // Parse out the parent ion and collision energy from .FilterText
                        if (ExtractParentIonMZFromFilterText(
                                scanInfo.FilterText,
                                out var parentIonMz,
                                out var msLevelMSn,
                                out var collisionMode,
                                out var parentIons))
                        {
                            scanInfo.ParentIonMZ = parentIonMz;
                            scanInfo.CollisionMode = collisionMode;

                            if (msLevelMSn > 2)
                            {
                                scanInfo.MSLevel = msLevelMSn;
                            }

                            // Check whether this is an SRM MS2 scan
                            scanInfo.MRMScanType = DetermineMRMScanType(scanInfo.FilterText);

                            scanInfo.ParentIons.AddRange(parentIons);

                            // Determine the parent scan
                            if (getParentScan && GetParentScanNumber(scanInfo, out var parentScan))
                            {
                                scanInfo.ParentScan = parentScan;
                            }
                        }
                        else
                        {
                            if (ValidateMSScan(scanInfo.FilterText, out var msLevel, out var simScan, out var mrmScanType, out var zoomScan))
                            {
                                // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                                scanInfo.MSLevel = msLevel;
                                scanInfo.SIMScan = simScan;
                                scanInfo.MRMScanType = mrmScanType;
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

                    if (string.IsNullOrWhiteSpace(scanInfo.FilterText))
                    {
                        // FilterText is empty; this indicates a problem with the .Raw file
                        // This is rare, but does happen (see scans 2 and 3 in QC_Shew_08_03_pt5_1_MAXPRO_27Oct08_Raptor_08-01-01.raw)
                        scanInfo.MSLevel = 1;
                        scanInfo.SIMScan = false;
                        scanInfo.MRMScanType = MRMScanTypeConstants.NotMRM;
                    }
                    else
                    {
                        if (ValidateMSScan(scanInfo.FilterText, out var msLevel, out var simScan, out var mrmScanType, out var zoomScan))
                        {
                            // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                            scanInfo.MSLevel = msLevel;
                            scanInfo.SIMScan = simScan;
                            scanInfo.MRMScanType = mrmScanType;
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
                if (scanInfo.MSLevel > 1)
                {
                    scanInfo.ActivationType = GetActivationType(scan);
                }
                else
                {
                    scanInfo.ActivationType = ActivationTypeConstants.CID;
                }

                // Cache the list of dependent scans (if any)
                if (getDependentScans && GetDependentScans(scanInfo, out var dependentScans))
                {
                    scanInfo.DependentScans.AddRange(dependentScans);
                }

                MRMInfo newMRMInfo;

                if (scanInfo.MRMScanType != MRMScanTypeConstants.NotMRM)
                {
                    // Parse out the MRM_QMS or SRM information for this scan
                    ExtractMRMMasses(scanInfo.FilterText, scanInfo.MRMScanType, out newMRMInfo);
                }
                else
                {
                    newMRMInfo = new MRMInfo();
                }

                scanInfo.MRMInfo = newMRMInfo;

                // Retrieve the Status Log for this scan using the following
                // The Status Log includes numerous instrument parameters, including voltages, temperatures, pressures, turbo pump speeds, etc.

                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        var retentionTime = mXRawFile.RetentionTimeFromScanNumber(scan);

                        // Get the status log nearest to a retention time.
                        var statusLogEntry = mXRawFile.GetStatusLogForRetentionTime(retentionTime);

                        var arrayCount = statusLogEntry.Length;
                        var logNames = statusLogEntry.Labels;
                        var logValues = statusLogEntry.Values;

                        if (arrayCount > 0)
                        {
                            scanInfo.StoreStatusLog(logNames, logValues);
                        }
                    }
                }
                catch (AccessViolationException ex)
                {
                    var msg = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(msg);
                }
                catch (Exception ex)
                {
                    var msg = "Warning: Exception calling mXRawFile.GetStatusLogForScanNum for scan " + scan + ": " + ex.Message;
                    RaiseWarningMessage(msg);

                    if (ex.Message.IndexOf("memory is corrupt", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mCorruptMemoryEncountered = true;
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetScanInfo: " + ex.Message;
                RaiseWarningMessage(msg);
                CacheScanInfo(scan, scanInfo);
                return false;
            }

            if (getParentScan && getDependentScans)
            {
                CacheScanInfo(scan, scanInfo);
            }

            return true;
        }

        /// <summary>
        /// Parse the scan type name out of the scan filter string
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns>Scan type name, e.g. HMS or HCD-HMSn</returns>
        public static string GetScanTypeNameFromThermoScanFilterText(string filterText)
        {
            // Examines filterText to determine what the scan type is
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

            const string defaultScanTypeName = "MS";

            try
            {
                var validScanFilter = true;
                var collisionMode = string.Empty;
                MRMScanTypeConstants mrmScanType;
                var simScan = false;
                var zoomScan = false;

                if (string.IsNullOrWhiteSpace(filterText))
                {
                    return defaultScanTypeName;
                }

                if (!ExtractMSLevel(filterText, out var msLevel, out _))
                {
                    // Assume this is an MS scan
                    msLevel = 1;
                }

                if (msLevel > 1)
                {
                    // Parse out the parent ion and collision energy from filterText

                    if (ExtractParentIonMZFromFilterText(filterText, out _, out msLevel, out collisionMode))
                    {
                        // Check whether this is an SRM MS2 scan
                        mrmScanType = DetermineMRMScanType(filterText);
                    }
                    else
                    {
                        // Could not find "Full ms2" in filterText
                        // XRaw periodically mislabels a scan as .EventNumber > 1 when it's really an MS scan; check for this
                        if (ValidateMSScan(filterText, out msLevel, out simScan, out mrmScanType, out zoomScan))
                        {
                            // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                        }
                        else
                        {
                            // Unknown format for filterText; return an error
                            validScanFilter = false;
                        }
                    }
                }
                else
                {
                    // MSLevel is 1
                    // Make sure .FilterText contains one of the known MS1, SIM or MRM tags
                    if (ValidateMSScan(filterText, out msLevel, out simScan, out mrmScanType, out zoomScan))
                    {
                        // Yes, scan is an MS, SIM, or MRMQMS, or SRM scan
                    }
                    else
                    {
                        // Unknown format for filterText; return an error
                        validScanFilter = false;
                    }
                }

                if (!validScanFilter)
                {
                    return defaultScanTypeName;
                }

                if (mrmScanType == MRMScanTypeConstants.NotMRM ||
                    mrmScanType == MRMScanTypeConstants.SIM)
                {
                    if (simScan)
                    {
                        return SIM_MS_TEXT.Trim();
                    }

                    if (zoomScan)
                    {
                        return "Zoom-MS";
                    }

                    // This is a standard MS or MSn scan

                    var baseScanTypeName = msLevel > 1 ? "MSn" : "MS";

                    string scanTypeName;
                    if (ScanIsFTMS(filterText))
                    {
                        // HMS or HMSn scan
                        scanTypeName = "H" + baseScanTypeName;
                    }
                    else
                    {
                        scanTypeName = baseScanTypeName;
                    }

                    if (msLevel > 1 && collisionMode.Length > 0)
                    {
                        return CapitalizeCollisionMode(collisionMode) + "-" + scanTypeName;
                    }

                    return scanTypeName;
                }

                // This is an MRM or SRM scan
                switch (mrmScanType)
                {
                    case MRMScanTypeConstants.MRMQMS:
                        if (ContainsText(filterText, MRM_Q1MS_TEXT, 1))
                        {
                            return MRM_Q1MS_TEXT.Trim();
                        }
                        else if (ContainsText(filterText, MRM_Q3MS_TEXT, 1))
                        {
                            return MRM_Q3MS_TEXT.Trim();
                        }
                        else
                        {
                            // Unknown QMS mode
                            return "MRM QMS";
                        }

                    case MRMScanTypeConstants.SRM:
                        if (collisionMode.Length > 0)
                        {
                            return collisionMode.ToUpper() + "-SRM";
                        }
                        else
                        {
                            return "CID-SRM";
                        }

                    case MRMScanTypeConstants.FullNL:
                        return "MRM_Full_NL";

                    default:
                        return "MRM";
                }
            }
            catch (Exception)
            {
                // Ignore errors here
            }

            return defaultScanTypeName;
        }

        private void GetTuneData()
        {
            var numTuneData = mXRawFile.GetTuneDataCount();

            for (var index = 0; index <= numTuneData - 1; index++)
            {
                var tuneLabelCount = 0;
                string[] tuneSettingNames = null;
                string[] tuneSettingValues = null;

                try
                {
                    if (!mCorruptMemoryEncountered)
                    {
                        var tuneData = mXRawFile.GetTuneData(index);
                        tuneSettingNames = tuneData.Labels;
                        tuneSettingValues = tuneData.Values;
                        tuneLabelCount = tuneData.Length;
                    }
                }
                catch (AccessViolationException)
                {
                    RaiseWarningMessage("Unable to load tune data; possibly a corrupt .Raw file");
                    break;
                }
                catch (Exception ex)
                {
                    // Exception getting TuneData
                    RaiseWarningMessage(string.Format(
                        "Warning: Exception calling mXRawFile.GetTuneData for Index {0}: {1}", index, ex.Message));

                    tuneLabelCount = 0;

                    if (ex.Message.IndexOf("memory is corrupt", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mCorruptMemoryEncountered = true;
                        break;
                    }
                }

                if (tuneLabelCount > 0)
                {
                    string msg;

                    if (tuneSettingNames == null)
                    {
                        // .GetTuneData returned a non-zero count, but no parameter names; unable to continue
                        msg = "Warning: the GetTuneData function returned a positive tune parameter count but no parameter names";
                    }
                    else if (tuneSettingValues == null)
                    {
                        // .GetTuneData returned parameter names, but tuneSettingValues is nothing; unable to continue
                        msg = "Warning: the GetTuneData function returned tune parameter names but no tune values";
                    }
                    else
                    {
                        msg = string.Empty;
                    }

                    if (msg.Length > 0)
                    {
                        RaiseWarningMessage(string.Format("{0} (Tune Method {1})", msg, index + 1));
                        tuneLabelCount = 0;
                    }
                }

                if (tuneLabelCount <= 0 || tuneSettingNames == null || tuneSettingValues == null)
                {
                    continue;
                }

                var newTuneMethod = new TuneMethod();

                // Step through the names and store in the .Setting() arrays
                var tuneCategory = "General";
                for (var settingIndex = 0; settingIndex <= tuneLabelCount - 1; settingIndex++)
                {
                    if (tuneSettingValues[settingIndex].Length == 0 && !tuneSettingNames[settingIndex].EndsWith(":"))
                    {
                        // New category
                        if (tuneSettingNames[settingIndex].Length > 0)
                        {
                            tuneCategory = string.Copy(tuneSettingNames[settingIndex]);
                        }
                        else
                        {
                            tuneCategory = "General";
                        }
                    }
                    else
                    {
                        var tuneMethodSetting = new TuneMethodSettingType
                        {
                            Category = string.Copy(tuneCategory),
                            Name = tuneSettingNames[settingIndex].TrimEnd(':'),
                            Value = string.Copy(tuneSettingValues[settingIndex])
                        };

                        newTuneMethod.Settings.Add(tuneMethodSetting);
                    }
                }

                if (FileInfo.TuneMethods.Count == 0)
                {
                    FileInfo.TuneMethods.Add(newTuneMethod);
                }
                else
                {
                    // Compare this tune method to the previous one; if identical, don't keep it
                    if (!TuneMethodsMatch(FileInfo.TuneMethods.Last(), newTuneMethod))
                    {
                        FileInfo.TuneMethods.Add(newTuneMethod);
                    }
                }
            }
        }

        private void LoadMethodInfo()
        {
            if (TraceMode)
                OnDebugEvent("Accessing mXRawFile.InstrumentMethodsCount");
            try
            {
                var methodCount = mXRawFile.InstrumentMethodsCount;

                if (TraceMode)
                    OnDebugEvent("File has {0} methods", methodCount);

                for (var methodIndex = 0; methodIndex < methodCount; methodIndex++)
                {
                    if (TraceMode)
                        OnDebugEvent("Retrieving method from index " + methodIndex);

                    var methodText = mXRawFile.GetInstrumentMethod(methodIndex);
                    if (!string.IsNullOrWhiteSpace(methodText))
                    {
                        FileInfo.InstMethods.Add(methodText);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Path.DirectorySeparatorChar == '/')
                {
                    RaiseWarningMessage("Error while reading the method info: " + ex.Message);
                }
                else
                {
                    RaiseErrorMessage("Error while reading the method info: " + ex.Message);
                }

                RaiseWarningMessage("Consider instantiating the XRawFileIO class with a ThermoReaderOptions object that has LoadMSMethodInfo = false");
            }
        }

        /// <summary>
        /// Remove scan-specific data from a scan filter string; primarily removes the parent ion m/z and the scan m/z range
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns>Generic filter text, e.g. FTMS + p NSI Full ms</returns>
        public static string MakeGenericThermoScanFilter(string filterText)
        {
            // Will make a generic version of the FilterText in filterText
            // Examples:
            // From                                                                 To
            // ITMS + c ESI Full ms [300.00-2000.00]                                ITMS + c ESI Full ms
            // FTMS + p NSI Full ms [400.00-2000.00]                                FTMS + p NSI Full ms
            // ITMS + p ESI d Z ms [579.00-589.00]                                  ITMS + p ESI d Z ms
            // ITMS + c ESI d Full ms2 583.26@cid35.00 [150.00-1180.00]             ITMS + c ESI d Full ms2 0@cid35.00
            // ITMS + c NSI d Full ms2 606.30@pqd27.00 [50.00-2000.00]              ITMS + c NSI d Full ms2 0@pqd27.00
            // FTMS + c NSI d Full ms2 516.03@hcd40.00 [100.00-2000.00]             FTMS + c NSI d Full ms2 0@hcd40.00
            // ITMS + c NSI d sa Full ms2 516.03@etd100.00 [50.00-2000.00]          ITMS + c NSI d sa Full ms2 0@etd100.00

            // FTMS + p NSI SIM msx ms [475.0000-525.0000]                          FTMS + p NSI SIM msx ms

            // + c d Full ms2 1312.95@45.00 [ 350.00-2000.00]                       + c d Full ms2 0@45.00
            // + c d Full ms3 1312.95@45.00 873.85@45.00 [ 350.00-2000.00]          + c d Full ms3 0@45.00 0@45.00
            // ITMS + c NSI d Full ms10 421.76@35.00                                ITMS + c NSI d Full ms10 0@35.00

            // + p ms2 777.00@cid30.00 [210.00-1200.00]                             + p ms2 0@cid30.00
            // + c NSI SRM ms2 501.560@cid15.00 [507.259-507.261, 635-319-635.32]   + c NSI SRM ms2 0@cid15.00
            // + c NSI SRM ms2 748.371 [701.368-701.370, 773.402-773.404, 887.484-887.486, 975.513-975.515]    + c NSI SRM ms2
            // + p NSI Q1MS [179.652-184.582, 505.778-510.708, 994.968-999.898]     + p NSI Q1MS
            // + p NSI Q3MS [150.070-1500.000]                                      + p NSI Q3MS
            // c NSI Full cnl 162.053 [300.000-1200.000]                            c NSI Full cnl

            const string defaultGenericScanFilterText = "MS";

            try
            {
                if (string.IsNullOrWhiteSpace(filterText))
                {
                    return defaultGenericScanFilterText;
                }

                string genericScanFilterText;

                // First look for and remove numbers between square brackets
                var bracketIndex = filterText.IndexOf('[');
                if (bracketIndex > 0)
                {
                    genericScanFilterText = filterText.Substring(0, bracketIndex).TrimEnd(' ');
                }
                else
                {
                    genericScanFilterText = filterText.TrimEnd(' ');
                }

                var fullCnlCharIndex = genericScanFilterText.IndexOf(MRM_FullNL_TEXT, StringComparison.OrdinalIgnoreCase);
                if (fullCnlCharIndex > 0)
                {
                    // MRM neutral loss
                    // Remove any text after MRM_FullNL_TEXT
                    return genericScanFilterText.Substring(0, fullCnlCharIndex + MRM_FullNL_TEXT.Length).Trim();
                }

                // Replace any digits before any @ sign with a 0
                if (genericScanFilterText.IndexOf('@') > 0)
                {
                    return mCollisionSpecs.Replace(genericScanFilterText, " 0@");
                }

                // No @ sign; look for text of the form "ms2 748.371"
                var match = mMzWithoutCE.Match(genericScanFilterText);
                if (match.Success)
                {
                    return genericScanFilterText.Substring(0, match.Groups["MzValue"].Index);
                }

                return genericScanFilterText;
            }
            catch (Exception)
            {
                // Ignore errors
            }

            return defaultGenericScanFilterText;
        }

        private static bool ScanIsFTMS(string filterText)
        {
            return ContainsText(filterText, "FTMS");
        }

        private bool SetMSController()
        {
            mXRawFile.SelectInstrument(Device.MS, 1);
            var hasMsData = mXRawFile.SelectMsData();

            if (!hasMsData)
            {
                // Either the file is corrupt, or it simply doesn't have Mass Spec data
                // The ThermoRawFileReader is primarily intended for
                mCorruptMemoryEncountered = true;
            }

            return hasMsData;
        }

        /// <summary>
        /// Examines filterText to validate that it is a supported MS1 scan type (MS, SIM, or MRMQMS, or SRM scan)
        /// </summary>
        /// <remarks>Returns false for MSn scans (like ms2 or ms3)</remarks>
        /// <param name="filterText"></param>
        /// <param name="msLevel"></param>
        /// <param name="simScan">True if mrmScanType is SIM or MRMQMS</param>
        /// <param name="mrmScanType"></param>
        /// <param name="zoomScan"></param>
        /// <returns>True if filterText contains a known MS scan type</returns>
        public static bool ValidateMSScan(
            string filterText,
            out int msLevel,
            out bool simScan,
            out MRMScanTypeConstants mrmScanType,
            out bool zoomScan)
        {
            simScan = false;
            mrmScanType = MRMScanTypeConstants.NotMRM;
            zoomScan = false;

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

            if (ContainsAny(filterText, ms1Tags, 1))
            {
                // This is a Full MS scan
                msLevel = 1;
                return true;
            }

            if (ContainsAny(filterText, zoomTags, 1))
            {
                msLevel = 1;
                zoomScan = true;
                return true;
            }

            if (ContainsText(filterText, MS_ONLY_PZ_MS2_TEXT, 1))
            {
                // Technically, this should have MSLevel = 2, but that would cause a bunch of problems elsewhere in MASIC
                // Thus, we'll pretend it's MS1
                msLevel = 1;
                zoomScan = true;
                return true;
            }

            mrmScanType = DetermineMRMScanType(filterText);
            switch (mrmScanType)
            {
                case MRMScanTypeConstants.SIM:
                    // Selected ion monitoring, which is MS1 of a narrow m/z range
                    msLevel = 1;
                    simScan = true;
                    return true;

                case MRMScanTypeConstants.MRMQMS:
                    // Multiple SIM ranges in a single scan
                    msLevel = 1;
                    simScan = true;
                    return true;

                case MRMScanTypeConstants.SRM:
                    msLevel = 2;
                    return true;

                case MRMScanTypeConstants.FullNL:
                    msLevel = 2;
                    return true;

                default:
                    ExtractMSLevel(filterText, out msLevel, out _);
                    return false;
            }
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative), returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scanNumber">Scan number</param>
        /// <param name="mzList">Output array of mass values</param>
        /// <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData(int scanNumber, out double[] mzList, out double[] intensityList)
        {
            const int maxNumberOfPeaks = 0;
            const bool centroidData = false;
            return GetScanData(scanNumber, out mzList, out intensityList, maxNumberOfPeaks, centroidData);
        }

        /// <summary>
        /// Obtain the mass and intensity list for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative), returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scanNumber">Scan number</param>
        /// <param name="mzList">Output array of mass values</param>
        /// <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        /// <param name="maxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData(int scanNumber, out double[] mzList, out double[] intensityList, int maxNumberOfPeaks)
        {
            const bool centroid = false;
            return GetScanData(scanNumber, out mzList, out intensityList, maxNumberOfPeaks, centroid);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative),  returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scan">Scan number</param>
        /// <param name="mzList">Output array of mass values</param>
        /// <param name="intensityList">Output array of intensity values (parallel to mzList)</param>
        /// <param name="maxNumberOfPeaks">Set to 0 (or negative) to return all of the data</param>
        /// <param name="centroidData">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData(int scan, out double[] mzList, out double[] intensityList, int maxNumberOfPeaks, bool centroidData)
        {
            int dataCount;

            try
            {
                var data = ReadScanData(scan, maxNumberOfPeaks, centroidData);
                dataCount = data.Masses.Length;
                if (dataCount <= 0)
                {
                    mzList = Array.Empty<double>();
                    intensityList = Array.Empty<double>();
                    return 0;
                }

                mzList = data.Masses;
                intensityList = data.Intensities;

                return dataCount;
            }
            catch
            {
                mzList = Array.Empty<double>();
                intensityList = Array.Empty<double>();
                dataCount = 0;

                RaiseWarningMessage(string.Format(
                    "Unable to load data for scan {0} in GetScanData; possibly a corrupt .Raw file", scan));
            }

            return dataCount;
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative), returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scan"></param>
        /// <param name="massIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData2D(int scan, out double[,] massIntensityPairs)
        {
            return GetScanData2D(scan, out massIntensityPairs, maxNumberOfPeaks: 0, centroidData: false);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative), returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scan"></param>
        /// <param name="massIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <param name="maxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData2D(int scan, out double[,] massIntensityPairs, int maxNumberOfPeaks)
        {
            return GetScanData2D(scan, out massIntensityPairs, maxNumberOfPeaks, centroidData: false);
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <remarks>If maxNumberOfPeaks is 0 (or negative), returns all data; set maxNumberOfPeaks to > 0 to limit the number of data points returned</remarks>
        /// <param name="scan"></param>
        /// <param name="massIntensityPairs">2D array where the first dimension is 0 for mass or 1 for intensity while the second dimension is the data point index</param>
        /// <param name="maxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <param name="centroidData">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanData2D(int scan, out double[,] massIntensityPairs, int maxNumberOfPeaks, bool centroidData)
        {
            try
            {
                var data = ReadScanData(scan, maxNumberOfPeaks, centroidData);
                var dataCount = data.Masses.Length;
                if (dataCount <= 0)
                {
                    massIntensityPairs = new double[0, 0];
                    return 0;
                }

                massIntensityPairs = new double[2, dataCount];

                /*
                // A more "black magic" version of doing the below array copy:
                Buffer.BlockCopy(data.Masses, 0, massIntensityPairs, 0, dataCount * sizeof(double));
                Buffer.BlockCopy(data.Intensities, 0, massIntensityPairs, dataCount * sizeof(double), dataCount * sizeof(double));
                 */

                for (var i = 0; i < dataCount; i++)
                {
                    // m/z
                    massIntensityPairs[0, i] = data.Masses[i];
                    // Intensity
                    massIntensityPairs[1, i] = data.Intensities[i];
                }

                return dataCount;
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(
                    string.Format(
                        "Unable to load data for scan {0} in GetScanData2D: {1}; possibly a corrupt .Raw file",
                        scan, ex.Message),
                    ex);
            }

            massIntensityPairs = new double[0, 0];
            return 0;
        }

        /// <summary>
        /// Obtain the mass and intensity for the specified scan
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="maxNumberOfPeaks">Maximum number of data points; 0 to return all data</param>
        /// <param name="centroidData">True to centroid the data, false to return as-is (either profile or centroid, depending on how the data was acquired)</param>
        /// <returns>The scan data container, or null if an error</returns>
        private ISimpleScanAccess ReadScanData(int scan, int maxNumberOfPeaks, bool centroidData)
        {
            if (scan < FileInfo.ScanStart)
            {
                scan = FileInfo.ScanStart;
            }
            else if (scan > FileInfo.ScanEnd)
            {
                scan = FileInfo.ScanEnd;
            }

            if (!GetScanInfo(scan, out var scanInfo))
            {
                throw new Exception(string.Format(
                    "Cannot retrieve ScanInfo from cache for scan {0} in ReadScanData; cannot retrieve scan data", scan));
            }

            try
            {
                if (mXRawFile == null)
                {
                    return null;
                }

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    return null;
                }

                ISimpleScanAccess data;

                // If the scan is already centroided, Scan.FromFile and Scan.ToCentroid either won't work,
                // or will cause centroided data to be re-centroided.
                // Thus, use GetSegmentedScanFromScanNumber if centroidData is false or if .IsCentroided is true

                if (centroidData && !scanInfo.IsCentroided)
                {
                    // Dataset was acquired on an Orbitrap, Exactive, or FTMS instrument, and the scan type
                    // includes additional peak information by default, including centroids: fastest access

                    // Option 1: data = mXRawFile.GetSimplifiedCentroids(scan);
                    //           This internally calls the same function as GetCentroidStream, and then copies data to new arrays.
                    // Option 2: Directly call GetCentroidStream

                    data = mXRawFile.GetCentroidStream(scan, false);

                    if (data?.Masses == null || data.Masses.Length == 0)
                    {
                        // Centroiding for profile-mode ion trap data, or for other scan types that don't include a centroid stream
                        var scanProf = Scan.FromFile(mXRawFile, scan);
                        var centroided = Scan.ToCentroid(scanProf);
                        data = new SimpleScanAccessTruncated(centroided.PreferredMasses, centroided.PreferredIntensities);
                    }
                }
                else
                {
                    // Option 1: var scanData = mXRawFile.GetSimplifiedScan(scan);
                    //           This internally calls the same function as GetSegmentedScanFromScanNumber, and then copies data to new arrays.
                    // Option 2: Directly call GetSegmentedScanFromScanNumber

                    var scanData = mXRawFile.GetSegmentedScanFromScanNumber(scan, null);
                    data = new SimpleScanAccessTruncated(scanData.Positions, scanData.Intensities);
                }

                if (maxNumberOfPeaks > 0)
                {
                    // Takes the maxNumberOfPeaks highest intensities from scan, and sorts them (and their respective mass) by mass into the first maxNumberOfPeaks positions in the arrays.
                    var sortCount = Math.Min(maxNumberOfPeaks, data.Masses.Length);
                    Array.Sort(data.Intensities, data.Masses);
                    Array.Reverse(data.Intensities);
                    Array.Reverse(data.Masses);
                    Array.Sort(data.Masses, data.Intensities, 0, sortCount);

                    var masses = new double[sortCount];
                    var intensities = new double[sortCount];
                    Array.Copy(data.Masses, masses, sortCount);
                    Array.Copy(data.Intensities, intensities, sortCount);
                    data = new SimpleScanAccessTruncated(masses, intensities);
                }
                // ReSharper disable once RedundantIfElseBlock
                else
                {

                    // Although the data returned by mXRawFile.GetMassListFromScanNum is generally sorted by m/z,
                    // we have observed a few cases in certain scans of certain datasets that points with
                    // similar m/z values are swapped and thus slightly out of order

                    // Prior to September 2018, we assured the data was sorted using Array.Sort(data.Masses, data.Intensities);
                    // However, we now leave the data as-is for efficiency purposes

                    // ReSharper disable CommentTypo

                    // If the calling application requires that the data be sorted by m/z, it will need to verify the sort
                    // DeconTools does this in DeconTools.Backend.Runs.XCaliburRun2.GetMassSpectrum

                    // ReSharper restore CommentTypo

                }

                return data;
            }
            catch (AccessViolationException)
            {
                RaiseWarningMessage(string.Format(
                    "Unable to load data for scan {0} in ReadScanData; possibly a corrupt .Raw file", scan));
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(
                    string.Format(
                        "Unable to load data for scan {0} in ReadScanData: {1}; possibly a corrupt .Raw file",
                        scan, ex.Message),
                    ex);
            }

            return null;
        }

        private class SimpleScanAccessTruncated : ISimpleScanAccess
        {
            public double[] Masses { get; }
            public double[] Intensities { get; }

            public SimpleScanAccessTruncated(double[] masses, double[] intensities)
            {
                Masses = masses;
                Intensities = intensities;
            }
        }

        /// <summary>
        /// Get the MSLevel (aka MS order) for a given scan
        /// </summary>
        /// <remarks>
        /// MS1 spectra will return 1, MS2 spectra will return 2, etc.
        /// Other, specialized scan types:
        ///   Neutral gain:   -3
        ///   Neutral loss:   -2
        ///   Parent scan:    -1
        /// </remarks>
        /// <param name="scan"></param>
        /// <returns>The MSOrder, or 0 if an error</returns>
        public int GetMSLevel(int scan)
        {
            try
            {
                if (mXRawFile == null)
                    return 0;

                // Make sure the MS controller is selected
                if (!SetMSController())
                    return 0;

                var scanFilter = mXRawFile.GetFilterForScanNumber(scan);

                var msOrder = scanFilter.MSOrder;

                return (int)msOrder;
            }
            catch (Exception ex)
            {
                var msg = "Unable to determine the MS Level for scan " + scan + ": " + ex.Message + "; possibly a corrupt .Raw file";
                RaiseErrorMessage(msg, ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets the scan label data for an FTMS-tagged scan (from an Orbitrap, Q-Exactive, Lumos, or any other high resolution instrument)
        /// </summary>
        /// <remarks>
        /// Output parameter ftLabelData will have the centroided spectrum, where each data point includes mass (m/z), intensity, and charge
        /// </remarks>
        /// <param name="scan">Scan number</param>
        /// <param name="ftLabelData">List of mass (m/z), intensity, resolution, baseline intensity, noise floor, and charge for each data point</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanLabelData(int scan, out FTLabelInfoType[] ftLabelData)
        {
            if (scan < FileInfo.ScanStart)
            {
                scan = FileInfo.ScanStart;
            }
            else if (scan > FileInfo.ScanEnd)
            {
                scan = FileInfo.ScanEnd;
            }

            if (!GetScanInfo(scan, out var scanInfo))
            {
                throw new Exception(string.Format(
                    "Cannot retrieve ScanInfo from cache for scan {0} in GetScanLabelData; cannot retrieve scan data", scan));
            }

            try
            {
                if (mXRawFile == null)
                {
                    ftLabelData = Array.Empty<FTLabelInfoType>();
                    return -1;
                }

                if (!scanInfo.IsFTMS)
                {
                    var msg = "Scan " + scan + " is not an FTMS scan; function GetScanLabelData cannot be used with this scan";
                    RaiseWarningMessage(msg);
                    ftLabelData = Array.Empty<FTLabelInfoType>();
                    return -1;
                }

                var data = mXRawFile.GetCentroidStream(scan, false);

                var dataCount = data.Length;

                if (dataCount > 0)
                {
                    ftLabelData = new FTLabelInfoType[dataCount];

                    var masses = data.Masses;
                    var intensities = data.Intensities;
                    var resolutions = data.Resolutions;
                    var baselines = data.Baselines;
                    var noises = data.Noises;
                    var charges = data.Charges;

                    for (var i = 0; i <= dataCount - 1; i++)
                    {
                        ftLabelData[i] = new FTLabelInfoType
                        {
                            Mass = masses[i],
                            Intensity = intensities[i],
                            Resolution = Convert.ToSingle(resolutions[i]),
                            Baseline = Convert.ToSingle(baselines[i]),
                            Noise = Convert.ToSingle(noises[i]),
                            Charge = Convert.ToInt32(charges[i])
                        };
                    }
                }
                else
                {
                    ftLabelData = Array.Empty<FTLabelInfoType>();
                }

                return dataCount;
            }
            catch (AccessViolationException)
            {
                RaiseWarningMessage(string.Format(
                    "Unable to load data for scan {0} in GetScanLabelData; possibly a corrupt .Raw file", scan));
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(
                    string.Format(
                        "Unable to load data for scan {0} in GetScanLabelData: {1}; possibly a corrupt .Raw file",
                        scan, ex.Message),
                    ex);
            }

            ftLabelData = Array.Empty<FTLabelInfoType>();
            return -1;
        }

        /// <summary>
        /// Gets scan precision data for FTMS data (resolution of each data point)
        /// </summary>
        /// <remarks>
        /// This returns the same data that GetScanLabelData returns, but with AccuracyMMU and AccuracyPPM instead of Baseline, Noise, and Charge
        /// </remarks>
        /// <param name="scan"></param>
        /// <param name="massResolutionData">List of intensity, mass (m/z), accuracy (MMU), accuracy (PPM), and resolution for each data point</param>
        /// <returns>The number of data points, or -1 if an error</returns>
        public int GetScanPrecisionData(int scan, out MassPrecisionInfoType[] massResolutionData)
        {
            if (scan < FileInfo.ScanStart)
            {
                scan = FileInfo.ScanStart;
            }
            else if (scan > FileInfo.ScanEnd)
            {
                scan = FileInfo.ScanEnd;
            }

            if (!GetScanInfo(scan, out var scanInfo))
            {
                throw new Exception(string.Format(
                    "Cannot retrieve ScanInfo from cache for scan {0} in GetScanPrecisionData; cannot retrieve scan data", scan));
            }

            try
            {
                if (mXRawFile == null)
                {
                    massResolutionData = Array.Empty<MassPrecisionInfoType>();
                    return -1;
                }

                if (!scanInfo.IsFTMS)
                {
                    RaiseWarningMessage(string.Format(
                        "Scan {0} is not an FTMS scan; method GetScanPrecisionData cannot be used with this scan", scan));

                    massResolutionData = Array.Empty<MassPrecisionInfoType>();
                    return -1;
                }

                //object massResolutionDataList = null;
                var mpe = new PrecisionEstimate
                {
                    Rawfile = mXRawFile,
                    ScanNumber = scan
                };

                var results = mpe.GetMassPrecisionEstimate().ToList();

                if (results.Count > 0)
                {
                    var dataCount = results.Count;
                    massResolutionData = new MassPrecisionInfoType[dataCount];

                    for (var i = 0; i < dataCount; i++)
                    {
                        massResolutionData[i] = new MassPrecisionInfoType
                        {
                            Intensity = results[i].Intensity,
                            Mass = results[i].Mass,
                            AccuracyMMU = results[i].MassAccuracyInMmu,
                            AccuracyPPM = results[i].MassAccuracyInPpm,
                            Resolution = results[i].Resolution
                        };
                    }

                    return dataCount;
                }

                massResolutionData = Array.Empty<MassPrecisionInfoType>();
                return 0;
            }
            catch (AccessViolationException)
            {
                RaiseWarningMessage(string.Format(
                    "Unable to load data for scan {0} in GetScanPrecisionData; possibly a corrupt .Raw file", scan));
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(
                    string.Format(
                        "Unable to load data for scan {0} in GetScanPrecisionData: {1}; possibly a corrupt .Raw file",
                        scan, ex.Message),
                    ex);
            }

            massResolutionData = Array.Empty<MassPrecisionInfoType>();
            return -1;
        }

        /// <summary>
        /// Sums data across scans
        /// </summary>
        /// <remarks>Uses the scan filter of the first scan to assure that we're only averaging similar scans</remarks>
        /// <param name="scanFirst"></param>
        /// <param name="scanLast"></param>
        /// <param name="massIntensityPairs"></param>
        /// <param name="maxNumberOfPeaks"></param>
        /// <param name="centroidData"></param>
        /// <returns>The number of data points</returns>
        public int GetScanDataSumScans(int scanFirst, int scanLast, out double[,] massIntensityPairs, int maxNumberOfPeaks, bool centroidData)
        {
            try
            {
                try
                {
                    // Instantiate an instance of the BackgroundSubtractor to assure that file
                    // ThermoFisher.CommonCore.BackgroundSubtraction.dll exists
                    var bgSub = new BackgroundSubtractor();
                    var info = bgSub.ToString();

                    if (string.IsNullOrWhiteSpace(info))
                    {
                        massIntensityPairs = new double[0, 0];
                        return -1;
                    }
                }
                catch (Exception)
                {
                    RaiseWarningMessage("Unable to load data summing scans; file ThermoFisher.CommonCore.BackgroundSubtraction.dll is missing or corrupt");
                }

                if (mXRawFile == null)
                {
                    massIntensityPairs = new double[0, 0];
                    return -1;
                }

                // Make sure the MS controller is selected
                if (!SetMSController())
                {
                    massIntensityPairs = new double[0, 0];
                    return -1;
                }

                if (scanFirst < FileInfo.ScanStart)
                {
                    scanFirst = FileInfo.ScanStart;
                }
                else if (scanFirst > FileInfo.ScanEnd)
                {
                    scanFirst = FileInfo.ScanEnd;
                }

                if (scanLast < scanFirst)
                    scanLast = scanFirst;

                if (scanLast < FileInfo.ScanStart)
                {
                    scanLast = FileInfo.ScanStart;
                }
                else if (scanLast > FileInfo.ScanEnd)
                {
                    scanLast = FileInfo.ScanEnd;
                }

                if (maxNumberOfPeaks < 0)
                    maxNumberOfPeaks = 0;

                // Filter scans to only average/sum scans with filter strings similar to the first scan
                // Without this, AverageScansInScanRange averages/sums all scans in the range, regardless of it being appropriate (i.e., it will sum MS1 and MS2 scans together)
                var filter = mXRawFile.GetFilterForScanNumber(scanFirst);

                var data = mXRawFile.AverageScansInScanRange(scanFirst, scanLast, filter);
                data.PreferCentroids = centroidData;

                var masses = data.PreferredMasses;

                var dataCount = maxNumberOfPeaks > 0 ? Math.Min(masses.Length, maxNumberOfPeaks) : masses.Length;

                if (dataCount > 0)
                {
                    var intensities = data.PreferredIntensities;

                    if (maxNumberOfPeaks > 0)
                    {
                        Array.Sort(intensities, masses);
                        Array.Reverse(intensities);
                        Array.Reverse(masses);
                        Array.Sort(masses, intensities, 0, dataCount);
                    }

                    massIntensityPairs = new double[2, dataCount];
                    for (var i = 0; i < dataCount; i++)
                    {
                        massIntensityPairs[0, i] = masses[i];
                        massIntensityPairs[1, i] = intensities[i];
                    }
                }
                else
                {
                    massIntensityPairs = new double[0, 0];
                }

                return dataCount;
            }
            catch (AccessViolationException)
            {
                RaiseWarningMessage(string.Format(
                    "Unable to load data summing scans {0} to {1}; possibly a corrupt .Raw file", scanFirst, scanLast));
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(
                    string.Format(
                        "Unable to load data summing scans {0} to {1}: {2}; possibly a corrupt .Raw file",
                        scanFirst, scanLast, ex),
                    ex);
            }

            massIntensityPairs = new double[0, 0];
            return -1;
        }

        /// <summary>
        /// Open the .raw file
        /// </summary>
        /// <param name="filePath"></param>
        public bool OpenRawFile(string filePath)
        {
            try
            {
                var dataFile = new FileInfo(filePath);

                if (!dataFile.Exists)
                {
                    RaiseErrorMessage(string.Format("File not found: {0}", filePath));
                    return false;
                }

                // Make sure any existing open files are closed
                CloseRawFile();

                mCachedScanInfo.Clear();
                mCachedScans.Clear();
                RawFilePath = string.Empty;

                if (TraceMode)
                    OnDebugEvent("Initializing RawFileReaderAdapter.FileFactory for " + dataFile.FullName);

                mXRawFile = RawFileReaderAdapter.FileFactory(dataFile.FullName);

                if (TraceMode)
                    OnDebugEvent("Accessing mXRawFile.FileHeader");

                mXRawFileHeader = mXRawFile.FileHeader;

                UpdateReaderOptions();

                if (mXRawFile.IsError)
                {
                    return false;
                }

                RawFilePath = dataFile.FullName;

                if (!FillFileInfo())
                {
                    RawFilePath = string.Empty;
                    return false;
                }

                if (FileInfo.ScanStart == 0 && FileInfo.ScanEnd == 0 && FileInfo.VersionNumber == 0 &&
                    Math.Abs(FileInfo.MassResolution) < double.Epsilon && string.IsNullOrWhiteSpace(FileInfo.InstModel))
                {
                    RaiseErrorMessage("File did not load correctly; ScanStart, ScanEnd, VersionNumber, and MassResolution are all 0 for " + RawFilePath);
                    FileInfo.CorruptFile = true;
                    RawFilePath = string.Empty;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseErrorMessage(string.Format("Exception opening {0}: {1}", filePath, ex.Message), ex);
                RawFilePath = string.Empty;
                return false;
            }
        }

        private bool TuneMethodsMatch(TuneMethod method1, TuneMethod method2)
        {
            if (method1.Settings.Count != method2.Settings.Count)
            {
                // Different segment number of setting count; the methods don't match
                return false;
            }

            for (var index = 0; index <= method1.Settings.Count - 1; index++)
            {
                if (method1.Settings[index].Category != method2.Settings[index].Category ||
                    method1.Settings[index].Name != method2.Settings[index].Name ||
                    method1.Settings[index].Value != method2.Settings[index].Value)
                {
                    // Different segment data; the methods don't match
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Update options in mXRawFile based on current values in the Options instance of ThermoReaderOptions
        /// </summary>
        /// <remarks>Called from OpenRawFile and whenever the Options class raises event OptionsUpdated</remarks>
        private void UpdateReaderOptions()
        {
            mXRawFile.IncludeReferenceAndExceptionData = Options.IncludeReferenceAndExceptionData;
        }

        /// <summary>
        /// Validate that the .raw file has this device. If it does, select it using mXRawFile.SelectInstrument
        /// If the device does not exist or there is an error, returns a message describing the problem
        /// </summary>
        /// <param name="deviceType"></param>
        /// <param name="deviceNumber"></param>
        /// <returns>Empty string if the device was successfully selected, otherwise an error message</returns>
        private string ValidateAndSelectDevice(Device deviceType, int deviceNumber)
        {
            if (!FileInfo.Devices.TryGetValue(deviceType, out var deviceCount) || deviceCount == 0)
            {
                return string.Format(".raw file does not have data from device type {0}", deviceType);
            }

            if (deviceNumber > deviceCount)
            {
                string validValues;
                if (deviceCount == 1)
                {
                    validValues = string.Format("the file only has one entry for device type {0}; specify deviceNumber = 1", deviceType);
                }
                else
                {
                    validValues = string.Format("valid device numbers for device type {0} are {1} through {2}", deviceType, 1, deviceCount);
                }

                return string.Format("The specified device number, {0}, is out of range; {1}", deviceType, validValues);
            }

            try
            {
                mXRawFile.SelectInstrument(deviceType, deviceNumber);
            }
            catch (Exception ex)
            {
                var message = string.Format("Unable to select {0} device #{1}; exception {2}", deviceNumber, deviceType, ex.Message);

                SetMSController();
                return message;
            }

            return string.Empty;
        }

        /// <summary>
        /// Dispose the reader
        /// </summary>
        public void Dispose()
        {
            CloseRawFile();
        }

        /// <summary>
        /// Return unnormalized collision energies via call IScanFilter.GetEnergy
        /// </summary>
        /// <param name="scan"></param>
        [Obsolete("The collision energies reported by IScanFilter.GetEnergy are not normalized and are thus not very useful")]
        public List<double> GetCollisionEnergyUnnormalized(int scan)
        {
            var collisionEnergies = new List<double>();

            try
            {
                if (mXRawFile == null)
                    return collisionEnergies;

                var scanFilter = mXRawFile.GetFilterForScanNumber(scan);

                var numMsOrders = (int)scanFilter.MSOrder;

                for (var msOrder = 1; msOrder <= numMsOrders; msOrder++)
                {
                    var collisionEnergy = scanFilter.GetEnergy(msOrder);

                    if (collisionEnergy > 0)
                    {
                        collisionEnergies.Add(collisionEnergy);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Error: Exception in GetCollisionEnergyUnnormalized: " + ex.Message;
                RaiseErrorMessage(msg, ex);
            }

            return collisionEnergies;
        }
    }
}
