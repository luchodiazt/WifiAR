using UnityEngine;

namespace WifiAR.Visualization
{
    /// <summary>
    /// Floor grid that spreads sparse samples into a continuous field (inverse distance weighting)
    /// and remembers how far each cell is from real data, so the renderer never paints a guess as fact.
    /// Coordinates are meters on the floor plane, relative to the grid center.
    /// </summary>
    public sealed class SignalGrid
    {
        public const int Size = 128;
        public const float CellSize = 0.25f;
        public const float Span = Size * CellSize;

        // Samples land roughly every 1.5-3 m (one RSSI refresh per ~3 s of slow walking).
        const float InfluenceRadius = 3.5f;
        const float FullConfidenceDistance = 1.2f;
        const float NoConfidenceDistance = 3f;
        const float Softening = 0.25f;

        readonly float[] weightSum = new float[Size * Size];
        readonly float[] signalSum = new float[Size * Size];
        readonly float[] bandSum = new float[Size * Size];
        readonly float[] nearest = new float[Size * Size];

        public int SampleCount { get; private set; }

        public SignalGrid()
        {
            for (int i = 0; i < nearest.Length; i++) nearest[i] = float.PositiveInfinity;
        }

        /// <param name="band01">0 for 2.4 GHz, 1 for 5/6 GHz.</param>
        public void Add(Vector2 point, float signal01, float band01)
        {
            SampleCount++;

            int minX = Mathf.Max(CellIndex(point.x - InfluenceRadius), 0);
            int maxX = Mathf.Min(CellIndex(point.x + InfluenceRadius), Size - 1);
            int minY = Mathf.Max(CellIndex(point.y - InfluenceRadius), 0);
            int maxY = Mathf.Min(CellIndex(point.y + InfluenceRadius), Size - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(CellCenter(x, y), point);
                    if (distance > InfluenceRadius) continue;

                    int i = y * Size + x;
                    float weight = 1f / (distance * distance + Softening);
                    weightSum[i] += weight;
                    signalSum[i] += weight * signal01;
                    bandSum[i] += weight * band01;
                    if (distance < nearest[i]) nearest[i] = distance;
                }
            }
        }

        public float SignalAt(int x, int y)
        {
            int i = y * Size + x;
            return weightSum[i] > 0f ? signalSum[i] / weightSum[i] : 0f;
        }

        public float BandAt(int x, int y)
        {
            int i = y * Size + x;
            return weightSum[i] > 0f ? bandSum[i] / weightSum[i] : 0f;
        }

        /// <summary>1 next to a real sample, fading to 0 where the value would be pure extrapolation.</summary>
        public float ConfidenceAt(int x, int y)
        {
            float t = Mathf.InverseLerp(FullConfidenceDistance, NoConfidenceDistance, nearest[y * Size + x]);
            return 1f - t * t * (3f - 2f * t);
        }

        /// <summary>R = signal, G = confidence, B = band. Row-major, one pixel per cell.</summary>
        public void WriteTo(Color32[] pixels)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = new Color32(
                        (byte)Mathf.RoundToInt(SignalAt(x, y) * 255f),
                        (byte)Mathf.RoundToInt(ConfidenceAt(x, y) * 255f),
                        (byte)Mathf.RoundToInt(BandAt(x, y) * 255f),
                        255);
                }
            }
        }

        public static int CellIndex(float coordinate)
        {
            return Mathf.FloorToInt(coordinate / CellSize) + Size / 2;
        }

        public static Vector2 CellCenter(int x, int y)
        {
            return new Vector2((x - Size / 2 + 0.5f) * CellSize, (y - Size / 2 + 0.5f) * CellSize);
        }
    }
}
