
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileReader
{
    public class DeviceInfo
    {
        public string InstrumentName { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string SoftwareVersion { get; set; }
        public DataUnits Units { get; set; }

        public string AxisLabelX { get; set; }
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
