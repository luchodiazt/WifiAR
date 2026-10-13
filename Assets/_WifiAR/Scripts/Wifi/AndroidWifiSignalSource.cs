using System;
using UnityEngine;

namespace WifiAR.Wifi
{
    /// <summary>
    /// Reads the connected network's signal through android.net.wifi.WifiManager.
    /// Needs ACCESS_WIFI_STATE. RSSI, frequency and link speed are expected to work without
    /// location permission (only SSID/BSSID are location-gated).
    /// </summary>
    public sealed class AndroidWifiSignalSource : IWifiSignalSource, IDisposable
    {
        const int InvalidRssi = -127; // WifiInfo.INVALID_RSSI

        readonly AndroidJavaObject wifiManager;

        public AndroidWifiSignalSource()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
        }

        public bool TryRead(out WifiReading reading)
        {
            reading = default;

            // getConnectionInfo() is deprecated since API 31 but still works and is the simplest way to poll.
            using var info = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
            if (info == null) return false;

            int rssi = info.Call<int>("getRssi");
            if (rssi <= InvalidRssi || rssi >= 0) return false;

            reading = new WifiReading(
                rssi,
                info.Call<int>("getFrequency"),
                info.Call<int>("getLinkSpeed"),
                Time.realtimeSinceStartupAsDouble);
            return true;
        }

        public void Dispose() => wifiManager?.Dispose();
    }
}
