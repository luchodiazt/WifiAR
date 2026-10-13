using NUnit.Framework;
using UnityEngine;
using WifiAR.Sampling;
using WifiAR.Wifi;

namespace WifiAR.Tests
{
    public class SignalSamplerTests
    {
        static WifiReading Reading(int rssi) => new WifiReading(rssi, 5180, 433, 0.0);

        [Test]
        public void FirstReading_IsAccepted()
        {
            var sampler = new SignalSampler();

            Assert.IsTrue(sampler.TryAccept(0.0, Vector3.zero, true, Reading(-40), out SignalSample sample));
            Assert.AreEqual(-40, sample.rssiDbm);
            Assert.AreEqual(5180, sample.frequencyMhz);
        }

        [Test]
        public void UnchangedReading_IsStaleUntilAssumedRefresh()
        {
            var sampler = new SignalSampler();
            sampler.TryAccept(0.0, Vector3.zero, true, Reading(-40), out _);

            // Walked on, but the OS is still handing out the old value.
            Assert.IsFalse(sampler.TryAccept(1.0, new Vector3(1f, 0f, 0f), true, Reading(-40), out _));
            Assert.IsTrue(sampler.TryAccept(3.2, new Vector3(2f, 0f, 0f), true, Reading(-40), out _));
        }

        [Test]
        public void ChangedReading_IsAcceptedAtOnce()
        {
            var sampler = new SignalSampler();
            sampler.TryAccept(0.0, Vector3.zero, true, Reading(-40), out _);

            Assert.IsTrue(sampler.TryAccept(0.5, new Vector3(1f, 0f, 0f), true, Reading(-45), out SignalSample sample));
            Assert.AreEqual(-45, sample.rssiDbm);
        }

        [Test]
        public void ReadingWithoutMovement_IsRejected()
        {
            var sampler = new SignalSampler();
            sampler.TryAccept(0.0, Vector3.zero, true, Reading(-40), out _);

            // Height does not count: the map lives on the floor.
            Assert.IsFalse(sampler.TryAccept(0.5, new Vector3(0.1f, 1f, 0.1f), true, Reading(-45), out _));
        }

        [Test]
        public void ReadingDuringUnreliablePose_IsSpentNotDeferred()
        {
            var sampler = new SignalSampler();
            sampler.TryAccept(0.0, Vector3.zero, true, Reading(-40), out _);

            Assert.IsFalse(sampler.TryAccept(0.5, new Vector3(1f, 0f, 0f), false, Reading(-60), out _));
            // Tracking is back, but -60 was measured somewhere we could not locate.
            Assert.IsFalse(sampler.TryAccept(1.0, new Vector3(2f, 0f, 0f), true, Reading(-60), out _));
        }
    }
}
