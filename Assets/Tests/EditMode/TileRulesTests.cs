using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class TileRulesTests
    {
        [TestCase(TileType.Empty, true, false, false)]
        [TestCase(TileType.Red, false, true, false)]
        [TestCase(TileType.Green, false, true, false)]
        [TestCase(TileType.Blue, false, true, false)]
        [TestCase(TileType.Yellow, false, true, false)]
        [TestCase(TileType.Purple, false, true, false)]
        [TestCase(TileType.Orange, false, true, false)]
        [TestCase(TileType.HorizontalRocket, false, false, true)]
        [TestCase(TileType.VerticalRocket, false, false, true)]
        [TestCase(TileType.Propeller, false, false, true)]
        [TestCase(TileType.TNT, false, false, true)]
        [TestCase(TileType.LightBall, false, false, true)]
        [TestCase((TileType)(-1), false, false, false)]
        [TestCase((TileType)12, false, false, false)]
        [TestCase((TileType)99, false, false, false)]
        public void Classification_IsExplicitAndDisjoint(TileType type, bool empty, bool normal, bool special)
        {
            Assert.That(TileRules.IsEmpty(type), Is.EqualTo(empty));
            Assert.That(TileRules.IsNormalMatchItem(type), Is.EqualTo(normal));
            Assert.That(TileRules.IsSpecial(type), Is.EqualTo(special));
        }
    }
}
