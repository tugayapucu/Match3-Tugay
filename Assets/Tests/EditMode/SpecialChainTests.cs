using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialChainTests
    {
        [TestCase(TileType.Red, TileType.Blue)]
        [TestCase(TileType.Blue, TileType.Red)]
        [TestCase(null, TileType.Red)]
        [TestCase(TileType.Red, null)]
        public void Plan_ConflictingDuplicateTargets_AreRejectedBeforeBoardChanges(TileType? first, TileType? second)
        {
            var board = TestBoards.Create("HR");
            var before = Snapshot(board);
            var origin = new GridPosition(0, 0);
            var error = Assert.Throws<ArgumentException>(() => SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(origin, first), new SpecialActivation(origin, second)
            }));
            Assert.That(error.ParamName, Is.EqualTo("initialActivations"));
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void Plan_ExactDuplicateTargets_StillCollapseToOneActivation()
        {
            var board = TestBoards.Create("LR");
            var request = new SpecialActivation(new GridPosition(0, 0), TileType.Red);
            Assert.That(SpecialEffects.Plan(board, new[] { request, request }).Effects.Count, Is.EqualTo(1));
        }

        [TestCase(TileType.HorizontalRocket)]
        [TestCase(TileType.VerticalRocket)]
        [TestCase(TileType.TNT)]
        [TestCase(TileType.Propeller)]
        public void TargetColor_OnNonLightBall_IsRejectedByFootprintAndPlan(TileType special)
        {
            var board = TestBoards.Create("RG");
            var origin = new GridPosition(0, 0);
            board.SetTile(origin, special);
            var before = Snapshot(board);
            var random = new Random(42);
            var request = new SpecialActivation(origin, TileType.Red);

            var footprintError = Assert.Throws<ArgumentException>(() =>
                SpecialEffects.GetFootprint(board, origin, TileType.Red));
            var planError = Assert.Throws<ArgumentException>(() =>
                SpecialEffects.Plan(board, new[] { request }, random));

            Assert.That(footprintError.ParamName, Is.EqualTo("targetColor"));
            Assert.That(planError.ParamName, Is.EqualTo("targetColor"));
            Assert.That(Snapshot(board), Is.EqualTo(before));
            Assert.That(random.Next(), Is.EqualTo(new Random(42).Next()), "Reject before a targeting draw.");
        }

        [Test]
        public void TargetColor_OnLightBall_AcceptsNormalColorAndClearsOnlyThatColor()
        {
            var board = TestBoards.Create("LRG");
            var origin = new GridPosition(0, 0);
            var expected = new[] { origin, new GridPosition(1, 0) };

            Assert.That(SpecialEffects.GetFootprint(board, origin, TileType.Red), Is.EqualTo(expected));
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(origin, TileType.Red) });
            Assert.That(plan.RemovedPositions, Is.EqualTo(expected));
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
            Assert.That(board.GetTile(origin), Is.EqualTo(TileType.LightBall));
        }

        [TestCase("RGB/BGG/HVR", 2, 5)]
        [TestCase("RGB/BGG/HVT", 3, 9)]
        public void Plan_RocketChains_UseFrozenBoardAndUniqueOccupiedRemovals(string layout, int count, int removed)
        {
            var board = TestBoards.Create(layout.Split('/'));
            var before = Snapshot(board);
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });

            Assert.That(plan.Effects.Count, Is.EqualTo(count));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(removed));
            Assert.That(new HashSet<GridPosition>(plan.RemovedPositions).Count, Is.EqualTo(removed));
            Assert.That(plan.Effects[0].Type, Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(plan.Effects[1].Type, Is.EqualTo(TileType.VerticalRocket));
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void Plan_DuplicateInitialRequestsAndRepeatedHits_ActivateEachSpecialOnceInStableOrder()
        {
            var board = TestBoards.Create("RGB", "BGG", "HVT");
            var rocket = new SpecialActivation(new GridPosition(0, 0));
            var tnt = new SpecialActivation(new GridPosition(2, 0));
            var first = SpecialEffects.Plan(board, new[] { tnt, rocket, rocket });
            var second = SpecialEffects.Plan(board, new[] { rocket, tnt });
            var expected = new[] { new GridPosition(0, 0), new GridPosition(2, 0), new GridPosition(1, 0) };

            Assert.That(first.Effects.Count, Is.EqualTo(3));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(first.Effects[i].Origin, Is.EqualTo(expected[i]));
                Assert.That(second.Effects[i].Origin, Is.EqualTo(expected[i]));
                Assert.That(first.Effects[i].Footprint, Is.EqualTo(second.Effects[i].Footprint));
            }
            Assert.That(first.RemovedPositions, Is.EqualTo(second.RemovedPositions));
        }

        [Test]
        public void Plan_TntStartsChainAndInitialRequestsSortByYBeforeX()
        {
            var board = TestBoards.Create("RGB", "BGG", "HVT");
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(2, 0)) });
            Assert.That(plan.Effects.Count, Is.EqualTo(3));
            Assert.That(plan.Effects[0].Type, Is.EqualTo(TileType.TNT));
            Assert.That(plan.Effects[1].Type, Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(plan.Effects[2].Type, Is.EqualTo(TileType.VerticalRocket));

            board = TestBoards.Create("TGR", "RGH");
            plan = SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(0, 1)), new SpecialActivation(new GridPosition(2, 0))
            });
            Assert.That(plan.Effects[0].Origin, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(plan.Effects[1].Origin, Is.EqualTo(new GridPosition(0, 1)));
        }

        [Test]
        public void OverlappingEffects_ClearFactsIncludeSpecialsButResultContainsEachNormalOnce()
        {
            var board = TestBoards.Create("RGB", "BGG", "HVT");
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });
            var physical = BoardResolution.ClearCells(board, plan.RemovedPositions);
            var colors = new List<TileType>();
            foreach (TileType type in physical)
                if (TileRules.IsNormalMatchItem(type)) colors.Add(type);
            var result = new ResolutionResult(colors);

            Assert.That(physical.Count, Is.EqualTo(9));
            Assert.That(physical, Does.Contain(TileType.HorizontalRocket));
            Assert.That(physical, Does.Contain(TileType.VerticalRocket));
            Assert.That(physical, Does.Contain(TileType.TNT));
            Assert.That(result.ClearedCount, Is.EqualTo(6));
            Assert.That(result.CountOf(TileType.Red), Is.EqualTo(1));
            Assert.That(result.CountOf(TileType.Green), Is.EqualTo(3));
            Assert.That(result.CountOf(TileType.Blue), Is.EqualTo(2));
        }

        [Test]
        public void Plan_HolesAreExcludedAndReturnedCollectionsAreReadOnlySnapshots()
        {
            var board = TestBoards.Create("...", "..R", "H.V");
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(2, 0), new GridPosition(2, 1)
            }));
            Assert.Throws<NotSupportedException>(() => ((IList<GridPosition>)plan.RemovedPositions).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<SpecialEffect>)plan.Effects).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<GridPosition>)plan.Effects[0].Footprint).Clear());
            BoardResolution.ClearCells(board, plan.RemovedPositions);
            Assert.That(plan.Effects[0].Type, Is.EqualTo(TileType.HorizontalRocket));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(3));
        }

        [Test]
        public void Plan_PropellerWithoutTargetingStream_FailsBeforeBoardMutation()
        {
            var board = TestBoards.Create("HF");
            var before = Snapshot(board);
            Assert.Throws<ArgumentNullException>(() => SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(0, 0)) }));
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void Plan_EmptyAndInvalidRequests_HaveExplicitBehavior()
        {
            var board = TestBoards.Create("HR");
            Assert.That(SpecialEffects.Plan(board, new SpecialActivation[0]).Effects, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => SpecialEffects.Plan(null, new SpecialActivation[0]));
            Assert.Throws<ArgumentNullException>(() => SpecialEffects.Plan(board, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(2, 0)) }));
            Assert.Throws<ArgumentException>(() => SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(1, 0)) }));
        }

        private static TileType[,] Snapshot(Board board)
        {
            var result = new TileType[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    result[x, y] = board.GetTile(new GridPosition(x, y));
            return result;
        }
    }
}
