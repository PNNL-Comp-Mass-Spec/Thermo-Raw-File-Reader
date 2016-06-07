using System;
using System.Collections.Generic;

// Base class for derived classes that can read Finnigan .Raw files (LCQ, LTQ, etc.)
// 
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

namespace ThermoRawFileReader
{
    /// <summary>
    /// Base class for retrieving data from Thermo Finnigan .Raw files
    /// </summary>
	public abstract class FinniganFileReaderBaseClass
	{

		#region "Constants and Enums"

        /// <summary>
        /// Activation Types enum
        /// </summary>
		public enum ActivationTypeConstants
		{
            /// <summary>
            /// Unknown activation type
            /// </summary>
			Unknown = -1,

            /// <summary>
            /// Collision-Induced Dissociation
            /// </summary>
            CID = 0,

            /// <summary>
            /// Multi Photo Dissociation
            /// </summary>
            MPD = 1,

            /// <summary>
            /// Electron Capture Dissociation
            /// </summary>
            ECD = 2,

            /// <summary>
            /// Pulsed Q Dissociation
            /// </summary>
            PQD = 3,

            /// <summary>
            /// Electron Transfer Dissociation
            /// </summary>
            ETD = 4,

            /// <summary>
            /// High-energy Collision-induce Dissociation (psi-ms: beam-type collision-induced dissociation)
            /// </summary>
            HCD = 5,

            /// <summary>
            /// Any activation type
            /// </summary>
            AnyType = 6,

            /// <summary>
            /// Supplemental Activation
            /// </summary>
            SA = 7,

            /// <summary>
            /// Photon Transfer Reaction
            /// </summary>
            PTR = 8,

            /// <summary>
            /// Negative Electron Transfer Dissociation
            /// </summary>
            NETD = 9,

            /// <summary>
            /// Negative Photon Transfer Reaction
            /// </summary>
			NPTR = 10
		}

        /// <summary>
        /// MRM Scan Types
        /// </summary>
		public enum MRMScanTypeConstants
		{
            /// <summary>
            /// Not MRM
            /// </summary>
			NotMRM = 0,

			/// <summary>
            /// Multiple SIM ranges in a single scan
			/// </summary>
			MRMQMS = 1,

			/// <summary>
            /// Monitoring a parent ion and one or more daughter ions
			/// </summary>
			SRM = 2,

			/// <summary>
            /// Full neutral loss scan
			/// </summary>
			FullNL = 3
		}

        /// <summary>
        /// Ion Modes
        /// </summary>
		public enum IonModeConstants
		{
            /// <summary>
            /// Unknown Ion Mode
            /// </summary>
			Unknown = 0,

            /// <summary>
            /// Positive Ion Mode
            /// </summary>
			Positive = 1,

            /// <summary>
            /// Negative Ion Mode
            /// </summary>
			Negative = 2
		}

        /// <summary>
        /// Maximum size of the scan info cache
        /// </summary>
		protected int MAX_SCANS_TO_CACHE_INFO = 50000;
		#endregion

		#region "Structures"

        /// <summary>
        /// Type for Tune Methods
        /// </summary>
		public struct udtTuneMethodType
		{
            /// <summary>
            /// Settings
            /// </summary>
		    public List<udtTuneMethodSetting> Settings;

            /// <summary>
            /// Clear the settings
            /// </summary>
            public void Clear()
            {
                Settings = new List<udtTuneMethodSetting>();
            }
		}

        /// <summary>
        /// Type for Tune Method Settings
        /// </summary>
	    public struct udtTuneMethodSetting
	    {
            /// <summary>
            /// Tune category
            /// </summary>
            public string Category;

            /// <summary>
            /// Tune name
            /// </summary>
            public string Name;

            /// <summary>
            /// Tune value
            /// </summary>
            public string Value;
	    }

        /// <summary>
        /// Type for File Information
        /// </summary>
	    public struct udtFileInfoType
		{
            /// <summary>
            /// Date of Acquisition
            /// </summary>
            public string AcquisitionDate;      // Will often be blank

            /// <summary>
            /// Acquisition Filename
            /// </summary>
            public string AcquisitionFilename;  // Will often be blank

