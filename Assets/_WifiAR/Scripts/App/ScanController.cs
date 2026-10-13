using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WifiAR.Sampling;
using WifiAR.UI;
using WifiAR.Visualization;
using WifiAR.Wifi;

namespace WifiAR.App
{
    /// <summary>
    /// Open and see. The screen always shows the live signal as waves, with nothing to set up.
    /// Whenever AR tracking happens to be good, readings are also left on the floor as a wave field
    /// that spreads from the strongest spot found so far (in practice, the router).
    /// Nothing ever blocks on tracking: with poor light or bare floors the app is still useful.
    /// </summary>
    public sealed class ScanController : MonoBehaviour
    {
        const float PollInterval = 0.5f;
        // Used for the floor until ARCore finds the real one.
        const float AssumedPhoneHeight = 1.3f;
        const float MinHeightAboveFloor = 0.5f;
        const float SignalSmoothing = 0.5f;
        // After this long without a trustworthy pose ARCore may have started a new coordinate frame,
        // so old floor samples can no longer be placed relative to new ones.
        const double TrackingGapThatResetsField = 5.0;
        // RSSI wobbles by a dB or two; a new spot must clearly beat the best one to become the wave origin.
        const int StrongestSpotMarginDb = 2;
        // The floor field takes over when it is visible; the overlay stays as a quiet pulse.
        const float OverlayIntensityOverField = 0.35f;

        [SerializeField] Camera arCamera;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] WaveFieldRenderer waveField;
        [SerializeField] LiveWaveOverlay liveWaves;
        [SerializeField] ScanHud hud;

        readonly PoseValidator poseValidator = new PoseValidator();
        readonly SignalSampler sampler = new SignalSampler();
        IWifiSignalSource source;
        string sourceError;

        float nextPoll;
        bool hasReading;
        WifiReading reading;
        float smoothedSignal;

        double lastReliableTime = double.NegativeInfinity;
        bool fieldNeedsRestart;
        int strongestRssi;
        bool floorFound;
        float floorY;

        bool loggedReliable;
        ARSessionState loggedState;

        void Start()
        {
            // The user walks around watching the screen without touching it; a dimmed or locked
            // screen stops the camera and with it the AR session.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            try
            {
                source = Application.platform == RuntimePlatform.Android
                    ? new AndroidWifiSignalSource()
                    : new SimulatedWifiSignalSource(
                        () => arCamera.transform.position, Vector3.zero, () => Time.unscaledTimeAsDouble);
            }
            catch (Exception e)
            {
                sourceError = e.Message;
                Debug.LogException(e);
            }
        }

        void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            Vector3 cameraPosition = arCamera.transform.position;
            bool tracking = ARSession.state == ARSessionState.SessionTracking;
            bool poseReliable = poseValidator.Update(now, cameraPosition, tracking);
            LogTrackingChanges(poseReliable);

            if (poseReliable)
            {
                if (now - lastReliableTime > TrackingGapThatResetsField) fieldNeedsRestart = true;
                lastReliableTime = now;
            }

            if (source != null && Time.unscaledTime >= nextPoll)
            {
                nextPoll = Time.unscaledTime + PollInterval;
                PollWifi(now, cameraPosition, poseReliable);
            }

            // Without a trustworthy pose the field would be drawn in the wrong place, so it hides.
            bool fieldVisible = waveField.IsActive && !fieldNeedsRestart && poseReliable;
            waveField.SetVisible(fieldVisible);
            if (fieldVisible) RefineFloor(cameraPosition);

