using System;
using UnityEngine;

namespace WifiAR.Wifi
{
    /// <summary>
    /// Fake signal for the Editor: strength falls off with distance to a virtual router.
    /// Values only refresh every few seconds, like Android does, so sampling code is exercised realistically.
    /// </summary>
    public sealed class SimulatedWifiSignalSource : IWifiSignalSource
    {
        // Log-distance path loss: rssi(d) = RssiAtOneMeter - 10 * PathLossExponent * log10(d)
        public const float RssiAtOneMeter = -40f;
        public const float PathLossExponent = 3f;

        const int FrequencyMhz = 5180;
        const int LinkSpeedMbps = 433;

        readonly Func<Vector3> devicePosition;
        readonly Func<double> clock;
        readonly double refreshInterval;
        readonly float noiseDb;
        readonly System.Random random;

        WifiReading last;
        double lastRefresh = double.NegativeInfinity;

        public Vector3 RouterPosition { get; set; }

        public SimulatedWifiSignalSource(
            Func<Vector3> devicePosition,
            Vector3 routerPosition,
            Func<double> clock,
            double refreshInterval = 3.0,
            float noiseDb = 2f,
            int seed = 0)
        {
            this.devicePosition = devicePosition;
            RouterPosition = routerPosition;
            this.clock = clock;
            this.refreshInterval = refreshInterval;
            this.noiseDb = noiseDb;
            random = new System.Random(seed);
        }

        public bool TryRead(out WifiReading reading)
        {
            double now = clock();
            if (now - lastRefresh >= refreshInterval)
            {
                float distance = Vector3.Distance(devicePosition(), RouterPosition);
                float noise = ((float)random.NextDouble() * 2f - 1f) * noiseDb;
                int rssi = Mathf.RoundToInt(Mathf.Clamp(RssiAtDistance(distance) + noise, -100f, -20f));
                last = new WifiReading(rssi, FrequencyMhz, LinkSpeedMbps, now);
                lastRefresh = now;
            }

            reading = last;
            return true;
        }

        public static float RssiAtDistance(float meters)
        {
            return RssiAtOneMeter - 10f * PathLossExponent * Mathf.Log10(Mathf.Max(meters, 0.1f));
        }
    }
}
