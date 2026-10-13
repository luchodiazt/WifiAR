using UnityEngine;
using WifiAR.Wifi;

namespace WifiAR.Sampling
{
    public struct SignalSample
    {
        public Vector3 position;
        public int rssiDbm;
        public int frequencyMhz;
        public double time;
    }

    /// <summary>
    /// Picks which WiFi readings become points on the map.
    /// Android only refreshes RSSI about every 3 s (measured: 2.7-3.2 s on a Galaxy A56), so a reading
    /// is tied to the place where it first showed up instead of being smeared along the walk.
    /// </summary>
    public sealed class SignalSampler
    {
        /// <summary>Samples closer than this to the previous one add nothing but noise.</summary>
        public float MinDistance = 0.3f;
        /// <summary>If the value did not change for this long, assume the OS refreshed it with the same number.</summary>
        public double AssumedRefreshInterval = 3.2;

        bool hasReading;
        int lastRssi;
        double lastFreshTime;
        bool hasSample;
        Vector3 lastSamplePosition;

        public bool TryAccept(double now, Vector3 position, bool poseReliable, WifiReading reading, out SignalSample sample)
        {
            sample = default;

            bool fresh = !hasReading || reading.rssiDbm != lastRssi || now - lastFreshTime >= AssumedRefreshInterval;
            if (!fresh) return false;

            // The reading is spent even when it cannot be placed: it belongs to this moment, not a later one.
            hasReading = true;
            lastRssi = reading.rssiDbm;
            lastFreshTime = now;

            if (!poseReliable) return false;
            if (hasSample && HorizontalDistance(position, lastSamplePosition) < MinDistance) return false;

            hasSample = true;
            lastSamplePosition = position;
            sample = new SignalSample
            {
                position = position,
                rssiDbm = reading.rssiDbm,
                frequencyMhz = reading.frequencyMhz,
                time = now
            };
            return true;
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
