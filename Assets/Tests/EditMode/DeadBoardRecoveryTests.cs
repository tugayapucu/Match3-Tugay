using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class DeadBoardRecoveryTests
    {
        private static readonly TileType[] Palette =
        {
            TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange
        };

        // Reached in a Core simulation of the current resolution rules: seed 93, ten normal moves, still Playing.
        private static readonly string[] Seed93DeadRows =
        {
            "PGGOYYOP", "BRYBGBGY", "OYBPYRPY", "PGRPRBGP",
            "PGOGBOYO", "OYRGGPPY", "BYPRYORR", "GRPBGPYR"
        };

        [Test]
        public void RecoverIfDead_PlayableBoard_ChangesNothingAndDrawsNoRandomness()
        {
            var board = TestBoards.Create("RRG", "BYR", "PGY");
            var random = new Random(5);

            Assert.That(DeadBoardRecovery.RecoverIfDead(board, Palette, random, TestBoards.Create("RRG", "BYR", "PGY")), Is.False);

            AssertSameTiles(board, TestBoards.Create("RRG", "BYR", "PGY"));
            Assert.That(random.Next(), Is.EqualTo(new Random(5).Next()), "No draw may be consumed.");
        }

        [Test]
        public void RecoverIfDead_Seed93DeadBoard_ReplacesItInPlaceWithTheFirstValidCandidate()
        {
            var board = TestBoards.Create(Seed93DeadRows);
            var random = new Random(5);
            // The first generation seed drawn from the same stream gives the expected replacement.
            var expectedRandom = new Random(5);
            var expected = BoardGenerator.Generate(8, 8, Palette, expectedRandom.Next());
            // Part of the deterministic contract: if this changes, the test must fail rather than skip.
            Assert.That(BoardPlayability.HasAnyLegalMove(expected), Is.True, "This seed's first candidate is playable.");

            Assert.That(DeadBoardRecovery.RecoverIfDead(board, Palette, random, CreateFallback()), Is.True);

            AssertSameTiles(board, expected);
            Assert.That(MatchFinder.FindMatches(board), Is.Empty);
            Assert.That(BoardPlayability.HasAnyLegalMove(board), Is.True);
            Assert.That(random.Next(), Is.EqualTo(expectedRandom.Next()), "Exactly one draw was consumed.");
        }

        [Test]
        public void RecoverIfDead_SameDeadBoardAndSeed_RecoverTheSameWay()
        {
            var first = TestBoards.Create(Seed93DeadRows);
            var second = TestBoards.Create(Seed93DeadRows);

            DeadBoardRecovery.RecoverIfDead(first, Palette, new Random(5), CreateFallback());
            DeadBoardRecovery.RecoverIfDead(second, Palette, new Random(5), CreateFallback());

            AssertSameTiles(first, second);
        }

        [Test]
        public void RecoverIfDead_EveryCandidateDead_FallsBackWithoutThrowingAfterTenDraws()
        {
            // On a 4x1 board most generated candidates are dead; with Random(0) all ten are.
            var board = TestBoards.Create("RGBY");
            var fallback = TestBoards.Create("RRGR");
            var random = new Random(0);

            bool recovered = false;
            Assert.DoesNotThrow(() => recovered = DeadBoardRecovery.RecoverIfDead(board, Palette, random, fallback));

            Assert.That(recovered, Is.True);
            AssertSameTiles(board, TestBoards.Create("RRGR"));
            var expectedRandom = new Random(0);
            for (int draw = 0; draw < 10; draw++)
            {
                expectedRandom.Next();
            }

            Assert.That(random.Next(), Is.EqualTo(expectedRandom.Next()), "Exactly ten draws were consumed.");
        }

        [Test]
        public void RecoverIfDead_AfterFallingBack_TheFallbackStaysIndependent()
        {
            var board = TestBoards.Create("RGBY");
            var fallback = TestBoards.Create("RRGR");
            DeadBoardRecovery.RecoverIfDead(board, Palette, new Random(0), fallback);

            board.SetTile(new GridPosition(0, 0), TileType.Blue);

            AssertSameTiles(fallback, TestBoards.Create("RRGR"));
        }

        [Test]
        public void RecoverIfDead_InvalidFallbackNeeded_ThrowsBecauseThatIsAProgrammingError()
        {
            // A dead fallback can only come from a bug: the level validates its starting board first.
            var board = TestBoards.Create("RGBY");

            Assert.Throws<InvalidOperationException>(
                () => DeadBoardRecovery.RecoverIfDead(board, Palette, new Random(0), TestBoards.Create("RGBY")));
        }

        [Test]
        public void RecoverIfDead_InvalidArguments_ThrowBeforeAnyDrawOrChange()
        {
            var board = TestBoards.Create(Seed93DeadRows);
            var random = new Random(5);

            Assert.Throws<ArgumentNullException>(() => DeadBoardRecovery.RecoverIfDead(null, Palette, random, CreateFallback()));
            Assert.Throws<ArgumentNullException>(() => DeadBoardRecovery.RecoverIfDead(board, null, random, CreateFallback()));
            Assert.Throws<ArgumentNullException>(() => DeadBoardRecovery.RecoverIfDead(board, Palette, null, CreateFallback()));
            Assert.Throws<ArgumentNullException>(() => DeadBoardRecovery.RecoverIfDead(board, Palette, random, null));
            Assert.Throws<ArgumentException>(() => DeadBoardRecovery.RecoverIfDead(board, Palette, random, new Board(3, 3)));

            AssertSameTiles(board, TestBoards.Create(Seed93DeadRows));
            Assert.That(random.Next(), Is.EqualTo(new Random(5).Next()), "No draw may be consumed.");
        }

        // A playable 8x8 fallback, as the level would keep from its starting board.
        private static Board CreateFallback()
        {
            return BoardGenerator.GeneratePlayable(8, 8, Palette, 42);
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
