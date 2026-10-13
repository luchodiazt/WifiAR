using UnityEngine;
using WifiAR.Sampling;

namespace WifiAR.Visualization
{
    /// <summary>
    /// Draws the signal as waves travelling outwards from the router across the floor.
    /// Wave color and strength come from measured samples; where nothing was measured only a faint
    /// neutral hint is shown.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaveFieldRenderer : MonoBehaviour
    {
        // Keeps the quad clear of the real floor so it does not z-fight with plane visuals later on.
        const float FloorOffset = 0.01f;
        const float OriginResponse = 4f;

        static readonly int DataTexId = Shader.PropertyToID("_DataTex");
        static readonly int GridCenterId = Shader.PropertyToID("_GridCenter");
        static readonly int GridSpanId = Shader.PropertyToID("_GridSpan");
        static readonly int RouterPosId = Shader.PropertyToID("_RouterPos");
        static readonly int ColorWeakId = Shader.PropertyToID("_ColorWeak");
        static readonly int ColorMidId = Shader.PropertyToID("_ColorMid");
        static readonly int ColorStrongId = Shader.PropertyToID("_ColorStrong");

        [SerializeField] Material waveMaterial;

        SignalGrid grid;
        Texture2D dataTexture;
        Color32[] pixels;
        Material material;
        MeshRenderer meshRenderer;
        Vector2 center;
        Vector2 waveOrigin;
        Vector2 waveOriginTarget;
        bool dirty;

        public bool IsActive => grid != null;
        public int SampleCount => grid != null ? grid.SampleCount : 0;

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            GetComponent<MeshFilter>().sharedMesh = BuildFloorQuad();
        }

        /// <summary>Starts a new, empty map centered on this spot. Waves start there until a better origin is set.</summary>
        public void Begin(Vector3 centerPosition, float floorY)
        {
            grid = new SignalGrid();
            center = new Vector2(centerPosition.x, centerPosition.z);
            waveOrigin = center;
            waveOriginTarget = center;

            if (dataTexture == null)
            {
                dataTexture = new Texture2D(SignalGrid.Size, SignalGrid.Size, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                pixels = new Color32[SignalGrid.Size * SignalGrid.Size];
            }

            if (material == null)
            {
                material = new Material(waveMaterial);
                material.SetColor(ColorWeakId, SignalQuality.WeakColor);
                material.SetColor(ColorMidId, SignalQuality.MidColor);
                material.SetColor(ColorStrongId, SignalQuality.StrongColor);
                meshRenderer.sharedMaterial = material;
            }

            material.SetTexture(DataTexId, dataTexture);
            material.SetVector(GridCenterId, center);
            material.SetFloat(GridSpanId, SignalGrid.Span);
            material.SetVector(RouterPosId, center);

            SetFloorHeight(floorY);
            transform.rotation = Quaternion.identity;
            meshRenderer.enabled = true;
            dirty = true;
        }

        public void SetFloorHeight(float floorY)
        {
            transform.position = new Vector3(center.x, floorY + FloorOffset, center.y);
        }

        public void SetVisible(bool visible)
        {
            meshRenderer.enabled = visible && grid != null;
        }

        /// <summary>Where the waves spread from: the strongest spot found so far.</summary>
        public void SetWaveOrigin(Vector3 worldPosition)
        {
            waveOriginTarget = new Vector2(worldPosition.x, worldPosition.z);
        }

        public void AddSample(Vector3 worldPosition, float signal01, float band01)
        {
            if (grid == null) return;
            grid.Add(new Vector2(worldPosition.x - center.x, worldPosition.z - center.y), signal01, band01);
            dirty = true;
        }

        void LateUpdate()
        {
            if (grid == null) return;

            // Glide instead of jumping when a stronger spot turns up.
            waveOrigin = Vector2.Lerp(waveOrigin, waveOriginTarget, 1f - Mathf.Exp(-OriginResponse * Time.unscaledDeltaTime));
            material.SetVector(RouterPosId, waveOrigin);

            if (!dirty) return;
            dirty = false;
            grid.WriteTo(pixels);
            dataTexture.SetPixels32(pixels);
            dataTexture.Apply(false);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (dataTexture != null) Destroy(dataTexture);
        }

        static Mesh BuildFloorQuad()
        {
            float half = SignalGrid.Span * 0.5f;
            var mesh = new Mesh { name = "Wave Field" };
            mesh.SetVertices(new[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(-half, 0f, half),
                new Vector3(half, 0f, half),
                new Vector3(half, 0f, -half)
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