            /// <summary>
            /// First Comment
            /// </summary>
            public string Comment1;             // Will often be blank

            /// <summary>
            /// Second Comment
            /// </summary>
            public string Comment2;             // Will often be blank

            /// <summary>
            /// Sample Name
            /// </summary>
            public string SampleName;           // Will often be blank

            /// <summary>
            /// Sample Comment
            /// </summary>
            public string SampleComment;        // Will often be blank

            /// <summary>
            /// Creation date
            /// </summary>
			public DateTime CreationDate;

            /// <summary>
            /// Logon name of the user when the file was created
            /// </summary>
            public string CreatorID;

            /// <summary>
            /// Instrument Flags
            /// </summary>
            public string InstFlags;                // Values should be one of the constants in InstFlags

            /// <summary>
            /// Instrument Hardware Version
            /// </summary>
			public string InstHardwareVersion;

            /// <summary>
            /// Instrument Software Version
            /// </summary>
			public string InstSoftwareVersion;

            /// <summary>
            /// Instrument Methods
            /// </summary>
            public List<string> InstMethods;            // Typically only have one instrument method; the length of this array defines the number of instrument methods

            /// <summary>
            /// Instrument Model
            /// </summary>
			public string InstModel;

            /// <summary>
            /// Instrument Name
            /// </summary>
			public string InstName;

            /// <summary>
            /// Instrument Description
            /// </summary>
            public string InstrumentDescription;        // Typically only defined for instruments converted from other formats

            /// <summary>
            /// Instrument Serial Number
            /// </summary>
			public string InstSerialNumber;
            
            /// <summary>
            /// Tune Methods
            /// </summary>
            public List<udtTuneMethodType> TuneMethods; // Typically have one or two tune methods; the length of this array defines the number of tune methods defined

            /// <summary>
            /// Version Number
            /// </summary>
            public int VersionNumber;                   // File format Version Number

            /// <summary>
            /// Mass Resolution
            /// </summary>
			public double MassResolution;

            /// <summary>
            /// First scan number
            /// </summary>
			public int ScanStart;

            /// <summary>
            /// Last scan number
            /// </summary>
			public int ScanEnd;

            /// <summary>
            /// Flag for corrupt files
            /// </summary>
	        public bool CorruptFile;            // Auto-set to true if the file is corrupt / has no data

            /// <summary>
            /// Reset all data in the struct
            /// </summary>
	        public void Clear()
	        {
                AcquisitionDate = string.Empty;
                AcquisitionFilename = string.Empty;
                Comment1 = string.Empty;
                Comment2 = string.Empty;
                SampleName = string.Empty;
                SampleComment = string.Empty;
	            CreationDate = DateTime.MinValue;

                CreatorID = string.Empty;
                InstFlags = string.Empty;
                InstHardwareVersion = string.Empty;
                InstSoftwareVersion = string.Empty;
	            InstMethods = new List<string>();
                InstModel = string.Empty;
                InstName = string.Empty;
                InstrumentDescription = string.Empty;
                InstSerialNumber = string.Empty;
                TuneMethods = new List<udtTuneMethodType>();

	            VersionNumber = 0;
                MassResolution = 0;
                ScanStart = 0;
                ScanEnd = 0;

	            CorruptFile = false;
	        }
		}

        /// <summary>
        /// Type for storing MRM Mass Ranges
        /// </summary>
		public struct udtMRMMassRangeType
		{
            /// <summary>
            /// Start Mass
            /// </summary>
			public double StartMass;

            /// <summary>
            /// End Mass
            /// </summary>
			public double EndMass;

            /// <summary>
            /// Central Mass
            /// </summary>
            public double CentralMass;      // Useful for MRM/SRM experiments

            /// <summary>
            /// Return a summary of this object
            /// </summary>
            /// <returns></returns>
			public override string ToString()
			{
				return StartMass.ToString("0.000") + "-" + EndMass.ToString("0.000");
			}
		}

        /// <summary>
        /// Type for MRM Information
        /// </summary>
		public struct udtMRMInfoType
		{
			/// <summary>
            /// List of mass ranges monitored by the first quadrupole
			/// </summary>
			public List<udtMRMMassRangeType> MRMMassList;

