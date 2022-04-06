using System;
using System.Collections.Generic;
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileReader
{
    // Ignore Spelling: cid, ETciD, etd, EThcD, hcd, sa, struct, Username

    /// <summary>
    /// Type for Tune Methods
    /// </summary>
    [CLSCompliant(true)]
    public class TuneMethod
    {
        /// <summary>
        /// Settings
        /// </summary>
        public readonly List<TuneMethodSettingType> Settings = new();
    }

    /// <summary>
    /// Type for Tune Method Settings
    /// </summary>
    [CLSCompliant(true)]
    public struct TuneMethodSettingType
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

        /// <summary>
        /// Display the category, name, and value of this setting
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0,-20}  {1,-40} = {2}", (Category ?? "Undefined") + ":", Name ?? string.Empty, Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Type for File Information
    /// </summary>
    [CLSCompliant(true)]
    public class RawFileInfo
    {
        /// <summary>
        /// Date of Acquisition
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string AcquisitionDate;

        /// <summary>
        /// Acquisition Filename
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string AcquisitionFilename;

        /// <summary>
        /// First Comment
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string Comment1;

        /// <summary>
        /// Second Comment
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string Comment2;

        /// <summary>
        /// Sample Name
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string SampleName;

        /// <summary>
        /// Sample Comment
        /// </summary>
        /// <remarks>Will often be blank</remarks>
        public string SampleComment;

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreationDate;

        /// <summary>
        /// Username of the user when the file was created
        /// </summary>
        public string CreatorID;

        /// <summary>
        /// Dictionary tracking the device data stored in the .raw file
        /// Keys are Device type, values are the number of devices of this type
        /// </summary>
        /// <remarks>
        /// Typically a .raw file has a single device, of type Device.MS
        /// Some .raw files also have LC information, stored as Device.Analog or Device.UV
        /// </remarks>
        public readonly Dictionary<Device, int> Devices;

        /// <summary>
        /// Instrument Flags
        /// </summary>
        /// <remarks>Values should be one of the constants in the InstFlags class (file Enums.cs)</remarks>
        public string InstFlags;

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
        /// <remarks>Typically only have one instrument method; the length of this array defines the number of instrument methods</remarks>
        public readonly List<string> InstMethods;

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
        /// <remarks>Typically only defined for instruments converted from other formats</remarks>
        public string InstrumentDescription;

        /// <summary>
        /// Instrument Serial Number
        /// </summary>
        public string InstSerialNumber;

        /// <summary>
        /// Tune Methods
        /// </summary>
        /// <remarks>Typically have one or two tune methods; the length of this array defines the number of tune methods defined</remarks>
        public readonly List<TuneMethod> TuneMethods;

        /// <summary>
        /// File format Version Number
        /// </summary>
        public int VersionNumber;

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
        /// <remarks>Auto-set to true if the file is corrupt / has no data</remarks>
        public bool CorruptFile;

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
            Devices.Clear();
            InstFlags = string.Empty;
            InstHardwareVersion = string.Empty;
            InstSoftwareVersion = string.Empty;
            InstMethods.Clear();
            InstModel = string.Empty;
            InstName = string.Empty;
            InstrumentDescription = string.Empty;
            InstSerialNumber = string.Empty;
            TuneMethods.Clear();

            VersionNumber = 0;
            MassResolution = 0;
            ScanStart = 0;
            ScanEnd = 0;

            CorruptFile = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RawFileInfo()
        {
            AcquisitionDate = string.Empty;
            AcquisitionFilename = string.Empty;
            Comment1 = string.Empty;
            Comment2 = string.Empty;
            SampleName = string.Empty;
            SampleComment = string.Empty;
            CreationDate = DateTime.MinValue;

            CreatorID = string.Empty;
            Devices = new Dictionary<Device, int>();
            InstFlags = string.Empty;
            InstHardwareVersion = string.Empty;
            InstSoftwareVersion = string.Empty;
            InstMethods = new List<string>();
            InstModel = string.Empty;
            InstName = string.Empty;
            InstrumentDescription = string.Empty;
            InstSerialNumber = string.Empty;
            TuneMethods = new List<TuneMethod>();

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
    [CLSCompliant(true)]
    public struct MRMMassRangeType
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
        public override string ToString()
        {
            return StartMass.ToString("0.000") + "-" + EndMass.ToString("0.000");
        }
    }

    /// <summary>
    /// Type for MRM Information
    /// </summary>
    [CLSCompliant(true)]
    public class MRMInfo
    {
        /// <summary>
        /// List of mass ranges monitored by the first quadrupole
        /// </summary>
        public readonly List<MRMMassRangeType> MRMMassList = new();

        /// <summary>
        /// Duplicate the MRM info
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        // ReSharper disable once UnusedMember.Global
        public static void DuplicateMRMInfo(MRMInfo source, out MRMInfo target)
        {
            target = new MRMInfo();

            target.MRMMassList.AddRange(source.MRMMassList);
        }
    }

    /// <summary>
    /// Type for storing Parent Ion Information
    /// </summary>
    [CLSCompliant(true)]
    public struct ParentIonInfoType
    {
        /// <summary>
        /// MS Level of the spectrum
        /// </summary>
        /// <remarks>1 for MS1 spectra, 2 for MS2, 3 for MS3</remarks>
        public int MSLevel;

        /// <summary>
        /// Parent ion m/z
        /// </summary>
        public double ParentIonMZ;

        /// <summary>
        /// Collision mode
        /// </summary>
        /// <remarks>Examples: cid, etd, hcd, EThcD, ETciD</remarks>
        public string CollisionMode;

        /// <summary>
        /// Secondary collision mode
        /// </summary>
        /// <remarks>
        /// For example, for filter string: ITMS + c NSI r d sa Full ms2 1143.72@etd120.55@cid20.00 [120.00-2000.00]
        /// CollisionMode = ETciD
        /// CollisionMode2 = cid
        /// </remarks>
        public string CollisionMode2;

        /// <summary>
        /// Collision energy
        /// </summary>
        public float CollisionEnergy;

        /// <summary>
        /// Secondary collision energy
        /// </summary>
        /// <remarks>
        /// For example, for filter string: ITMS + c NSI r d sa Full ms2 1143.72@etd120.55@cid20.00 [120.00-2000.00]
        /// CollisionEnergy = 120.55
        /// CollisionEnergy2 = 20.0
        /// </remarks>
        public float CollisionEnergy2;

        /// <summary>
        /// Activation type
        /// </summary>
        /// <remarks>Examples: CID, ETD, or HCD</remarks>
        public ActivationTypeConstants ActivationType;

        /// <summary>
        /// Clear the data
        /// </summary>
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

        /// <summary>
        /// Return a simple summary of the object
        /// </summary>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(CollisionMode))
            {
                return "ms" + MSLevel + " " + ParentIonMZ.ToString("0.0#");
            }

            return "ms" + MSLevel + " " + ParentIonMZ.ToString("0.0#") + "@" + CollisionMode + CollisionEnergy.ToString("0.00");
        }
    }

    /// <summary>
    /// Type for Mass Precision Information
    /// </summary>
    [CLSCompliant(true)]
    public struct MassPrecisionInfoType
    {
        /// <summary>
        /// Peak Intensity
        /// </summary>
        public double Intensity;

        /// <summary>
        /// Peak Mass
        /// </summary>
        public double Mass;

        /// <summary>
        /// Peak Accuracy (in MMU)
        /// </summary>
        public double AccuracyMMU;

        /// <summary>
        /// Peak Accuracy (in PPM)
        /// </summary>
        public double AccuracyPPM;

        /// <summary>
        /// Peak Resolution
        /// </summary>
        public double Resolution;
    }

    /// <summary>
    /// Type for storing FT Label Information
    /// </summary>
    [CLSCompliant(true)]
    public struct FTLabelInfoType
    {
        /// <summary>
        /// Peak m/z
        /// </summary>
        /// <remarks>This is observed m/z; it is not monoisotopic mass</remarks>
        public double Mass;

        /// <summary>
        /// Peak Intensity
        /// </summary>
        public double Intensity;

        /// <summary>
        /// Peak Resolution
        /// </summary>
        public float Resolution;

        /// <summary>
        /// Peak Baseline
        /// </summary>
        public float Baseline;

        /// <summary>
        /// Peak Noise
        /// </summary>
        /// <remarks>For signal/noise ratio, see SignalToNoise</remarks>
        public float Noise;

        /// <summary>
        /// Peak Charge
        /// </summary>
        /// <remarks>Will be 0 if the charge could not be determined</remarks>
        public int Charge;

        /// <summary>
        /// Signal to noise ratio
        /// </summary>
        /// <returns>Intensity divided by noise, or 0 if Noise is 0</returns>
        public double SignalToNoise
        {
            get
            {
                if (Noise > 0)
                    return Intensity / Noise;

                return 0;
            }
        }

        /// <summary>
        /// Return a summary of this object
        /// </summary>
        public override string ToString()
        {
            return string.Format("m/z {0,9:F3}, S/N {1,7:F1}, intensity {2,12:F0}", Mass, SignalToNoise, Intensity);
        }
    }
}
