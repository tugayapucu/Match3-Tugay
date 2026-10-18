using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialPlacementTests
    {
        [TestCase(TileType.HorizontalRocket)]
        [TestCase(TileType.VerticalRocket)]
        [TestCase(TileType.Propeller)]
        [TestCase(TileType.TNT)]
        [TestCase(TileType.LightBall)]
        public void PlaceSpecials_PlacesTypeThenGravityAndRefillPreserveIt(TileType type)
        {
            var board = TestBoards.Create(".", ".", "R");
            var anchor = new GridPosition(0, 2);

            BoardResolution.PlaceSpecials(board, new[] { new TileSpawn(anchor, type) });
            Assert.That(board.GetTile(anchor), Is.EqualTo(type));
            BoardResolution.ApplyGravity(board);
            BoardResolution.Refill(board, new[] { TileType.Red, TileType.Green, TileType.Blue }, new Random(7));

            Assert.That(board.GetTile(new GridPosition(0, 1)), Is.EqualTo(type));
            Assert.That(board.GetTile(new GridPosition(0, 0)), Is.EqualTo(TileType.Red));
        }

        [Test]
        public void ConsumeThenPlace_FourNormalColorsRemainInResult()
        {
            var board = TestBoards.Create("RRRR");
            var analysis = MatchFinder.Analyze(board);
            var creations = SpecialCreationRules.FindCreations(analysis);
            var cleared = BoardResolution.ClearCells(board, analysis.MatchedPositions);

            BoardResolution.PlaceSpecials(board, creations);

            var result = new ResolutionResult(cleared);
            Assert.That(result.CountOf(TileType.Red), Is.EqualTo(4));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(result.CountOf(TileType.HorizontalRocket), Is.Zero);
        }

        [Test]
        public void PlaceSpecials_InvalidBatch_ChangesNothing()
        {
            var board = TestBoards.Create(".R");
            var valid = new TileSpawn(new GridPosition(0, 0), TileType.TNT);
            Assert.Throws<ArgumentNullException>(() => BoardResolution.PlaceSpecials(null, new[] { valid }));
            Assert.Throws<ArgumentNullException>(() => BoardResolution.PlaceSpecials(board, null));
            Assert.Throws<ArgumentException>(() => BoardResolution.PlaceSpecials(board, new[] { valid, valid }));
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardResolution.PlaceSpecials(board, new[]
            {
                valid, new TileSpawn(new GridPosition(2, 0), TileType.TNT)
            }));
            Assert.Throws<InvalidOperationException>(() => BoardResolution.PlaceSpecials(board, new[]
            {
                valid, new TileSpawn(new GridPosition(1, 0), TileType.TNT)
            }));
            foreach (TileType invalid in new[] { TileType.Red, TileType.Empty, (TileType)99 })
            {
                Assert.Throws<ArgumentException>(() => BoardResolution.PlaceSpecials(board, new[]
                {
                    new TileSpawn(valid.Position, invalid)
                }));
            }
            Assert.That(board.GetTile(valid.Position), Is.EqualTo(TileType.Empty));
            Assert.That(board.GetTile(new GridPosition(1, 0)), Is.EqualTo(TileType.Red));
        }
    }
}