            /// <summary>
            /// Clear all data in the object
            /// </summary>
		    public void Clear()
		    {
		        MRMMassList = new List<udtMRMMassRangeType>();
		    }
		}

        /// <summary>
        /// Type for scan header information/metadata
        /// </summary>
		public struct udtScanHeaderInfoType
		{
            /// <summary>
            /// MS Level
            /// </summary>
            public int MSLevel;         // 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS

            /// <summary>
            /// Event Number: 1 for parent-ion scan, >1 for fragmentation scans (in order)
            /// </summary>
            public int EventNumber;     // 1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.		
		
            /// <summary>
            /// If this is a Selected Ion Monitoring scan
            /// </summary>
            public bool SIMScan;        // True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined); if multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS

            /// <summary>
            /// MRM Scan Type
            /// </summary>
            public MRMScanTypeConstants MRMScanType;        // 1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)	
			
            /// <summary>
            /// If this is a zoom scan
            /// </summary>
            public bool ZoomScan;       // True when the given scan is a zoomed in mass region; these spectra are typically skipped when creating SICs

            /// <summary>
            /// Number of mass intensity value pairs in the specified scan (may not be defined until .GetScanData() is called; -1 if unknown)
            /// </summary>
            public int NumPeaks;

            /// <summary>
            /// Retention time (in minutes)
            /// </summary>
            public double RetentionTime;

            /// <summary>
            /// Lowest m/z value
            /// </summary>
			public double LowMass;

            /// <summary>
            /// Highest m/z value
            /// </summary>
			public double HighMass;

            /// <summary>
            /// Total Ion Current
            /// </summary>
			public double TotalIonCurrent;

            /// <summary>
            /// Mase Peak m/z
            /// </summary>
			public double BasePeakMZ;

            /// <summary>
            /// Base Peak Intensity
            /// </summary>
			public double BasePeakIntensity;

            /// <summary>
            /// Scan Filter string
            /// </summary>
			public string FilterText;

            /// <summary>
            /// Parent Ion m/z
            /// </summary>
			public double ParentIonMZ;

            /// <summary>
            /// Activation type (aka activation method) as reported by the reader
            /// </summary>
            public ActivationTypeConstants ActivationType;

            /// <summary>
            /// Activation type, determined from the filter string
            /// </summary>
            public string CollisionMode;

            /// <summary>
            /// Ion Mode
            /// </summary>
			public IonModeConstants IonMode;

            /// <summary>
            /// MRM Information
            /// </summary>
			public udtMRMInfoType MRMInfo;

            /// <summary>
            /// Number of channels
            /// </summary>
			public int NumChannels;

            /// <summary>
            /// Indicates whether the sampling time increment for the controller is constant
            /// </summary>
			public bool UniformTime;

            /// <summary>
            /// Sampling frequency for the current controller
            /// </summary>
            public double Frequency;

            /// <summary>
            /// True if centroid (sticks) scan; False if profile (continuum) scan
            /// </summary>
            public bool IsCentroidScan;

            /// <summary>
            /// Names of scan events
            /// </summary>
            public string[] ScanEventNames;

            /// <summary>
            /// Values of scan events, corresponding to ScanEventNames
            /// </summary>
		    public string[] ScanEventValues;

            /// <summary>
            /// Names of status log entries
            /// </summary>
		    public string[] StatusLogNames;

            /// <summary>
            /// Values of status log entries, corresponding to StatusLogNames
            /// </summary>
		    public string[] StatusLogValues;

            /// <summary>
            /// Summary of this object in a string
            /// </summary>
            /// <returns></returns>
			public override string ToString()
			{
				if (string.IsNullOrEmpty(FilterText)) {
					return "Generic udtScanHeaderInfoType";
				} else {
					return FilterText;
				}
			}
		}

		#endregion

		#region "Classwide Variables"

        /// <summary>
        /// The currently loaded .raw file
        /// </summary>
		protected string mCachedFileName;

        /// <summary>
        /// The scan info cache
        /// </summary>
		protected Dictionary<int, clsScanInfo> mCachedScanInfo;

        /// <summary>
        /// File info for the currently loaded .raw file
        /// </summary>
		protected udtFileInfoType mFileInfo;

        /// <summary>
        /// MS Method Information
        /// </summary>
		protected bool mLoadMSMethodInfo = true;

