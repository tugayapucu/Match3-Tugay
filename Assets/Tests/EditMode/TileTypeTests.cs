using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class TileTypeTests
    {
        [TestCase(TileType.Empty, 0)]
        [TestCase(TileType.Red, 1)]
        [TestCase(TileType.Green, 2)]
        [TestCase(TileType.Blue, 3)]
        [TestCase(TileType.Yellow, 4)]
        [TestCase(TileType.Purple, 5)]
        [TestCase(TileType.Orange, 6)]
        [TestCase(TileType.HorizontalRocket, 7)]
        [TestCase(TileType.VerticalRocket, 8)]
        [TestCase(TileType.Propeller, 9)]
        [TestCase(TileType.TNT, 10)]
        [TestCase(TileType.LightBall, 11)]
        public void Values_AreStable(TileType type, int expected)
        {
            Assert.That((int)type, Is.EqualTo(expected));
        }

        [Test]
        public void Empty_IsZeroAndTheDefaultValue()
        {
            Assert.That((int)TileType.Empty, Is.Zero);
            Assert.That(default(TileType), Is.EqualTo(TileType.Empty));
        }
    }
}
