using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class LightBallEffectsTests
    {
        [Test]
        public void ExplicitPartnerColor_ClearsOnlyThatColorAndTheLightBall()
        {
            var board = TestBoards.Create("RGLB", "BRGB");
            var plan = SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(2, 1), TileType.Blue)
            });
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[]
            {
                new GridPosition(0, 0), new GridPosition(3, 0),
                new GridPosition(2, 1), new GridPosition(3, 1)
            }));
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
            Assert.That(board.GetTile(new GridPosition(2, 1)), Is.EqualTo(TileType.LightBall));
        }

        [TestCase("GGLRR", TileType.Red)]
        [TestCase("BGLGG", TileType.Green)]
        public void NoPartner_UsesMostCommonNormalColorWithNumericTieBreak(string row, TileType expectedColor)
        {
            var board = TestBoards.Create(row);
            var origin = new GridPosition(2, 0);
            var footprint = SpecialEffects.GetFootprint(board, origin);
            var expected = new System.Collections.Generic.List<GridPosition> { origin };
            for (int x = 0; x < row.Length; x++)
                if (board.GetTile(new GridPosition(x, 0)) == expectedColor) expected.Add(new GridPosition(x, 0));
            Assert.That(footprint, Is.EquivalentTo(expected));
        }

        [Test]
        public void ExplicitColorWithoutMatches_DoesNotFallBackToAnotherColor()
        {
            var board = TestBoards.Create("GGL");
            Assert.That(SpecialEffects.GetFootprint(board, new GridPosition(2, 0), TileType.Red),
                Is.EqualTo(new[] { new GridPosition(2, 0) }));
        }

        [Test]
        public void NoNormalColors_ClearsOnlyItsOwnCell()
        {
            var board = TestBoards.Create("LFHL");
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });
            Assert.That(plan.RemovedPositions, Is.EqualTo(new[] { new GridPosition(0, 0) }));
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
        }

        [Test]
        public void ChainFallback_CountsNormalColorsAlreadyPendingRemoval()
        {
            var board = TestBoards.Create("GGBB.", "HRRRL");
            var plan = SpecialEffects.Plan(board, new[] { new SpecialActivation(new GridPosition(0, 0)) });
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(5), "The Light Ball must choose the three pending Reds.");
            foreach (GridPosition cell in plan.RemovedPositions) Assert.That(cell.Y, Is.Zero);
        }

        [Test]
        public void TwoLightBalls_CanChooseTheSameColorWithoutAComboBonus()
        {
            var board = TestBoards.Create("LLRRB");
            var plan = SpecialEffects.Plan(board, new[]
            {
                new SpecialActivation(new GridPosition(1, 0)), new SpecialActivation(new GridPosition(0, 0))
            });
            Assert.That(plan.Effects.Count, Is.EqualTo(2));
            Assert.That(plan.RemovedPositions.Count, Is.EqualTo(4));
            Assert.That(plan.RemovedPositions, Has.No.Member(new GridPosition(4, 0)));
        }

        [Test]
        public void InvalidTargetColor_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => SpecialEffects.GetFootprint(TestBoards.Create("L"),
                new GridPosition(0, 0), TileType.TNT));
        }
    }
}
