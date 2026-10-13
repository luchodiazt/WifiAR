using UnityEngine;
using UnityEngine.UI;
using WifiAR.Sampling;

namespace WifiAR.Visualization
{
    /// <summary>
    /// Screen-space waves that follow the live signal. They do not depend on AR tracking, so the app
    /// is useful from the first second and keeps working in the dark or over featureless floors.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class LiveWaveOverlay : MonoBehaviour
    {
        // Readings only change every ~3 s; easing between them keeps the overlay from stepping.
        const float Response = 3f;
        // In screen widths. 5 GHz rings sit closer together, same as in the floor field.
        const float Spacing24 = 0.2f;
        const float Spacing5 = 0.1f;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int SignalId = Shader.PropertyToID("_Signal");
        static readonly int SpacingId = Shader.PropertyToID("_Spacing");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");

        Material material;
        float signal, band, intensity;
        float targetSignal, targetBand, targetIntensity;

        void Awake()
        {
            var graphic = GetComponent<Graphic>();
            // Own copy, so play mode never writes into the material asset.
            material = new Material(graphic.material);
            graphic.material = material;
            Apply();
        }

        /// <param name="band01">0 for 2.4 GHz, 1 for 5/6 GHz.</param>
        /// <param name="intensity01">0 hides the overlay; lower it when the floor field is doing the talking.</param>
        public void Show(float signal01, float band01, float intensity01)
        {
            targetSignal = signal01;
            targetBand = band01;
            targetIntensity = intensity01;
        }

        void Update()
        {
            float t = 1f - Mathf.Exp(-Response * Time.unscaledDeltaTime);
            signal = Mathf.Lerp(signal, targetSignal, t);
            band = Mathf.Lerp(band, targetBand, t);
            intensity = Mathf.Lerp(intensity, targetIntensity, t);
            Apply();
        }

        void Apply()
        {
            material.SetColor(ColorId, SignalQuality.ColorOf(signal));
            material.SetFloat(SignalId, signal);
            material.SetFloat(SpacingId, Mathf.Lerp(Spacing24, Spacing5, band));
            material.SetFloat(IntensityId, intensity);
            material.SetFloat(AspectId, (float)Screen.height / Mathf.Max(Screen.width, 1));
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
