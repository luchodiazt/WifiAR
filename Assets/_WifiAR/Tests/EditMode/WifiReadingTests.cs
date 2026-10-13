using NUnit.Framework;
using WifiAR.Wifi;

namespace WifiAR.Tests
{
    public class WifiReadingTests
    {
        [TestCase(2412, WifiBand.Band24GHz)]
        [TestCase(2484, WifiBand.Band24GHz)]
        [TestCase(5180, WifiBand.Band5GHz)]
        [TestCase(5825, WifiBand.Band5GHz)]
        [TestCase(5955, WifiBand.Band6GHz)]
        [TestCase(7115, WifiBand.Band6GHz)]
        [TestCase(-1, WifiBand.Unknown)]
        [TestCase(0, WifiBand.Unknown)]
        public void BandFromFrequency_MapsChannelFrequencyToBand(int mhz, WifiBand expected)
        {
            Assert.AreEqual(expected, WifiReading.BandFromFrequency(mhz));
        }
    }
}
