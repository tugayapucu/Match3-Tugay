using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class GridPositionTests
    {
        [TestCase(0, 0)]
        [TestCase(2, -3)]
        public void SameCoordinates_AreEqual(int x, int y)
        {
            var first = new GridPosition(x, y);
            var second = new GridPosition(x, y);

            Assert.That(first.Equals(second), Is.True);
            Assert.That(second.Equals(first), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [TestCase(3, 3)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        public void DifferentCoordinates_AreNotEqual(int otherX, int otherY)
        {
            var first = new GridPosition(2, 3);
            var second = new GridPosition(otherX, otherY);

            Assert.That(first.Equals(second), Is.False);
            Assert.That(second.Equals(first), Is.False);
            Assert.That(first.Equals((object)second), Is.False);
            Assert.That(first == second, Is.False);
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void Equals_NullOrDifferentObjectType_ReturnsFalse()
        {
            var position = new GridPosition(2, 3);

            Assert.That(position.Equals(null), Is.False);
            Assert.That(position.Equals("(2, 3)"), Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(2, 3)]
        [TestCase(-2, -3)]
        public void Constructor_PreservesCoordinates(int x, int y)
        {
            var position = new GridPosition(x, y);

            Assert.That(position.X, Is.EqualTo(x));
            Assert.That(position.Y, Is.EqualTo(y));
        }
    }
}