            liveWaves.Show(smoothedSignal, BandValue(reading.frequencyMhz),
                !hasReading ? 0f : fieldVisible ? OverlayIntensityOverField : 1f);
            UpdateHud(fieldVisible);
        }

        // Tracking problems only show up on a real device, so development builds leave a trail in logcat.
        void LogTrackingChanges(bool poseReliable)
        {
            if (!Debug.isDebugBuild) return;
            if (poseReliable == loggedReliable && ARSession.state == loggedState) return;

            loggedReliable = poseReliable;
            loggedState = ARSession.state;
            Debug.Log($"[WifiAR] pose reliable={poseReliable} session={ARSession.state} " +
                $"reason={ARSession.notTrackingReason} lastFault={poseValidator.LastFault}");
        }

        void PollWifi(double now, Vector3 cameraPosition, bool poseReliable)
        {
            try
            {
                bool hadReading = hasReading;
                hasReading = source.TryRead(out reading);
                if (!hasReading) return;

                float signal = SignalQuality.Normalize(reading.rssiDbm);
                smoothedSignal = hadReading ? Mathf.Lerp(smoothedSignal, signal, SignalSmoothing) : signal;

                // Always offered to the sampler: a reading seen while the pose was bad must not be
                // placed later, somewhere else.
                bool accepted = sampler.TryAccept(now, cameraPosition, poseReliable, reading, out SignalSample sample);
                if (!poseReliable) return;
                if (!waveField.IsActive || fieldNeedsRestart) BeginField(cameraPosition);

                if (accepted)
                {
                    waveField.AddSample(sample.position, SignalQuality.Normalize(sample.rssiDbm),
                        BandValue(sample.frequencyMhz));

                    if (sample.rssiDbm >= strongestRssi + StrongestSpotMarginDb)
                    {
                        strongestRssi = sample.rssiDbm;
                        waveField.SetWaveOrigin(sample.position);
                    }
                }
            }
            catch (Exception e)
            {
                source = null;
                sourceError = e.Message;
                Debug.LogException(e);
            }
        }

        void BeginField(Vector3 cameraPosition)
        {
            floorFound = TryFindFloor(cameraPosition, out floorY);
            if (!floorFound) floorY = cameraPosition.y - AssumedPhoneHeight;

            strongestRssi = int.MinValue / 2;
            fieldNeedsRestart = false;
            waveField.Begin(cameraPosition, floorY);
        }

        void RefineFloor(Vector3 cameraPosition)
        {
            if (!TryFindFloor(cameraPosition, out float y)) return;

            // The first real plane replaces the guess; after that only a lower one can be the floor.
            if (floorFound && y >= floorY) return;
            floorFound = true;
            floorY = y;
            waveField.SetFloorHeight(floorY);
        }

        bool TryFindFloor(Vector3 cameraPosition, out float y)
        {
            y = float.PositiveInfinity;
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                if (plane.trackingState != TrackingState.Tracking) continue;

                float planeY = plane.transform.position.y;
                // Tables and shelves are horizontal too; the floor is well below the phone.
                if (planeY < cameraPosition.y - MinHeightAboveFloor && planeY < y) y = planeY;
            }
            return !float.IsPositiveInfinity(y);
        }

        void UpdateHud(bool fieldVisible)
        {
            if (sourceError != null)
            {
                hud.Show("No se pudo leer el WiFi", sourceError, SignalQuality.WeakColor);
                return;
            }
            if (!hasReading)
            {
                hud.Show("Sin WiFi", "Conéctate a tu red para ver la señal", Color.white);
                return;
            }

            string tip = fieldVisible
                ? "Camina y mira el rastro en el piso"
                : ARSession.notTrackingReason == NotTrackingReason.InsufficientLight
                    ? "Con más luz verás el rastro en el piso"
                    : "Camina por tu casa y mira cómo cambia";

            string label = SignalQuality.Label(SignalQuality.LevelOf(reading.rssiDbm));
            hud.Show($"Señal {label}", $"{BandLabel(reading.Band)} · {tip}", SignalQuality.ColorOf(smoothedSignal));
        }

        static float BandValue(int frequencyMhz)
        {
            return WifiReading.BandFromFrequency(frequencyMhz) == WifiBand.Band24GHz ? 0f : 1f;
        }

        static string BandLabel(WifiBand band)
        {
            switch (band)
            {
                case WifiBand.Band24GHz: return "2.4 GHz";
                case WifiBand.Band5GHz: return "5 GHz";
                case WifiBand.Band6GHz: return "6 GHz";
                default: return "WiFi";
            }
        }
    }
}
