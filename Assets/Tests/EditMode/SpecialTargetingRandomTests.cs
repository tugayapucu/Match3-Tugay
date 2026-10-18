using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialTargetingRandomTests
    {
        [Test]
        public void FreshTargetingStream_ReplaysTheWholeSessionsChoices()
        {
            const int seed = 42;
            var board = TestBoards.Create("RGB", "YFO", "BGR");
            var first = Targets(board, new Random(seed ^ 0x5EED1234));
            var restarted = Targets(board, new Random(seed ^ 0x5EED1234));

            Assert.That(restarted, Is.EqualTo(first));
            Assert.That(new HashSet<GridPosition>(first).Count, Is.GreaterThan(1),
                "The test should replay a sequence, not repeatedly assert one possible target.");
        }

        [Test]
        public void TargetingDraws_DoNotChangeRefillOnTheSameEmptyBoard()
        {
            var refill = new Random(19);
            var targeting = new Random(19 ^ 0x5EED1234);
            Targets(TestBoards.Create("RGB", "YFO", "BGR"), targeting);

            var afterTargeting = new Board(4, 4);
            var baseline = new Board(4, 4);
            var palette = new[] { TileType.Red, TileType.Green, TileType.Blue };
            BoardResolution.Refill(afterTargeting, palette, refill);
            BoardResolution.Refill(baseline, palette, new Random(19));

            for (int y = 0; y < baseline.Height; y++)
                for (int x = 0; x < baseline.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    Assert.That(afterTargeting.GetTile(cell), Is.EqualTo(baseline.GetTile(cell)));
                }
        }

        private static List<GridPosition> Targets(Board board, Random targeting)
        {
            var choices = new List<GridPosition>();
            var requests = new[] { new SpecialActivation(new GridPosition(1, 1)) };
            for (int i = 0; i < 12; i++)
            {
                var plan = SpecialEffects.Plan(board, requests, targeting);
                Assert.That(plan.Effects[0].RemoteTarget.HasValue, Is.True);
                choices.Add(plan.Effects[0].RemoteTarget.Value);
            }
            return choices;
        }
    }
}
