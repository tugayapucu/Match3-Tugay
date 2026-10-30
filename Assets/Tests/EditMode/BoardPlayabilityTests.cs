using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class BoardPlayabilityTests
    {
        // The board the shipped game starts with (8x8, six colours, seed 42).
        private static readonly string[] Seed42Rows =
        {
            "RBPYGRBG", "GPGYRRPY", "PBRRGPYR", "GRGYPRYP",
            "PYRPROPY", "YRPYBRRP", "GPGGYGBG", "PRRYGGPY"
        };

        // Reached in a Core simulation of the current resolution rules: seed 93, ten normal moves,
        // level still Playing with 10 moves left. Stable, no specials, and no legal move.
        private static readonly string[] Seed93DeadRows =
        {
            "PGGOYYOP", "BRYBGBGY", "OYBPYRPY", "PGRPRBGP",
            "PGOGBOYO", "OYRGGPPY", "BYPRYORR", "GRPBGPYR"
        };

        [Test]
        public void HasAnyLegalMove_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BoardPlayability.HasAnyLegalMove(null));
        }

        [Test]
        public void HasAnyLegalMove_StableBoardWithoutAnyAcceptedSwap_ReturnsFalse()
        {
            Assert.That(BoardPlayability.HasAnyLegalMove(TestBoards.Create("RGB", "YPO", "GBR")), Is.False);
        }

        [Test]
        public void HasAnyLegalMove_OneSwapCompletesALine_ReturnsTrue()
        {
            Assert.That(BoardPlayability.HasAnyLegalMove(TestBoards.Create("RRG", "BYR", "PGY")), Is.True);
        }

        [Test]
        public void HasAnyLegalMove_ShippedSeed42Board_ReturnsTrue()
        {
            var palette = new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange };
            var board = BoardGenerator.Generate(8, 8, palette, 42);

            // Pins the fixture: if generation ever changes, this board is no longer the one players start with.
            AssertSameTiles(board, TestBoards.Create(Seed42Rows));
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
        }

        [Test]
        public void HasAnyLegalMove_SpecialWithAnOccupiedNeighbour_ReturnsTrue()
        {
            // The same tiles as the dead board, with a special in the centre.
            Assert.That(BoardPlayability.HasAnyLegalMove(TestBoards.Create("RGB", "YHO", "GBR")), Is.True);
        }

        // A lone string[] would be expanded into TestCase's params array, so wrap it in object[].
        [TestCase(new object[] { new[] { "H" } })]
        [TestCase(new object[] { new[] { "...", ".H.", "..." } })]
        public void HasAnyLegalMove_SpecialWithoutAnOccupiedNeighbour_ReturnsFalse(string[] rows)
        {
            // Generic semantics: a special makes a board playable only through an actual adjacent swap.
            Assert.That(BoardPlayability.HasAnyLegalMove(TestBoards.Create(rows)), Is.False);
        }

        [TestCase(new[] { "RRGR" }, true)]
        [TestCase(new[] { "R", "R", "G", "R" }, true)]
        [TestCase(new[] { "R" }, false)]
        public void HasAnyLegalMove_NarrowBoards_CheckBothNeighbourDirections(string[] rows, bool expected)
        {
            Assert.That(BoardPlayability.HasAnyLegalMove(TestBoards.Create(rows)), Is.EqualTo(expected));
        }

        [TestCase(new object[] { new[] { "RRG", "BYR", "PGY" } })]
        [TestCase(new object[] { new[] { "RGB", "YPO", "GBR" } })]
        public void HasAnyLegalMove_LeavesTheInputBoardUnchanged(string[] rows)
        {
            var board = TestBoards.Create(rows);

            BoardPlayability.HasAnyLegalMove(board);

            AssertSameTiles(board, TestBoards.Create(rows));
        }

        [TestCase(new[] { "RRG", "BYR", "PGY" }, true)]
        [TestCase(new[] { "RGB", "YPO", "GBR" }, false)]
        public void HasAnyLegalMove_RepeatedCalls_ReturnTheSameAnswer(string[] rows, bool expected)
        {
            var board = TestBoards.Create(rows);

            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.EqualTo(expected));
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.EqualTo(expected));
        }

        [Test]
        public void HasAnyLegalMove_ReachedSeed93DeadBoard_ReturnsFalse()
        {
            var board = TestBoards.Create(Seed93DeadRows);

            Assert.That(MatchFinder.FindMatches(board), Is.Empty, "The fixture must be stable.");
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.False);
        }

        private static void AssertSameTiles(Board actual, Board expected)
        {
            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    Assert.That(actual.GetTile(position), Is.EqualTo(expected.GetTile(position)), $"Tile at {position}.");
                }
            }
        }
    }
}
