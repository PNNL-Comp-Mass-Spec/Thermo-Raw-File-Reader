
namespace ThermoRawFileReader
{
    /// <summary>
    /// Thermo reader options
    /// </summary>
    public class ThermoReaderOptions
    {
        #region "Events"

        /// <summary>
        /// Delegate method for OptionsUpdatedEvent
        /// </summary>
        /// <param name="sender"></param>
        public delegate void OptionsUpdatedEventHandler(object sender);

        /// <summary>
        /// This event is raised when one of the options tracked by this class is changed
        /// </summary>
        public event OptionsUpdatedEventHandler OptionsUpdatedEvent;

        #endregion

        #region "Member variables"

        private bool mIncludeReferenceAndExceptionData;

        #endregion

        #region "Properties"

        /// <summary>
        /// When true, include reference and exception peaks when obtaining mass spec data
        /// using GetScanData, GetScanData2D, or GetScanDataSumScans
        /// </summary>
        /// <remarks>Reference and exception peaks are internal mass calibration data within a scan</remarks>
        public bool IncludeReferenceAndExceptionData
        {
            get => mIncludeReferenceAndExceptionData;
            set
            {
                if (mIncludeReferenceAndExceptionData == value)
                    return;

                mIncludeReferenceAndExceptionData = value;
                OptionsUpdatedEvent?.Invoke(this);
            }
        }

        /// <summary>
        /// Load MS Method Information when calling OpenRawFile
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
        /// Load MS Tune Information when calling OpenRawFile
        /// </summary>
        public bool LoadMSTuneInfo { get; set; } = true;

        #endregion

    }
}
