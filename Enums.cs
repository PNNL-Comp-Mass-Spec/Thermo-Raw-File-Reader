using System;

namespace ThermoRawFileReader
{
    /// <summary>
    /// Activation Types enum
    /// </summary>
    [CLSCompliant(true)]
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
    [CLSCompliant(true)]
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
    [CLSCompliant(true)]
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
    /// Sample types
    /// </summary>
    /// <remarks>Returned by <see cref="XRawFileIO.mXRawFile"/>.GetSeqRowSampleType()</remarks>
    [CLSCompliant(true)]
    public enum SampleTypeConstants
    {
        /// <summary>
        /// Unknown sample type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Blank sample
        /// </summary>
        Blank = 1,

        /// <summary>
        /// QC sample
        /// </summary>
        QC = 2,

        /// <summary>
        /// Standard Clear (None) sample
        /// </summary>
        StandardClear_None = 3,

        /// <summary>
        /// Standard Update (None) sample
        /// </summary>
        StandardUpdate_None = 4,

        /// <summary>
        /// Standard Bracket (Open) sample
        /// </summary>
        StandardBracket_Open = 5,

        /// <summary>
        /// Standard Bracket Start (multiple brackets) sample
        /// </summary>
        StandardBracketStart_MultipleBrackets = 6,

        /// <summary>
        /// Standard Bracket End (multiple brackets) sample
        /// </summary>
        StandardBracketEnd_multipleBrackets = 7
    }

    /// <summary>
    /// Controller Types
    /// </summary>
    /// <remarks> Used with <see cref="XRawFileIO.SetMSController()"/></remarks>
    [CLSCompliant(true)]
    public enum ControllerTypeConstants
    {
        /// <summary>
        /// No Device
        /// </summary>
        NoDevice = -1,

        /// <summary>
        /// MS Controller
        /// </summary>
        MS = 0,

        /// <summary>
        /// Analog controller
        /// </summary>
        Analog = 1,

        /// <summary>
        /// A/D card controller
        /// </summary>
        AD_Card = 2,

        /// <summary>
        /// PDA controller
        /// </summary>
        PDA = 3,

        /// <summary>
        /// UV controller
        /// </summary>
        UV = 4
    }

    /// <summary>
    /// Intensity Cutoff Types
    /// </summary>
    /// <remarks>Used with <see cref="XRawFileIO.mXRawFile"/> functions in <see cref="XRawFileIO.GetScanData2D(int,out double[,],int,bool)"/> and <see cref="XRawFileIO.GetScanDataSumScans"/></remarks>
    [CLSCompliant(true)]
    public enum IntensityCutoffTypeConstants
    {
        /// <summary>
        /// All Values Returned
        /// </summary>
        None = 0,

        /// <summary>
        /// Absolute Intensity Units
        /// </summary>
        AbsoluteIntensityUnits = 1,

        /// <summary>
        /// Intensity relative to base peak
        /// </summary>
        RelativeToBasePeak = 2
    }

    /// <summary>
    /// Instrument Flags
    /// </summary>
    [CLSCompliant(true)]
    public class InstFlags
    {
        /// <summary>
        /// Total Ion Map
        /// </summary>
        public const string TIM = "Total Ion Map";

        /// <summary>
        /// Neutral Loss Map
        /// </summary>
        public const string NLM = "Neutral Loss Map";

        /// <summary>
        /// Parent Ion Map
        /// </summary>
        public const string PIM = "Parent Ion Map";

        /// <summary>
        /// Data Dependent ZoomScan Map
        /// </summary>
        public const string DDZMap = "Data Dependent ZoomScan Map";
    }
}
