using NUnit.Framework;

namespace Match3.Presentation.Tests
{
    public class TileEasingTests
    {
        [TestCase(0f, 0f)]
        [TestCase(0.25f, 0.15625f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.75f, 0.84375f)]
        [TestCase(1f, 1f)]
        public void EaseInOut_KnownPoints(float t, float expected)
        {
            Assert.That(TileEasing.EaseInOut(t), Is.EqualTo(expected).Within(1e-6f));
        }

        [TestCase(-1f, 0f)]
        [TestCase(2f, 1f)]
        public void EaseInOut_ClampsOutsideZeroToOne(float t, float expected)
        {
            Assert.That(TileEasing.EaseInOut(t), Is.EqualTo(expected));
        }

        [Test]
        public void EaseInOut_IsSymmetricMonotonicAndGentleAtBothEnds()
        {
            float previous = 0f;
            for (int i = 1; i <= 100; i++)
            {
                float t = i / 100f;
                float eased = TileEasing.EaseInOut(t);
                Assert.That(eased, Is.GreaterThanOrEqualTo(previous), "Never moves backwards.");
                Assert.That(TileEasing.EaseInOut(1f - t), Is.EqualTo(1f - eased).Within(1e-6f), "Symmetric.");
                previous = eased;
            }

            // Slower than linear near the ends: the tile starts and settles gently.
            Assert.That(TileEasing.EaseInOut(0.1f), Is.LessThan(0.1f));
            Assert.That(TileEasing.EaseInOut(0.9f), Is.GreaterThan(0.9f));
        }
    }
}
