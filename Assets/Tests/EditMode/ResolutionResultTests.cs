using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class ResolutionResultTests
    {
        [Test]
        public void Counts_AreExact()
        {
            var result = new ResolutionResult(new[]
            {
                TileType.Red, TileType.Red, TileType.Green, TileType.Blue, TileType.Red
            });

            Assert.That(result.ClearedCount, Is.EqualTo(5));
            Assert.That(result.CountOf(TileType.Red), Is.EqualTo(3));
            Assert.That(result.CountOf(TileType.Green), Is.EqualTo(1));
            Assert.That(result.CountOf(TileType.Yellow), Is.Zero);
            // Order is not part of the contract: compare as a multiset.
            Assert.That(result.ClearedTiles, Is.EquivalentTo(new[]
            {
                TileType.Blue, TileType.Red, TileType.Green, TileType.Red, TileType.Red
            }));
        }

        [Test]
        public void EmptyResult_IsValid()
        {
            var result = new ResolutionResult(new TileType[0]);

            Assert.That(result.ClearedCount, Is.Zero);
            Assert.That(result.ClearedTiles, Is.Empty);
            Assert.That(result.CountOf(TileType.Red), Is.Zero);
        }

        [Test]
        public void ChangingTheSourceAfterwards_DoesNotChangeTheResult()
        {
            var source = new List<TileType> { TileType.Red, TileType.Green };
            var result = new ResolutionResult(source);

            source.Add(TileType.Red);
            source[0] = TileType.Blue;

            Assert.That(result.ClearedCount, Is.EqualTo(2));
            Assert.That(result.CountOf(TileType.Red), Is.EqualTo(1));
            Assert.That(result.CountOf(TileType.Blue), Is.Zero);
        }

        [Test]
        public void ClearedTiles_CannotBeModifiedThroughTheResult()
        {
            var result = new ResolutionResult(new[] { TileType.Red });

            // Even if a caller casts the read-only view back to a collection, it refuses changes.
            var asCollection = (ICollection<TileType>)result.ClearedTiles;
            Assert.That(asCollection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => asCollection.Add(TileType.Blue));
        }

        [Test]
        public void NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ResolutionResult(null));
        }

        [TestCase(TileType.Empty)]
        [TestCase((TileType)99)]
        public void EmptyOrUndefinedEntry_Throws(TileType invalid)
        {
            Assert.Throws<ArgumentException>(() => new ResolutionResult(new[] { TileType.Red, invalid }));
        }
    }
}
