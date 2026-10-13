using System;

namespace WifiAR.Wifi
{
    public enum WifiBand
    {
        Unknown,
        Band24GHz,
        Band5GHz,
        Band6GHz
    }

    /// <summary>Signal of the WiFi network the device is connected to, at one instant.</summary>
    [Serializable]
    public struct WifiReading
    {
        public int rssiDbm;
        public int frequencyMhz;
        public int linkSpeedMbps;
        /// <summary>Seconds since app start when the value was obtained.</summary>
        public double timestamp;

        public WifiReading(int rssiDbm, int frequencyMhz, int linkSpeedMbps, double timestamp)
        {
            this.rssiDbm = rssiDbm;
            this.frequencyMhz = frequencyMhz;
            this.linkSpeedMbps = linkSpeedMbps;
            this.timestamp = timestamp;
        }

        public WifiBand Band => BandFromFrequency(frequencyMhz);

        public static WifiBand BandFromFrequency(int mhz)
        {
            if (mhz >= 2400 && mhz < 2500) return WifiBand.Band24GHz;
            if (mhz >= 4900 && mhz < 5925) return WifiBand.Band5GHz;
            if (mhz >= 5925 && mhz <= 7125) return WifiBand.Band6GHz;
            return WifiBand.Unknown;
        }
    }
}
