using System;
using System.Collections.Generic;

// Base class for derived classes that can read Finnigan .Raw files (LCQ, LTQ, etc.)
// 
// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in November 2004
// Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

namespace ThermoRawFileReader
{

	public abstract class FinniganFileReaderBaseClass
	{

		#region "Constants and Enums"

		public enum ActivationTypeConstants
		{
			Unknown = -1,
			CID = 0,
			MPD = 1,
			ECD = 2,
			PQD = 3,
			ETD = 4,
			HCD = 5,
			AnyType = 6,
			SA = 7,
			PTR = 8,
			NETD = 9,
			NPTR = 10
		}

		public enum MRMScanTypeConstants
		{
			NotMRM = 0,
			MRMQMS = 1,
			// Multiple SIM ranges in a single scan
			SRM = 2,
			// Monitoring a parent ion and one or more daughter ions
			FullNL = 3
			// Full neutral loss scan
		}

		public enum IonModeConstants
		{
			Unknown = 0,
			Positive = 1,
			Negative = 2
		}


		protected int MAX_SCANS_TO_CACHE_INFO = 50000;
		#endregion

		#region "Structures"

		public struct udtTuneMethodType
		{
		    public List<udtTuneMethodSetting> Settings;

            public void Clear()
            {
                Settings = new List<udtTuneMethodSetting>();
            }
		}

	    public struct udtTuneMethodSetting
	    {
            public string Category;
            public string Name;
            public string Value;
	    }

	    public struct udtFileInfoType
		{

            public string AcquisitionDate;      // Will often be blank
            public string AcquisitionFilename;  // Will often be blank
            public string Comment1;             // Will often be blank
            public string Comment2;             // Will often be blank
            public string SampleName;           // Will often be blank
            public string SampleComment;        // Will often be blank
			public DateTime CreationDate;

            public string CreatorID;                // Logon name of the user when the file was created
            public string InstFlags;                // Values should be one of the constants in InstFlags
			public string InstHardwareVersion;
			public string InstSoftwareVersion;
            public List<string> InstMethods;            // Typically only have one instrument method; the length of this array defines the number of instrument methods
			public string InstModel;
			public string InstName;
            public string InstrumentDescription;        // Typically only defined for instruments converted from other formats
			public string InstSerialNumber;
            public List<udtTuneMethodType> TuneMethods; // Typically have one or two tune methods; the length of this array defines the number of tune methods defined
            public int VersionNumber;                   // File format Version Number
			public double MassResolution;
			public int ScanStart;
			public int ScanEnd;

	        public bool CorruptFile;            // Auto-set to true if the file is corrupt / has no data

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

		public struct udtMRMMassRangeType
		{
			public double StartMass;
			public double EndMass;
            public double CentralMass;      // Useful for MRM/SRM experiments

			public override string ToString()
			{
				return StartMass.ToString("0.000") + "-" + EndMass.ToString("0.000");
			}
		}

		public struct udtMRMInfoType
		{
			// List of mass ranges monitored by the first quadrupole
			public List<udtMRMMassRangeType> MRMMassList;

		    public void Clear()
		    {
		        MRMMassList = new List<udtMRMMassRangeType>();
		    }
		}

		public struct udtScanHeaderInfoType
		{

            public int MSLevel;         // 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS
            public int EventNumber;     // 1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.				
            public bool SIMScan;        // True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined); if multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS				
            public MRMScanTypeConstants MRMScanType;        // 1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)				
            public bool ZoomScan;       // True when the given scan is a zoomed in mass region; these spectra are typically skipped when creating SICs

            public int NumPeaks;        // Number of mass intensity value pairs in the specified scan (may not be defined until .GetScanData() is called; -1 if unknown)				
            public double RetentionTime;    // Retention time (in minutes)
			public double LowMass;
			public double HighMass;
			public double TotalIonCurrent;
			public double BasePeakMZ;

			public double BasePeakIntensity;
			public string FilterText;

			public double ParentIonMZ;
            public ActivationTypeConstants ActivationType;      // Activation type (aka activation method) as reported by the reader
            public string CollisionMode;                        // Activation type, determined from the filter string

			public IonModeConstants IonMode;

			public udtMRMInfoType MRMInfo;
			public int NumChannels;
			public bool UniformTime;            // Indicates whether the sampling time increment for the controller is constant
            public double Frequency;            // Sampling frequency for the current controller
            public bool IsCentroidScan;         // True if centroid (sticks) scan; False if profile (continuum) scan

            public string[] ScanEventNames;
		    public string[] ScanEventValues;

		    public string[] StatusLogNames;
		    public string[] StatusLogValues;

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

		protected string mCachedFileName;

		protected Dictionary<int, clsScanInfo> mCachedScanInfo;

		protected udtFileInfoType mFileInfo;
		protected bool mLoadMSMethodInfo = true;

		protected bool mLoadMSTuneInfo = true;
		#endregion

		#region "Interface Functions"

		public udtFileInfoType FileInfo {
			get { return mFileInfo; }
		}

		public bool LoadMSMethodInfo {
			get { return mLoadMSMethodInfo; }
			set { mLoadMSMethodInfo = value; }
		}

		public bool LoadMSTuneInfo {
			get { return mLoadMSTuneInfo; }
			set { mLoadMSTuneInfo = value; }
		}

		#endregion

		#region "Events"

		public event ReportErrorEventHandler ReportError;
		public delegate void ReportErrorEventHandler(string strMessage);
		public event ReportWarningEventHandler ReportWarning;
		public delegate void ReportWarningEventHandler(string strMessage);

		#endregion

		public abstract bool CheckFunctionality();
		public abstract void CloseRawFile();
		public abstract int GetNumScans();

		public abstract bool GetScanInfo(int scan, out udtScanHeaderInfoType udtScanHeaderInfo);
        public abstract bool GetScanInfo(int scan, out clsScanInfo scanInfo);

		public abstract int GetScanData(int scan, out double[] ionMZ, out double[] ionIntensity);
		public abstract int GetScanData(int scan, out double[] ionMZ, out double[] ionIntensity, int maxNumberOfPeaks);

		public abstract bool OpenRawFile(string filePath);

		protected abstract bool FillFileInfo();

		public static void DuplicateMRMInfo(udtMRMInfoType udtSource, out udtMRMInfoType udtTarget)
		{
		    udtTarget = new udtMRMInfoType();
		    udtTarget.Clear();

		    foreach (var item in udtSource.MRMMassList)
		    {
		        udtTarget.MRMMassList.Add(item);
		    }
		}

		protected void RaiseErrorMessage(string message)
		{
			if (ReportError != null) {
				ReportError(message);
			}
		}

		protected void RaiseWarningMessage(string message)
		{
			if (ReportWarning != null) {
				ReportWarning(message);
			}
		}
	}
}
