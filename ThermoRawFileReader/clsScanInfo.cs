﻿using System;
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
        // Ignore Spelling: EThcD, ETciD, frag, Lumos, Orbitrap, Q-Exactive

        /// <summary>
        /// UTC Time that this scan info was cached
        /// </summary>
        /// <remarks>Used for determining which cached scan info can be discarded if too many scans become cached</remarks>
        public DateTime CacheDateUTC { get; }

        /// <summary>
        /// Scan number
        /// </summary>
        public int ScanNumber { get; }

        /// <summary>
        /// MS Level
        /// </summary>
        /// <returns>MS acquisition level, where 1 means MS, 2 means MS/MS, 3 means MS^3 aka MS/MS/MS</returns>
        public int MSLevel { get; set; }

        /// <summary>
        /// Event Number
        /// </summary>
        /// <returns>1 for parent-ion scan; 2 for 1st frag scan, 3 for 2nd frag scan, etc.</returns>
        public int EventNumber { get; set; }

        /// <summary>
        /// SIM Scan Flag
        /// </summary>
        /// <returns>True if this is a selected ion monitoring (SIM) scan (i.e. a small mass range is being examined)</returns>
        /// <remarks>If multiple selected ion ranges are examined simultaneously, then this will be false but MRMScanType will be .MRMQMS</remarks>
        public bool SIMScan { get; set; }

        /// <summary>
        /// Multiple reaction monitoring mode
        /// </summary>
        /// <returns>1 or 2 if this is a multiple reaction monitoring scan (MRMQMS or SRM)</returns>
        public MRMScanTypeConstants MRMScanType { get; set; }

        /// <summary>
        /// Zoom scan flag
        /// </summary>
        /// <returns>True when the given scan is a zoomed in mass region</returns>
        /// <remarks>These spectra are typically skipped when creating SICs</remarks>
        public bool ZoomScan { get; set; }

        /// <summary>
        /// Number of mass intensity value pairs
        /// </summary>
        /// <returns>Number of points, -1 if unknown</returns>
        public int NumPeaks { get; set; }

        /// <summary>
        /// Retention time (in minutes)
        /// </summary>
        public double RetentionTime { get; set; }

        /// <summary>
        /// Lowest m/z value
        /// </summary>
        public double LowMass { get; set; }

        /// <summary>
        /// Highest m/z value
        /// </summary>
        public double HighMass { get; set; }

        /// <summary>
        /// Total ion current
        /// </summary>
        /// <returns>Sum of all ion abundances</returns>
        public double TotalIonCurrent { get; set; }

        /// <summary>
        /// Base peak m/z
        /// </summary>
        /// <returns>m/z value of the most abundant ion in the scan</returns>
        public double BasePeakMZ { get; set; }

        /// <summary>
        /// Base peak intensity
        /// </summary>
        /// <returns>Intensity of the most abundant ion in the scan</returns>
        public double BasePeakIntensity { get; set; }

        /// <summary>
        /// Scan Filter string
        /// </summary>
        public string FilterText { get; set; }

        /// <summary>
        /// Parent ion m/z (aka precursor m/z)
        /// </summary>
        public double ParentIonMZ { get; set; }

        /// <summary>
        /// The monoisotopic parent ion m/z, as determined by the Thermo software
        /// </summary>
        [Obsolete("Unused")]
        public double ParentIonMonoisotopicMZ
        {
            get
            {
                if (TryGetScanEvent("Monoisotopic M/Z:", out var value))
                {
                    return Convert.ToDouble(value);
                }
                return 0;
            }
        }

        /// <summary>
        /// The monoisotopic parent ion m/z, as determined by the Thermo software
        /// </summary>
        [Obsolete("Unused")]
        public double IsolationWindowTargetMZ
        {
            get
            {
                if (TryGetScanEvent("MS2 Isolation Width:", out var value))
                {
                    return Convert.ToDouble(value);
                }
                return 0;
            }
        }

        /// <summary>
        /// The parent ion charge state, as determined by the Thermo software
        /// </summary>
        [Obsolete("Unused")]
        public int ChargeState
        {
            get
            {
                if (TryGetScanEvent("Charge State:", out var value))
                {
                    return Convert.ToInt32(value);
                }
                return 0;
            }
        }

        /// <summary>
        /// Activation type (aka activation method) as reported by the reader
        /// </summary>
        /// <remarks>Not applicable for MS1 scans (though will report 0=CID, which should be disregarded)</remarks>
        public ActivationTypeConstants ActivationType { get; set; }

        /// <summary>
        /// Collision mode, determined from the filter string
        /// </summary>
        /// <remarks>Typically CID, ETD, HCD, EThcD, or ETciD</remarks>
        public string CollisionMode { get; set; }

        /// <summary>
        /// Ionization polarity
        /// </summary>
        public IonModeConstants IonMode { get; set; }

        /// <summary>
        /// MRM mode
        /// </summary>
        public MRMInfo MRMInfo { get; set; }

        /// <summary>
        ///Number of channels
        /// </summary>
        public int NumChannels { get; set; }

        /// <summary>
        /// Indicates whether the sampling time increment for the controller is constant
        /// </summary>
        public bool UniformTime { get; set; }

        /// <summary>
        /// Sampling frequency for the current controller
        /// </summary>
        public double Frequency { get; set; }

        /// <summary>
        /// Ion Injection Time (in milliseconds)
        /// </summary>
        public double IonInjectionTime;

        /// <summary>
        /// Centroid scan flag
        /// </summary>
        /// <returns>True if centroid (sticks) scan; False if profile (continuum) scan</returns>
        public bool IsCentroided { get; set; }

        /// <summary>
        /// FTMS flag (or Orbitrap, Q-Exactive, Lumos, or any other high resolution instrument)
        /// </summary>
        /// <returns>True if acquired on a high resolution mass analyzer (for example, on an Orbitrap or Q-Exactive)</returns>
        public bool IsFTMS { get; set; }

        /// <summary>
        /// Scan event data
        /// </summary>
        /// <returns>List of key/value pairs</returns>
        /// <remarks>Ignores scan events with a blank or null event name</remarks>
        public List<KeyValuePair<string, string>> ScanEvents { get; }

        /// <summary>
        /// Status log data
        /// </summary>
        /// <returns>List of key/value pairs</returns>
        /// <remarks>Includes blank events that separate log sections</remarks>
        public List<KeyValuePair<string, string>> StatusLog { get; }

        /// <summary>
        /// Constructor with only scan number
        /// </summary>
        public clsScanInfo(int scan)
        {
            NumPeaks = -1;
            ScanNumber = scan;
            CacheDateUTC = DateTime.UtcNow;

            FilterText = string.Empty;
            CollisionMode = string.Empty;
            ActivationType = ActivationTypeConstants.Unknown;

            ScanEvents = new List<KeyValuePair<string, string>>();
            StatusLog = new List<KeyValuePair<string, string>>();
        }

        /// <summary>
        /// Store this scan's scan events using parallel string arrays
        /// </summary>
        /// <param name="eventNames"></param>
        /// <param name="eventValues"></param>
        public void StoreScanEvents(string[] eventNames, string[] eventValues)
        {
            StoreParallelStrings(ScanEvents, eventNames, eventValues, true, true);
        }

        /// <summary>
        /// Store this scan's log messages using parallel string arrays
        /// </summary>
        /// <param name="logNames"></param>
        /// <param name="logValues"></param>
        public void StoreStatusLog(string[] logNames, string[] logValues)
        {
            StoreParallelStrings(StatusLog, logNames, logValues);
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
            IEnumerable<KeyValuePair<string, string>> results;

            if (partialMatchToStart)
            {
                // Partial match
                results = from item in ScanEvents where item.Key.StartsWith(eventName, StringComparison.OrdinalIgnoreCase) select item;
            }
            else
            {
                results = from item in ScanEvents where string.Equals(item.Key, eventName, StringComparison.OrdinalIgnoreCase) select item;
            }

            foreach (var item in results)
            {
                eventValue = item.Value;
                return true;
            }

            eventValue = string.Empty;
            return false;
        }

        /// <summary>
        /// Overridden ToString(): Displays a short summary of this object
        /// </summary>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                return "Scan " + ScanNumber + ": Generic ScanHeaderInfo";
            }

            return "Scan " + ScanNumber + ": " + FilterText;
        }

        private void StoreParallelStrings(
            ICollection<KeyValuePair<string, string>> targetList,
            IList<string> names,
            IList<string> values,
            bool skipEmptyNames = false,
            bool replaceTabsInValues = false)
        {
            targetList.Clear();

            for (var i = 0; i <= names.Count - 1; i++)
            {
                if (skipEmptyNames && (string.IsNullOrWhiteSpace(names[i]) || names[i] == "\u0001"))
                {
                    // Name is empty or null
                    continue;
                }

                if (replaceTabsInValues && values[i].Contains("\t"))
                {
                    targetList.Add(new KeyValuePair<string, string>(names[i], values[i].Replace("\t", " ").TrimEnd(' ')));
                }
                else
                {
                    targetList.Add(new KeyValuePair<string, string>(names[i], values[i].TrimEnd(' ')));
                }
            }
        }
    }
}