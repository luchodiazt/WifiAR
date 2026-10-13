using UnityEngine;

namespace WifiAR.Sampling
{
    public enum PoseFault
    {
        None,
        NotTracking,
        TooFar,
        Jump,
        TooFast
    }

    /// <summary>
    /// Decides whether the AR camera pose can be trusted right now.
    /// ARCore keeps reporting "tracking" for a few frames while its estimate flies away (seen at
    /// hundreds of m/s in the phase 1 probe), so the session state alone is not enough.
    /// </summary>
    public sealed class PoseValidator
    {
        /// <summary>Farthest the phone can plausibly move between two frames; a diverging tracker covers more.</summary>
        public float MaxStep = 0.5f;
        /// <summary>
        /// Averaged over <see cref="SpeedWindow"/>, never per frame: pose updates arrive at camera rate
        /// while frame times jitter, so a 2 cm update landing on a 2 ms frame would read as 10 m/s.
        /// </summary>
        public float MaxSpeed = 3f;
        public double SpeedWindow = 0.5;
        /// <summary>A house does not need more; guards against a pose that drifted far and then stopped.</summary>
        public float MaxDistanceFromOrigin = 200f;
        /// <summary>Seconds the pose must behave after a fault before it is trusted again.</summary>
        public double SettleTime = 1.0;

        bool hasLast;
        Vector3 lastPosition;
        Vector3 windowPosition;
        double windowTime;
        double unreliableUntil;

        /// <summary>Why the pose was last rejected. Kept after recovery, for diagnostics.</summary>
        public PoseFault LastFault { get; private set; }

        public bool Update(double now, Vector3 position, bool sessionTracking)
        {
            PoseFault fault = PoseFault.None;

            if (!sessionTracking)
            {
                fault = PoseFault.NotTracking;
            }
            else if (position.magnitude > MaxDistanceFromOrigin)
            {
                fault = PoseFault.TooFar;
            }
            else if (hasLast && Vector3.Distance(position, lastPosition) > MaxStep)
            {
                fault = PoseFault.Jump;
            }
            else if (hasLast && now - windowTime >= SpeedWindow)
            {
                if (Vector3.Distance(position, windowPosition) / (now - windowTime) > MaxSpeed) fault = PoseFault.TooFast;
                windowPosition = position;
                windowTime = now;
            }

            if (!hasLast || fault != PoseFault.None)
            {
                windowPosition = position;
                windowTime = now;
            }

            hasLast = true;
            lastPosition = position;

            if (fault != PoseFault.None)
            {
                LastFault = fault;
                unreliableUntil = now + SettleTime;
            }
            return now >= unreliableUntil;
        }
    }
}
