using System;
using System.Collections.Generic;
using System.Linq;

namespace ThermoRawFileReader
{
    /// <summary>
    /// Container for metadata relating to a single scan
    /// </summary>
    [CLSCompliant(true)]
    public class clsScanInfo
    {

        #region "Member variables"

        /// <summary>
        /// UTC Time that this scan info was cached
        /// </summary>
        protected readonly DateTime mCacheDateUTC;

        /// <summary>
        /// Scan number
        /// </summary>
        protected readonly int mScanNumber;

        /// <summary>
        /// Scan event data
        /// </summary>
        /// <remarks>Ignores scan events with a blank or null event name</remarks>
        protected readonly List<KeyValuePair<string, string>> mScanEvents;

        /// <summary>
        /// Status Log data
        /// </summary>
        /// <remarks>Includes blank events that separate log sections</remarks>
        protected readonly List<KeyValuePair<string, string>> mStatusLog;
        #endregion

        #region "Properties"

        /// <summary>
        /// UTC Time that this scan info was cached
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks>Used for determining which cached scan info can be discarded if too many scans become cached</remarks>
        public DateTime CacheDateUTC => mCacheDateUTC;

        /// <summary>
        /// Scan number
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int ScanNumber => mScanNumber;

        /// <summary>
        /// MS Level
        /// </summary>
        /// <value></value>
        /// <returns>MS acquisition level, where 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS</returns>
        /// <remarks></remarks>
        public int MSLevel { get; set; }

        /// <summary>
        /// Event Number
        /// </summary>
        /// <value></value>
        /// <returns>1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.</returns>
        /// <remarks></remarks>
        public int EventNumber { get; set; }

        /// <summary>
        /// SIM Scan Flag
        /// </summary>
        /// <value></value>
        /// <returns>True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined)</returns>
        /// <remarks>If multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS</remarks>
        public bool SIMScan { get; set; }

        /// <summary>
        /// Multiple reaction monitoring mode
        /// </summary>
        /// <value></value>
        /// <returns>1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)</returns>
        /// <remarks></remarks>
        public MRMScanTypeConstants MRMScanType { get; set; }

        /// <summary>
        /// Zoom scan flag
        /// </summary>
        /// <value></value>
        /// <returns>True when the given scan is a zoomed in mass region</returns>
        /// <remarks>These spectra are typically skipped when creating SICs</remarks>
        public bool ZoomScan { get; set; }

        /// <summary>
        /// Number of mass intensity value pairs
        /// </summary>
        /// <value></value>
        /// <returns>Number of points, -1 if unknown</returns>
        /// <remarks></remarks>
        public int NumPeaks { get; set; }

        /// <summary>
        /// Retention time (in minutes)
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public double RetentionTime { get; set; }

        /// <summary>
        /// Lowest m/z value
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public double LowMass { get; set; }

        /// <summary>
        /// Highest m/z value
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public double HighMass { get; set; }

        /// <summary>
        /// Total ion current
        /// </summary>
        /// <value></value>
        /// <returns>Sum of all ion abundances</returns>
        /// <remarks></remarks>
        public double TotalIonCurrent { get; set; }

        /// <summary>
        /// Base peak m/z
        /// </summary>
        /// <value></value>
        /// <returns>m/z value of the most abundant ion in the scan</returns>
        /// <remarks></remarks>
        public double BasePeakMZ { get; set; }

        /// <summary>
        /// Base peak intensity
        /// </summary>
        /// <value></value>
        /// <returns>Intensity of the most abundant ion in the scan</returns>
        /// <remarks></remarks>
        public double BasePeakIntensity { get; set; }

        /// <summary>
        /// Scan Filter string
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public string FilterText { get; set; }

        /// <summary>
        /// Parent ion m/z
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public double ParentIonMZ { get; set; }

        /// <summary>
        /// Activation type (aka activation method) as reported by the reader
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public ActivationTypeConstants ActivationType { get; set; }

        /// <summary>
        /// Collision mode, determined from the filter string
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks>Typically CID, ETD, HCD, EThcD, or ETciD</remarks>
        public string CollisionMode { get; set; }

        /// <summary>
        /// Ionization polarity
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public IonModeConstants IonMode { get; set; }

