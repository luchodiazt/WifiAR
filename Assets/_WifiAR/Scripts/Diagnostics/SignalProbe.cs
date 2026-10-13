using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using WifiAR.Wifi;

namespace WifiAR.Diagnostics
{
    /// <summary>
    /// Phase 1 spike, not part of the shipped app. Shows the raw WiFi reading on screen and logs every
    /// poll to a CSV, so we can measure how often Android really refreshes RSSI and whether it is
    /// readable without location permission.
    /// </summary>
    public sealed class SignalProbe : MonoBehaviour
    {
        const string LogTag = "[WifiProbe]";

        [SerializeField] float pollInterval = 0.5f;

        IWifiSignalSource source;
        StreamWriter log;
        string logPath;
        string error;
        string hudText = "Iniciando...";
        GUIStyle hudStyle;

        float nextPoll;
        bool hasReading;
        int lastRssi;
        double lastChangeTime;
        int changeCount;
        double intervalSum;
        double intervalMin = double.MaxValue;
        double intervalMax;

        // AR pose sanity: a person walks, so anything faster than this is the tracker diverging.
        const float MaxPlausibleSpeed = 3f;

        ARSessionState lastArState;
        NotTrackingReason lastArReason;
        Vector3 lastCam;
        float lastCamTime = -1f;
        int poseJumps;

        void Awake()
        {
            try
            {
                source = Application.platform == RuntimePlatform.Android
                    ? new AndroidWifiSignalSource()
                    : new SimulatedWifiSignalSource(CameraPosition, Vector3.zero, () => Time.realtimeSinceStartupAsDouble);

                logPath = Path.Combine(Application.persistentDataPath, $"rssi_probe_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                log = new StreamWriter(logPath) { AutoFlush = true };
                log.WriteLine("time_s,rssi_dbm,freq_mhz,link_mbps,changed,cam_x,cam_y,cam_z,ar_state,ar_reason,cam_speed");
                Debug.Log($"{LogTag} logging to {logPath}");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        void Update()
        {
            if (source == null || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + pollInterval;

            try
            {
                Poll();
            }
            catch (Exception e)
            {
                // A SecurityException here is the answer to "do we need more permissions?", so surface it.
                Fail(e);
            }
        }

        void Poll()
        {
            Vector3 cam = CameraPosition();

            ARSessionState arState = ARSession.state;
            NotTrackingReason arReason = ARSession.notTrackingReason;
            if (arState != lastArState || arReason != lastArReason)
            {
                Debug.Log($"{LogTag} ar_state={arState} reason={arReason}");
                lastArState = arState;
                lastArReason = arReason;
            }

            float camSpeed = lastCamTime >= 0f
                ? Vector3.Distance(cam, lastCam) / Mathf.Max(Time.unscaledTime - lastCamTime, 0.01f)
                : 0f;
            lastCam = cam;
            lastCamTime = Time.unscaledTime;
            if (camSpeed > MaxPlausibleSpeed)
            {
                poseJumps++;
                Debug.LogWarning($"{LogTag} pose jump speed={camSpeed:F1} m/s ar_state={arState} reason={arReason}");
            }

            if (!source.TryRead(out WifiReading reading))
            {
                hasReading = false;
                hudText = "Sin conexion WiFi";
                return;
            }

            bool changed = hasReading && reading.rssiDbm != lastRssi;
            if (changed)
            {
                double interval = reading.timestamp - lastChangeTime;
                changeCount++;
                intervalSum += interval;
                intervalMin = Math.Min(intervalMin, interval);
                intervalMax = Math.Max(intervalMax, interval);
                Debug.Log($"{LogTag} rssi={reading.rssiDbm} freq={reading.frequencyMhz} link={reading.linkSpeedMbps} dt={interval:F2}s");
            }
            if (changed || !hasReading) lastChangeTime = reading.timestamp;

            hasReading = true;
            lastRssi = reading.rssiDbm;

            var inv = CultureInfo.InvariantCulture;
            log.WriteLine(string.Join(",",
                reading.timestamp.ToString("F2", inv),
                reading.rssiDbm.ToString(inv),
                reading.frequencyMhz.ToString(inv),
                reading.linkSpeedMbps.ToString(inv),
                changed ? "1" : "0",
                cam.x.ToString("F2", inv),
                cam.y.ToString("F2", inv),
                cam.z.ToString("F2", inv),
                arState.ToString(),
                arReason.ToString(),
                camSpeed.ToString("F2", inv)));

            string intervals = changeCount > 0
                ? $"prom {intervalSum / changeCount:F1} s (min {intervalMin:F1}, max {intervalMax:F1})"
                : "-";
            hudText =
                $"RSSI: {reading.rssiDbm} dBm\n" +
                $"Banda: {BandLabel(reading.Band)} ({reading.frequencyMhz} MHz)\n" +
                $"Enlace: {reading.linkSpeedMbps} Mbps\n" +
                $"Cambios: {changeCount}\n" +
                $"Intervalo: {intervals}\n" +
                $"Ultimo cambio hace {reading.timestamp - lastChangeTime:F1} s\n" +
                $"Camara: {cam.x:F1}, {cam.y:F1}, {cam.z:F1}\n" +
                $"AR: {arState} ({arReason})\n" +
                $"Saltos de pose: {poseJumps}";
        }

        void Fail(Exception e)
        {
            source = null;
            error = e.Message;
            Debug.LogError($"{LogTag} {e}");
        }

        static Vector3 CameraPosition()
        {
            Camera cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        static string BandLabel(WifiBand band)
        {
            switch (band)
            {
                case WifiBand.Band24GHz: return "2.4 GHz";
                case WifiBand.Band5GHz: return "5 GHz";
                case WifiBand.Band6GHz: return "6 GHz";
                default: return "?";
            }
        }

        // IMGUI on purpose: display only, needs no assets, canvas or input module.
        void OnGUI()
        {
            hudStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8),
                normal = { textColor = Color.white }
            };

            float scale = (Screen.dpi > 0f ? Screen.dpi : 160f) / 160f * 1.5f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            // Screen.safeArea is bottom-left based, IMGUI is top-left based.
            Rect safe = Screen.safeArea;
            float margin = 8f;
            var area = new Rect(
                safe.x / scale + margin,
                (Screen.height - safe.yMax) / scale + margin,
                safe.width / scale - margin * 2f,
                safe.height / scale - margin * 2f);

            GUILayout.BeginArea(area);
            GUILayout.Label(error != null ? $"ERROR\n{error}" : hudText, hudStyle);
            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            log?.Dispose();
            (source as IDisposable)?.Dispose();
        }
    }
}
