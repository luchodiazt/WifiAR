using NUnit.Framework;
using UnityEngine;
using WifiAR.Visualization;

namespace WifiAR.Tests
{
    public class SignalGridTests
    {
        static int Cell(float meters) => SignalGrid.CellIndex(meters);

        [Test]
        public void EmptyGrid_HasNoConfidenceAnywhere()
        {
            var grid = new SignalGrid();

            Assert.AreEqual(0f, grid.ConfidenceAt(Cell(0f), Cell(0f)));
        }

        [Test]
        public void SingleSample_SetsValueAroundItAndFadesWithDistance()
        {
            var grid = new SignalGrid();
            grid.Add(Vector2.zero, 0.8f, 1f);

            Assert.AreEqual(0.8f, grid.SignalAt(Cell(0f), Cell(0f)), 0.001f);
            Assert.AreEqual(1f, grid.ConfidenceAt(Cell(0f), Cell(0f)), 0.001f);
            Assert.AreEqual(1f, grid.BandAt(Cell(0f), Cell(0f)), 0.001f);

            Assert.Greater(grid.ConfidenceAt(Cell(0f), Cell(0f)), grid.ConfidenceAt(Cell(2f), Cell(0f)));
            Assert.AreEqual(0f, grid.ConfidenceAt(Cell(5f), Cell(0f)));
        }

        [Test]
        public void TwoSamples_BlendTowardsTheCloserOne()
        {
            var grid = new SignalGrid();
            grid.Add(new Vector2(-1f, 0f), 1f, 1f);
            grid.Add(new Vector2(1f, 0f), 0f, 0f);

            Assert.Greater(grid.SignalAt(Cell(-1f), Cell(0f)), 0.5f);
            Assert.Less(grid.SignalAt(Cell(1f), Cell(0f)), 0.5f);
            Assert.Greater(grid.SignalAt(Cell(-1f), Cell(0f)), grid.SignalAt(Cell(-0.25f), Cell(0f)));
        }

        [Test]
        public void SampleOutsideTheGrid_IsIgnoredWithoutThrowing()
        {
            var grid = new SignalGrid();

            Assert.DoesNotThrow(() => grid.Add(new Vector2(500f, -500f), 1f, 1f));
            Assert.AreEqual(0f, grid.ConfidenceAt(SignalGrid.Size - 1, 0));
        }

        [Test]
        public void WriteTo_PacksSignalConfidenceAndBand()
        {
            var grid = new SignalGrid();
            grid.Add(Vector2.zero, 1f, 1f);
            var pixels = new Color32[SignalGrid.Size * SignalGrid.Size];

            grid.WriteTo(pixels);

            Color32 center = pixels[Cell(0f) * SignalGrid.Size + Cell(0f)];
            Assert.AreEqual(255, center.r);
            Assert.AreEqual(255, center.g);
            Assert.AreEqual(255, center.b);
            Assert.AreEqual(0, pixels[0].g);
        }
    }
}