        /// <summary>
        /// MRM mode
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public MRMInfo MRMInfo { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int NumChannels { get; set; }

        /// <summary>
        /// Indicates whether the sampling time increment for the controller is constant
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool UniformTime { get; set; }

        /// <summary>
        /// Sampling frequency for the current controller
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public double Frequency { get; set; }

        /// <summary>
        /// Centroid scan flag
        /// </summary>
        /// <value></value>
        /// <returns>True if centroid (sticks) scan; False if profile (continuum) scan</returns>
        /// <remarks></remarks>
        public bool IsCentroided { get; set; }

        /// <summary>
        /// FTMS flag
        /// </summary>
        /// <value></value>
        /// <returns>True if acquired on a high resolution mass analyzer (for example, on an Orbitrap or Q-Exactive)</returns>
        /// <remarks></remarks>
        public bool IsFTMS { get; set; }

        /// <summary>
        /// Scan event data
        /// </summary>
        /// <value></value>
        /// <returns>List of key/value pairs</returns>
        /// <remarks></remarks>
        public List<KeyValuePair<string, string>> ScanEvents => mScanEvents;

        /// <summary>
        /// Status log data
        /// </summary>
        /// <value></value>
        /// <returns>List of key/value pairs</returns>
        /// <remarks></remarks>
        public List<KeyValuePair<string, string>> StatusLog => mStatusLog;

        #endregion

        #region "Constructor and public methods"

        /// <summary>
        /// Constructor with only scan number
        /// </summary>
        /// <remarks></remarks>
        public clsScanInfo(int scan)
        {
            NumPeaks = -1;
            mScanNumber = scan;
            mCacheDateUTC = DateTime.UtcNow;

            FilterText = string.Empty;
            CollisionMode = string.Empty;
            ActivationType = ActivationTypeConstants.Unknown;

            mScanEvents = new List<KeyValuePair<string, string>>();
            mStatusLog = new List<KeyValuePair<string, string>>();

        }

        /// <summary>
        /// Constructor with scan number and data in a udtScanHeaderInfoType struct
        /// </summary>
        /// <remarks></remarks>
        [Obsolete("udtScanHeaderInfoType is obsolete")]
        public clsScanInfo(int scan, udtScanHeaderInfoType udtScanHeaderInfo) : this(scan)
        {
            CopyFromStruct(udtScanHeaderInfo);
        }

        /// <summary>
        /// Store this scan's scan events using parallel string arrays
        /// </summary>
        /// <param name="eventNames"></param>
        /// <param name="eventValues"></param>
        /// <remarks></remarks>
        public void StoreScanEvents(string[] eventNames, string[] eventValues)
        {
            StoreParallelStrings(mScanEvents, eventNames, eventValues);
        }

        /// <summary>
        /// Store this scan's log messages using parallel string arrays
        /// </summary>
        /// <param name="logNames"></param>
        /// <param name="logValues"></param>
        /// <remarks></remarks>
        public void StoreStatusLog(string[] logNames, string[] logValues)
        {
            StoreParallelStrings(mStatusLog, logNames, logValues);
        }

        /// <summary>
        /// Get the event value associated with the given scan event name
        /// </summary>
        /// <param name="eventName">Event name to find</param>
        /// <param name="eventValue">Event value</param>
        /// <param name="partialMatchToStart">Set to true to match the start of an event name, and not require a full match</param>
        /// <returns>True if found a match for the event name, otherwise false</returns>
        /// <remarks>Event names nearly always end in a colon, e.g. "Monoisotopic M/Z:" or "Charge State:"</remarks>
        public bool TryGetScanEvent(string eventName, out string eventValue, bool partialMatchToStart = false)
        {

            IEnumerable<KeyValuePair<string, string>> lstResults;

            if (partialMatchToStart) {
                // Partial match
                lstResults = from item in mScanEvents where item.Key.ToLower().StartsWith(eventName.ToLower()) select item;
            } else {
                lstResults = from item in mScanEvents where string.Equals(item.Key, eventName, StringComparison.InvariantCultureIgnoreCase) select item;
            }

            foreach (var item in lstResults) {
                eventValue = item.Value;
                return true;
            }

            eventValue = string.Empty;
            return false;

        }

        /// <summary>
        /// Overridden ToString(): Displays a short summary of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(FilterText)) {
                return "Scan " + ScanNumber + ": Generic ScanHeaderInfo";
            }

            return "Scan " + ScanNumber + ": " + FilterText;
        }

        #endregion

        #region "Private methods"


        [Obsolete("Use clsScanInfo")]
        private void CopyFromStruct(udtScanHeaderInfoType udtScanHeaderInfoType)
        {
            MSLevel = udtScanHeaderInfoType.MSLevel;
            EventNumber = udtScanHeaderInfoType.EventNumber;
            SIMScan = udtScanHeaderInfoType.SIMScan;
            MRMScanType = udtScanHeaderInfoType.MRMScanType;
            ZoomScan = udtScanHeaderInfoType.ZoomScan;

            NumPeaks = udtScanHeaderInfoType.NumPeaks;
            RetentionTime = udtScanHeaderInfoType.RetentionTime;
            LowMass = udtScanHeaderInfoType.LowMass;
            HighMass = udtScanHeaderInfoType.HighMass;
            TotalIonCurrent = udtScanHeaderInfoType.TotalIonCurrent;
            BasePeakMZ = udtScanHeaderInfoType.BasePeakMZ;
            BasePeakIntensity = udtScanHeaderInfoType.BasePeakIntensity;

            FilterText = udtScanHeaderInfoType.FilterText;
            ParentIonMZ = udtScanHeaderInfoType.ParentIonMZ;
            CollisionMode = udtScanHeaderInfoType.CollisionMode;
            ActivationType = udtScanHeaderInfoType.ActivationType;

            IonMode = udtScanHeaderInfoType.IonMode;
            MRMInfo = udtScanHeaderInfoType.MRMInfo;

            NumChannels = udtScanHeaderInfoType.NumChannels;
            UniformTime = udtScanHeaderInfoType.UniformTime;
            Frequency = udtScanHeaderInfoType.Frequency;
            IsCentroided = udtScanHeaderInfoType.IsCentroidScan;

            StoreScanEvents(udtScanHeaderInfoType.ScanEventNames, udtScanHeaderInfoType.ScanEventValues);
            StoreStatusLog(udtScanHeaderInfoType.StatusLogNames, udtScanHeaderInfoType.StatusLogValues);

        }


        private void StoreParallelStrings(ICollection<KeyValuePair<string, string>> targetList, IList<string> names, IList<string> values)
        {
            targetList.Clear();

            for (var i = 0; i <= names.Count - 1; i++) {
                targetList.Add(new KeyValuePair<string, string>(names[i], values[i]));
            }

        }
        #endregion
    }
}
