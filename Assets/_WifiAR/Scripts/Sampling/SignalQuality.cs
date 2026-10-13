using UnityEngine;

namespace WifiAR.Sampling
{
    public enum SignalLevel
    {
        Bad,
        Weak,
        Fair,
        Good,
        Excellent
    }

    /// <summary>Turns raw dBm into what the user actually sees: a 0..1 strength, a level and a color.</summary>
    public static class SignalQuality
    {
        // Range measured indoors during the phase 1 probe: -27 dBm next to the router, -77 dBm at the far end.
        public const int MinDbm = -90;
        public const int MaxDbm = -30;

        // Distinct in hue and brightness so the scale survives red-green color blindness.
        public static readonly Color WeakColor = new Color32(229, 72, 77, 255);
        public static readonly Color MidColor = new Color32(255, 197, 61, 255);
        public static readonly Color StrongColor = new Color32(48, 213, 200, 255);

        public static float Normalize(int rssiDbm)
        {
            return Mathf.InverseLerp(MinDbm, MaxDbm, rssiDbm);
        }

        public static SignalLevel LevelOf(int rssiDbm)
        {
            if (rssiDbm >= -50) return SignalLevel.Excellent;
            if (rssiDbm >= -60) return SignalLevel.Good;
            if (rssiDbm >= -70) return SignalLevel.Fair;
            if (rssiDbm >= -80) return SignalLevel.Weak;
            return SignalLevel.Bad;
        }

        public static string Label(SignalLevel level)
        {
            switch (level)
            {
                case SignalLevel.Excellent: return "excelente";
                case SignalLevel.Good: return "buena";
                case SignalLevel.Fair: return "aceptable";
                case SignalLevel.Weak: return "débil";
                default: return "mala";
            }
        }

        /// <summary>Same ramp the wave shader uses, so HUD and floor always agree.</summary>
        public static Color ColorOf(float signal01)
        {
            signal01 = Mathf.Clamp01(signal01);
            return signal01 < 0.5f
                ? Color.Lerp(WeakColor, MidColor, signal01 * 2f)
                : Color.Lerp(MidColor, StrongColor, signal01 * 2f - 1f);
        }
    }
}
