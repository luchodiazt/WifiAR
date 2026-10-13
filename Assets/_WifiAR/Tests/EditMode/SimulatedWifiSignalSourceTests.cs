using NUnit.Framework;
using UnityEngine;
using WifiAR.Wifi;

namespace WifiAR.Tests
{
    public class SimulatedWifiSignalSourceTests
    {
        [Test]
        public void RssiAtDistance_GetsWeakerWithDistance()
        {
            Assert.AreEqual(SimulatedWifiSignalSource.RssiAtOneMeter, SimulatedWifiSignalSource.RssiAtDistance(1f), 0.001f);
            Assert.Greater(SimulatedWifiSignalSource.RssiAtDistance(2f), SimulatedWifiSignalSource.RssiAtDistance(10f));
        }

        [Test]
        public void TryRead_HoldsValueUntilRefreshInterval()
        {
            Vector3 device = new Vector3(1f, 0f, 0f);
            double now = 0.0;
            var source = new SimulatedWifiSignalSource(() => device, Vector3.zero, () => now, refreshInterval: 3.0, noiseDb: 0f);

            Assert.IsTrue(source.TryRead(out WifiReading near));
            Assert.AreEqual(-40, near.rssiDbm);

            // Moved away, but the OS has not refreshed yet.
            device = new Vector3(10f, 0f, 0f);
            now = 1.0;
            source.TryRead(out WifiReading stale);
            Assert.AreEqual(near.rssiDbm, stale.rssiDbm);
            Assert.AreEqual(near.timestamp, stale.timestamp);

            now = 3.0;
            source.TryRead(out WifiReading far);
            Assert.AreEqual(-70, far.rssiDbm);
            Assert.AreEqual(3.0, far.timestamp);
        }
    }
}
