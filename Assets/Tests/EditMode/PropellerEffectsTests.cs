using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class PropellerEffectsTests
    {
        [TestCase(5, 5, 2, 2, 5)]
        [TestCase(5, 5, 0, 2, 4)]
        [TestCase(5, 5, 0, 0, 3)]
        [TestCase(1, 1, 0, 0, 1)]
        [TestCase(1, 3, 0, 1, 3)]
        public void Takeoff_IsExactlyTheClippedOrthogonalCross(int width, int height, int x, int y, int count)
        {
            var board = new Board(width, height);
            var origin = new GridPosition(x, y);
            board.SetTile(origin, TileType.Propeller);
            var expected = new List<GridPosition>();
            for (int cellY = 0; cellY < height; cellY++)
                for (int cellX = 0; cellX < width; cellX++)
                    if (Math.Abs(cellX - x) + Math.Abs(cellY - y) <= 1)
                        expected.Add(new GridPosition(cellX, cellY));

            var footprint = SpecialEffects.GetFootprint(board, origin);
            Assert.That(footprint.Count, Is.EqualTo(count));
            Assert.That(footprint, Is.EqualTo(expected));
        }

        [Test]
        public void RemoteCandidates_ExcludeTakeoffAndUseYThenXOrder()
        {
            var board = TestBoards.Create("RRR", "RFR", "RRR");
            var random = new IndexRandom(2);
            var plan = SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(1, 1)) }, random);

            Assert.That(plan.Effects[0].RemoteTarget, Is.EqualTo(new GridPosition(0, 2)));
            Assert.That(random.LastBound, Is.EqualTo(4), "Only the four corners are eligible.");
            Assert.That(random.Calls, Is.EqualTo(1));
            Assert.That(plan.Effects[0].Footprint.Count, Is.EqualTo(6));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(6));
            Assert.That(board.GetTile(new GridPosition(1, 1)), Is.EqualTo(TileType.Propeller));
        }

        [Test]
        public void NoEligibleTarget_AppliesOnlyTakeoffWithoutDrawingRandomness()
        {
            var board = TestBoards.Create("F");
            var random = new IndexRandom(0);
            var plan = SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(0, 0)) }, random);

            Assert.That(plan.Effects[0].RemoteTarget, Is.Null);
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[] { new GridPosition(0, 0) }));
            Assert.That(random.Calls, Is.Zero);
        }

        [Test]
        public void RemoteTarget_SkipsHolesAndRemovalsPlannedByEarlierEffects()
        {
            var board = TestBoards.Create("F.R", "HRR");
            var random = new IndexRandom(0);
            var plan = SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(0, 1)),
                new SpecialActivation(new GridPosition(0, 0))
            }, random);

            Assert.That(plan.Effects[1].Type, Is.EqualTo(TileType.Propeller));
            Assert.That(plan.Effects[1].RemoteTarget, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(random.LastBound, Is.EqualTo(1), "Bottom-row Reds are already scheduled for removal.");
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        [Test]
        public void RemoteTarget_SkipsAnInitialSpecialThatIsQueuedButNotProcessed()
        {
            var board = TestBoards.Create("FRH");
            var random = new IndexRandom(0);
            var plan = SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(0, 0)),
                new SpecialActivation(new GridPosition(2, 0))
            }, random);

            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].RemoteTarget, Is.Null);
            Assert.That(random.Calls, Is.Zero, "The queued Rocket is the only cell outside takeoff.");
        }

        [Test]
        public void RemoteSpecial_EntersTheSameQueueAndCannotTargetTheProcessedPropeller()
        {
            var board = TestBoards.Create("F.RF");
            var random = new IndexRandom(1);
            var plan = SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(0, 0)) }, random);

            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].RemoteTarget, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(plan.Effects[1].Origin, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(plan.Effects[1].RemoteTarget, Is.Null);
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(3));
            Assert.That(random.Calls, Is.EqualTo(1));
        }

        // Spectrum Core + Wisp player swap. Activations run in position order, so the Core goes first whichever cell
        // was selected first; it takes the most common colour (its partner is a special), and the Wisp can then only
        // fly to the one cell neither its takeoff nor the Core's pending Reds cover.
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void LightBallPlusPropellerSwap_EitherSelectionOrder_LeavesTheWispOneTarget(int firstX, int secondX)
        {
            var board = TestBoards.Create("FLRRB");
            var first = new GridPosition(firstX, 0);
            var second = new GridPosition(secondX, 0);
            board.TrySwapAdjacent(first, second);
            var move = SwapValidator.Analyze(board, first, second);
            var random = new IndexRandom(0);

            var plan = SpecialEffects.Plan(board, move.InitialActivations, random);

            Assert.That(move.Kind, Is.EqualTo(SwapKind.SpecialSwap));
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].Type, Is.EqualTo(TileType.LightBall));
            Assert.That(plan.Effects[0].Origin, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(plan.Effects[0].Footprint, Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(2, 0), new GridPosition(3, 0)
            }), "Red is the most common colour.");
            Assert.That(plan.Effects[1].Type, Is.EqualTo(TileType.Propeller));
            Assert.That(plan.Effects[1].Origin, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(plan.Effects[1].RemoteTarget, Is.EqualTo(new GridPosition(4, 0)));
            Assert.That(random.LastBound, Is.EqualTo(1), "Only the Blue is left to target.");
            Assert.That(random.Calls, Is.EqualTo(1));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(5));
        }

        // Two Wisps queued by the swap itself: the first one's remote hit lands inside the second one's takeoff,
        // so the second must look further, and every cell is still removed once.
        [Test]
        public void TwoAdjacentPropellers_SecondTargetsWhatTheFirstLeft_AndEachCellIsRemovedOnce()
        {
            var board = TestBoards.Create("FFRG");
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            board.TrySwapAdjacent(first, second);
            var move = SwapValidator.Analyze(board, first, second);
            var random = new IndexRandom(0);

            var plan = SpecialEffects.Plan(board, move.InitialActivations, random);

            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].Origin, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(plan.Effects[0].RemoteTarget, Is.EqualTo(new GridPosition(2, 0)), "First of Red and Green.");
            Assert.That(plan.Effects[1].Origin, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(plan.Effects[1].RemoteTarget, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(random.Calls, Is.EqualTo(2));
            Assert.That(random.LastBound, Is.EqualTo(1), "The Red is already scheduled, so only the Green is left.");
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0), new GridPosition(3, 0)
            }));
        }

        [Test]
        public void ConflictingRequests_AreRejectedBeforeAnyTargetingDraw()
        {
            var board = TestBoards.Create("FRR");
            var random = new IndexRandom(0);
            var origin = new GridPosition(0, 0);
            Assert.Throws<ArgumentException>(() => SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(origin, TileType.Red), new SpecialActivation(origin, TileType.Blue)
            }, random));
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(TileType.HorizontalRocket, TileType.Red)]
        [TestCase(TileType.VerticalRocket, TileType.Red)]
        [TestCase(TileType.TNT, TileType.Red)]
        [TestCase(TileType.Propeller, TileType.Red)]
        [TestCase(TileType.Red, null)]
        [TestCase(TileType.Empty, null)]
        [TestCase((TileType)99, null)]
        public void MalformedLaterInitialRequest_IsRejectedBeforeAnEarlierPropellerDraws(
            TileType type, TileType? targetColor)
        {
            var board = TestBoards.Create("RGBY", "GBHR", "FBRG");
            var later = new GridPosition(2, 1);
            board.SetTile(later, type);
            var before = Snapshot(board);
            var random = new IndexRandom(0);

            // Reverse the input order: the Propeller at (0, 0) still sorts before (2, 1).
            Assert.Throws<ArgumentException>(() => SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(later, targetColor),
                new SpecialActivation(new GridPosition(0, 0))
            }, random));

            Assert.That(random.Calls, Is.Zero, "Reject all malformed initial requests before targeting starts.");
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void OutOfBoundsLaterInitialRequest_IsRejectedBeforeAnEarlierPropellerDraws()
        {
            var board = TestBoards.Create("RGBY", "GBHR", "FBRG");
            var before = Snapshot(board);
            var random = new IndexRandom(0);

            Assert.Throws<ArgumentOutOfRangeException>(() => SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(4, 1)),
                new SpecialActivation(new GridPosition(0, 0))
            }, random));

            Assert.That(random.Calls, Is.Zero);
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        private static TileType[,] Snapshot(Board board)
        {
            var snapshot = new TileType[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    snapshot[x, y] = board.GetTile(new GridPosition(x, y));
            return snapshot;
        }

        private sealed class IndexRandom : Random
        {
            private readonly int index;
            public int Calls { get; private set; }
            public int LastBound { get; private set; }
            public IndexRandom(int index) { this.index = index; }
            public override int Next(int maxValue)
            {
                Calls++;
                LastBound = maxValue;
                Assert.That(index, Is.InRange(0, maxValue - 1));
                return index;
            }
        }
    }
}
