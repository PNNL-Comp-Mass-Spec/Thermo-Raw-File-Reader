
namespace ThermoRawFileReader
{
    public class ThermoReaderOptions
    {

        /// <summary>
        /// MS Method Information
        /// </summary>
        /// <remarks>
        /// Set this to false when using the ThermoRawFileReader on Linux systems;
        /// CommonCore.RawFileReader raises an exception due to a null value when accessing
        /// get_StorageDescriptions from get_InstrumentMethodsCount; stack trace:
        ///   ThermoRawFileReader.XRawFileIO.FillFileInfo
        ///   ThermoFisher.CommonCore.RawFileReader.RawFileAccessBase.get_InstrumentMethodsCount
        ///   ThermoFisher.CommonCore.RawFileReader.StructWrappers.Method.get_StorageDescriptions
        /// </remarks>
        public bool LoadMSMethodInfo { get; set; } = true;

        /// <summary>
        /// MS Tune Information
        /// </summary>
        public bool LoadMSTuneInfo { get; set; } = true;

    }
}
