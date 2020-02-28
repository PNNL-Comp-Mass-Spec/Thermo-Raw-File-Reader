using System;
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileReader
{
    /// <summary>
    /// Tracks information on the source device for data stored in a Thermo .raw file
    /// </summary>
    [CLSCompliant(true)]
    public class DeviceInfo
    {
        /// <summary>
        /// Device type
        /// </summary>
        public Device DeviceType { get; }

        /// <summary>
        /// Device number (for this device type)
        /// </summary>
        /// <remarks>Each device type starts with device number 1</remarks>
        public int DeviceNumber { get; }

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
        /// <param name="deviceType">Device type</param>
        /// <param name="deviceNumber">Device number (for this device type)</param>
        public DeviceInfo(Device deviceType, int deviceNumber)
        {
            DeviceType = deviceType;
            DeviceNumber = deviceNumber;
        }

        /// <summary>
        /// Display the device type and instrument model or name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Model))
                return string.Format("{0}: {1}", DeviceType.ToString(), Model);

            if (!string.IsNullOrWhiteSpace(InstrumentName))
                return string.Format("{0}: {1}", DeviceType.ToString(), InstrumentName);

            return string.Format("{0} device", DeviceType.ToString());
        }
    }
}
