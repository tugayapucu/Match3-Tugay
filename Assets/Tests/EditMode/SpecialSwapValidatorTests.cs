using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class SpecialSwapValidatorTests
    {
        [TestCase(TileType.HorizontalRocket, false)]
        [TestCase(TileType.HorizontalRocket, true)]
        [TestCase(TileType.VerticalRocket, false)]
        [TestCase(TileType.VerticalRocket, true)]
        [TestCase(TileType.TNT, false)]
        [TestCase(TileType.TNT, true)]
        [TestCase(TileType.Propeller, false)]
        [TestCase(TileType.Propeller, true)]
        [TestCase(TileType.LightBall, false)]
        [TestCase(TileType.LightBall, true)]
        public void SpecialWithNormal_IsAcceptedAtItsPostSwapCell(TileType special, bool reverse)
        {
            var board = TestBoards.Create("RG");
            var first = new GridPosition(reverse ? 1 : 0, 0);
            var second = new GridPosition(reverse ? 0 : 1, 0);
            board.SetTile(first, special);
            Assert.That(board.TrySwapAdjacent(first, second), Is.True);

            var analysis = SwapValidator.Analyze(board, first, second);

            Assert.That(analysis.Kind, Is.EqualTo(SwapKind.SpecialSwap));
            Assert.That(analysis.First, Is.EqualTo(first));
            Assert.That(analysis.Second, Is.EqualTo(second));
            Assert.That(analysis.InitialActivations.Count, Is.EqualTo(1));
            Assert.That(analysis.InitialActivations[0].Position, Is.EqualTo(second));
            Assert.That(analysis.InitialActivations[0].TargetColor,
                Is.EqualTo(special == TileType.LightBall ? board.GetTile(first) : (TileType?)null));
            Assert.That(board.GetTile(second), Is.EqualTo(special), "Analysis must not change the Board.");
            Assert.That(SwapValidator.IsValidResult(board, first, second), Is.True);
        }

        [TestCase(TileType.HorizontalRocket)]
        [TestCase(TileType.VerticalRocket)]
        [TestCase(TileType.TNT)]
        [TestCase(TileType.Propeller)]
        [TestCase(TileType.LightBall)]
        public void SelectingNormalFirst_ActivatesSpecialAtTheFirstCellAfterSwap(TileType special)
        {
            var board = TestBoards.Create("RR");
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            board.SetTile(second, special);
            board.TrySwapAdjacent(first, second);

            var analysis = SwapValidator.Analyze(board, first, second);

            Assert.That(analysis.Kind, Is.EqualTo(SwapKind.SpecialSwap));
            Assert.That(analysis.InitialActivations.Count, Is.EqualTo(1));
            Assert.That(analysis.InitialActivations[0].Position, Is.EqualTo(first));
            Assert.That(analysis.InitialActivations[0].TargetColor,
                Is.EqualTo(special == TileType.LightBall ? TileType.Red : (TileType?)null));
        }

        [TestCase("HL")]
        [TestCase("LL")]
        [TestCase("FT")]
        [TestCase("VH")]
        public void TwoSpecials_QueueBothWithoutPartnerColorsOrCombinationBonus(string row)
        {
            var board = TestBoards.Create(row);
            var first = new GridPosition(0, 0);
            var second = new GridPosition(1, 0);
            board.TrySwapAdjacent(first, second);

            var analysis = SwapValidator.Analyze(board, first, second);

            Assert.That(analysis.Kind, Is.EqualTo(SwapKind.SpecialSwap));
            Assert.That(analysis.InitialActivations.Count, Is.EqualTo(2));
            Assert.That(analysis.InitialActivations[0].Position, Is.EqualTo(first));
            Assert.That(analysis.InitialActivations[1].Position, Is.EqualTo(second));
            foreach (SpecialActivation activation in analysis.InitialActivations)
                Assert.That(activation.TargetColor, Is.Null);
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        public void SpecialDoesNotPermitSameDiagonalOrDistantSelections(int x, int y)
        {
            var board = TestBoards.Create("RGB", "HGB");
            Assert.That(SwapValidator.Analyze(board, new GridPosition(0, 0), new GridPosition(x, y)).Kind,
                Is.EqualTo(SwapKind.Invalid));
        }

        [Test]
        public void SpecialWithEmpty_IsNotAPlayerAction()
        {
            Assert.That(SwapValidator.Analyze(TestBoards.Create("H."),
                new GridPosition(0, 0), new GridPosition(1, 0)).Kind, Is.EqualTo(SwapKind.Invalid));
        }
    }
}
