using NUnit.Framework;
using WifiAR.Sampling;

namespace WifiAR.Tests
{
    public class SignalQualityTests
    {
        [TestCase(-30, 1f)]
        [TestCase(-60, 0.5f)]
        [TestCase(-90, 0f)]
        [TestCase(-20, 1f)]
        [TestCase(-100, 0f)]
        public void Normalize_MapsDbmRangeToZeroOne(int rssi, float expected)
        {
            Assert.AreEqual(expected, SignalQuality.Normalize(rssi), 0.001f);
        }

        [TestCase(-27, SignalLevel.Excellent)]
        [TestCase(-50, SignalLevel.Excellent)]
        [TestCase(-51, SignalLevel.Good)]
        [TestCase(-67, SignalLevel.Fair)]
        [TestCase(-77, SignalLevel.Weak)]
        [TestCase(-81, SignalLevel.Bad)]
        public void LevelOf_UsesCommonWifiThresholds(int rssi, SignalLevel expected)
        {
            Assert.AreEqual(expected, SignalQuality.LevelOf(rssi));
        }

        [Test]
        public void ColorOf_RunsWeakThroughMidToStrong()
        {
            // Color's == is tolerant; Equals (what Assert.AreEqual uses) is bit-exact and trips on lerp rounding.
            Assert.IsTrue(SignalQuality.WeakColor == SignalQuality.ColorOf(0f));
            Assert.IsTrue(SignalQuality.MidColor == SignalQuality.ColorOf(0.5f));
            Assert.IsTrue(SignalQuality.StrongColor == SignalQuality.ColorOf(1f));
        }
    }
}
