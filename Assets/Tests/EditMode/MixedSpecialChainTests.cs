using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class MixedSpecialChainTests
    {
        [TestCase(0)] // Rocket starts the chain.
        [TestCase(2)] // TNT starts the chain.
        public void OverlappingMixedEffects_ActivateEachSpecialOnceAndCreditEachNormalCellOnce(int startX)
        {
            var board = TestBoards.Create("RGB", "GFL", "HVT");
            var plan = SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(startX, 0)) }, new Random(42));

            Assert.That(plan.Effects.Count, Is.EqualTo(5), "Both Rocket orientations and all other specials activate.");
            var origins = new HashSet<GridPosition>();
            foreach (SpecialEffect effect in plan.Effects)
                Assert.That(origins.Add(effect.Origin), Is.True, "A repeated hit must not activate twice.");
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(9));
            Assert.That(new HashSet<GridPosition>(plan.RemovedPositions).Count, Is.EqualTo(9));
            Assert.That(board.GetTile(new GridPosition(1, 1)), Is.EqualTo(TileType.Propeller), "Planning is read-only.");

            var physical = BoardResolution.ClearCells(board, plan.RemovedPositions);
            var normal = new List<TileType>();
            foreach (TileType type in physical)
                if (TileRules.IsNormalMatchItem(type)) normal.Add(type);
            var result = new ResolutionResult(normal);
            Assert.That(physical.Count, Is.EqualTo(9), "Physical facts include the five removed specials.");
            Assert.That(result.ClearedCount, Is.EqualTo(4));
            Assert.That(result.CountOf(TileType.Red), Is.EqualTo(1));
            Assert.That(result.CountOf(TileType.Green), Is.EqualTo(2));
            Assert.That(result.CountOf(TileType.Blue), Is.EqualTo(1));
        }

        [Test]
        public void PropellerRemoteLightBall_UsesChainFallbackAndLeavesOtherColorsAlone()
        {
            var board = TestBoards.Create("F.LRRGB");
            var plan = SpecialEffects.Plan(board,
                new[] { new SpecialActivation(new GridPosition(0, 0)) }, new FirstCandidateRandom());

            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.Effects[0].RemoteTarget, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(plan.Effects[1].Type, Is.EqualTo(TileType.LightBall));
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(2, 0),
                new GridPosition(3, 0), new GridPosition(4, 0)
            }));
        }

        private sealed class FirstCandidateRandom : Random
        {
            public override int Next(int maxValue) { return 0; }
        }
    }
}