        /// <summary>
        /// MS Tune Information
        /// </summary>
		protected bool mLoadMSTuneInfo = true;
		#endregion

		#region "Interface Functions"

        /// <summary>
        /// Get FileInfo about the currently loaded .raw file
        /// </summary>
		public udtFileInfoType FileInfo {
			get { return mFileInfo; }
		}

        /// <summary>
        /// MS Method information
        /// </summary>
		public bool LoadMSMethodInfo {
			get { return mLoadMSMethodInfo; }
			set { mLoadMSMethodInfo = value; }
		}

        /// <summary>
        /// MS Tune Info
        /// </summary>
		public bool LoadMSTuneInfo {
			get { return mLoadMSTuneInfo; }
			set { mLoadMSTuneInfo = value; }
		}

		#endregion

		#region "Events"

        /// <summary>
        /// Event handler for reporting error messages
        /// </summary>
        public event ReportErrorEventHandler ReportError;

        /// <summary>
        /// Event handler delegate for reporting error messages
        /// </summary>
        /// <param name="strMessage"></param>
		public delegate void ReportErrorEventHandler(string strMessage);

        /// <summary>
        /// Event handler for reporting warning messages
        /// </summary>
		public event ReportWarningEventHandler ReportWarning;

        /// <summary>
        /// Event handler delegate for reporting warning messages
        /// </summary>
        /// <param name="strMessage"></param>
		public delegate void ReportWarningEventHandler(string strMessage);

		#endregion

        /// <summary>
        /// Test the functionality of the reader - can we instantiate the MSFileReader Object?
        /// </summary>
        /// <returns></returns>
		public abstract bool CheckFunctionality();

        /// <summary>
        /// Close the .raw file
        /// </summary>
		public abstract void CloseRawFile();

        /// <summary>
        /// Number of scans in the .raw file
        /// </summary>
        /// <returns></returns>
		public abstract int GetNumScans();

        /// <summary>
        /// Read scan metadata
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="udtScanHeaderInfo"></param>
        /// <returns></returns>
		public abstract bool GetScanInfo(int scan, out udtScanHeaderInfoType udtScanHeaderInfo);

        /// <summary>
        /// Read scan metadata
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="scanInfo"></param>
        /// <returns></returns>
        public abstract bool GetScanInfo(int scan, out clsScanInfo scanInfo);

        /// <summary>
        /// Read scan peak data
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="ionMZ"></param>
        /// <param name="ionIntensity"></param>
        /// <returns></returns>
		public abstract int GetScanData(int scan, out double[] ionMZ, out double[] ionIntensity);

        /// <summary>
        /// Read scan peak data
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="ionMZ"></param>
        /// <param name="ionIntensity"></param>
        /// <param name="maxNumberOfPeaks"></param>
        /// <returns></returns>
		public abstract int GetScanData(int scan, out double[] ionMZ, out double[] ionIntensity, int maxNumberOfPeaks);

        /// <summary>
        /// Open the .raw file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
		public abstract bool OpenRawFile(string filePath);

        /// <summary>
        /// Populate mFileInfo
        /// </summary>
        /// <returns></returns>
		protected abstract bool FillFileInfo();

        /// <summary>
        /// Duplicate the MRM info
        /// </summary>
        /// <param name="udtSource"></param>
        /// <param name="udtTarget"></param>
		public static void DuplicateMRMInfo(udtMRMInfoType udtSource, out udtMRMInfoType udtTarget)
		{
		    udtTarget = new udtMRMInfoType();
		    udtTarget.Clear();

		    foreach (var item in udtSource.MRMMassList)
		    {
		        udtTarget.MRMMassList.Add(item);
		    }
		}

        /// <summary>
        /// Report an error message to the error event handler
        /// </summary>
        /// <param name="message"></param>
		protected void RaiseErrorMessage(string message)
		{
			if (ReportError != null) {
				ReportError(message);
			}
		}

        /// <summary>
        /// Report a warning message to the warning event handler
        /// </summary>
        /// <param name="message"></param>
		protected void RaiseWarningMessage(string message)
		{
			if (ReportWarning != null) {
				ReportWarning(message);
			}
		}
	}
}
