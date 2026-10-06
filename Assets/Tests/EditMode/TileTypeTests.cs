using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class TileTypeTests
    {
        [Test]
        public void Empty_IsZeroAndTheDefaultValue()
        {
            Assert.That((int)TileType.Empty, Is.Zero);
            Assert.That(default(TileType), Is.EqualTo(TileType.Empty));
        }
    }
}
