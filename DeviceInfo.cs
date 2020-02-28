
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileReader
{
    /// <summary>
    /// Tracks information on the source device for data stored in a Thermo .raw file
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Instrument name (device name)
        /// </summary>
        public string InstrumentName { get; set; }

        /// <summary>
        /// Instrument model
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Instrument serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Acquisition software version
        /// </summary>
        public string SoftwareVersion { get; set; }

        /// <summary>
        /// Units for stored intensity data for this device
        /// </summary>
        public DataUnits Units { get; set; }

        /// <summary>
        /// X axis label for plotting data vs. scan
        /// </summary>
        public string AxisLabelX { get; set; }

        /// <summary>
        /// Y axis label for plotting data vs. scan
        /// </summary>
        public string AxisLabelY { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instrumentName"></param>
        public DeviceInfo(string instrumentName)
        {
            InstrumentName = instrumentName;
        }
    }
}
