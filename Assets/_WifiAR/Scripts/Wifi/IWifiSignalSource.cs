namespace WifiAR.Wifi
{
    public interface IWifiSignalSource
    {
        /// <summary>Returns false when the device is not connected to a WiFi network.</summary>
        bool TryRead(out WifiReading reading);
    }
}
