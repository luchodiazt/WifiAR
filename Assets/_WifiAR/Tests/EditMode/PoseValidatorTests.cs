using NUnit.Framework;
using UnityEngine;
using WifiAR.Sampling;

namespace WifiAR.Tests
{
    public class PoseValidatorTests
    {
        const double Frame = 1.0 / 30.0;

        [Test]
        public void WalkingPace_IsReliable()
        {
            var validator = new PoseValidator();

            for (int i = 0; i < 60; i++)
            {
                float metersWalked = 1.4f * (float)(i * Frame);
                Assert.IsTrue(validator.Update(i * Frame, new Vector3(metersWalked, 0f, 0f), true), $"frame {i}");
            }
        }

        [Test]
        public void FrameTimeJitter_DoesNotLookLikeAJump()
        {
            // Regression: on device a normal 2 cm pose update sometimes lands on a 2 ms frame.
            var validator = new PoseValidator();
            double now = 0.0;
            float x = 0f;

            for (int i = 0; i < 90; i++)
            {
                now += i % 3 == 0 ? 0.002 : 0.045;
                x += 0.02f;
                Assert.IsTrue(validator.Update(now, new Vector3(x, 0f, 0f), true), $"frame {i}");
            }
        }

        [Test]
        public void NotTracking_IsUnreliable()
        {
            var validator = new PoseValidator();

            Assert.IsFalse(validator.Update(0.0, Vector3.zero, false));
            Assert.AreEqual(PoseFault.NotTracking, validator.LastFault);
        }

        [Test]
        public void ImpossibleJump_IsUnreliableEvenWhileSessionSaysTracking()
        {
            var validator = new PoseValidator();
            validator.Update(0.0, Vector3.zero, true);

            // From the phase 1 probe: 42 m/s at the start of a divergence, i.e. 1.4 m in one frame.
            Assert.IsFalse(validator.Update(Frame, new Vector3(1.4f, 0f, 0f), true));
            Assert.AreEqual(PoseFault.Jump, validator.LastFault);
        }

        [Test]
        public void SustainedDriftFasterThanAPersonMoves_IsUnreliable()
        {
            var validator = new PoseValidator();
            bool reliable = true;

            // 4.5 m/s in steps too small to count as a jump.
            for (int i = 0; i < 30; i++) reliable = validator.Update(i * Frame, new Vector3(0.15f * i, 0f, 0f), true);

            Assert.IsFalse(reliable);
            Assert.AreEqual(PoseFault.TooFast, validator.LastFault);
        }

        [Test]
        public void AfterFault_PoseMustSettleBeforeBeingTrusted()
        {
            var validator = new PoseValidator();
            validator.Update(0.0, Vector3.zero, true);
            validator.Update(0.5, Vector3.zero, false);

            Assert.IsFalse(validator.Update(1.0, Vector3.zero, true));
            Assert.IsTrue(validator.Update(1.5, Vector3.zero, true));
        }

        [Test]
        public void PoseFarFromOrigin_IsUnreliable()
        {
            var validator = new PoseValidator();

            Assert.IsFalse(validator.Update(0.0, new Vector3(853.9f, 100.8f, 1045.5f), true));
            Assert.AreEqual(PoseFault.TooFar, validator.LastFault);
        }
    }
}
