using System;
using NUnit.Framework;

namespace Match3.Core.Tests
{
    public class LevelProgressTests
    {
        [TestCase(TileType.Red)]
        [TestCase(TileType.Green)]
        [TestCase(TileType.Blue)]
        [TestCase(TileType.Yellow)]
        [TestCase(TileType.Purple)]
        [TestCase(TileType.Orange)]
        public void NormalColor_CanBeCollectedAsAGoal(TileType color)
        {
            var progress = new LevelProgress(1, color, 1, 100);

            progress.ApplyValidMove(Result(color));

            Assert.That(progress.GoalProgress, Is.EqualTo(1));
            Assert.That(progress.Score, Is.EqualTo(100));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Constructor_SetsTheStartingValues()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);

            Assert.That(progress.MovesRemaining, Is.EqualTo(20));
            Assert.That(progress.Score, Is.Zero);
            Assert.That(progress.GoalTileType, Is.EqualTo(TileType.Red));
            Assert.That(progress.GoalTarget, Is.EqualTo(25));
            Assert.That(progress.GoalProgress, Is.Zero);
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void ApplyValidMove_ConsumesExactlyOneMove()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);

            progress.ApplyValidMove(Result(TileType.Green, TileType.Green, TileType.Green));

            Assert.That(progress.MovesRemaining, Is.EqualTo(19));
        }

        [Test]
        public void ApplyValidMove_ScoresEveryClearedTile()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red, TileType.Blue, TileType.Green));

            Assert.That(progress.Score, Is.EqualTo(500));
        }

        [Test]
        public void ApplyValidMove_OnlyGoalTilesAdvanceTheGoal_ButAllTilesScore()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Green, TileType.Blue, TileType.Green));

            Assert.That(progress.GoalProgress, Is.EqualTo(1));
            Assert.That(progress.Score, Is.EqualTo(400));
        }

        [Test]
        public void ApplyValidMove_CascadeClearsInTheSameResult_CountNormally_ForOneMove()
        {
            // One player move whose cascades cleared 3 Red, then 3 Blue, then 3 more Red (aggregated).
            var progress = new LevelProgress(20, TileType.Red, 25, 10);

            progress.ApplyValidMove(Result(
                TileType.Red, TileType.Red, TileType.Red,
                TileType.Blue, TileType.Blue, TileType.Blue,
                TileType.Red, TileType.Red, TileType.Red));

            Assert.That(progress.MovesRemaining, Is.EqualTo(19), "Cascades cost no extra moves.");
            Assert.That(progress.GoalProgress, Is.EqualTo(6));
            Assert.That(progress.Score, Is.EqualTo(90));
        }

        [Test]
        public void ApplyValidMove_SeveralMoves_Accumulate()
        {
            var progress = new LevelProgress(20, TileType.Red, 25, 100);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red));
            progress.ApplyValidMove(Result(TileType.Green, TileType.Red, TileType.Green));

            Assert.That(progress.MovesRemaining, Is.EqualTo(18));
            Assert.That(progress.Score, Is.EqualTo(600));
            Assert.That(progress.GoalProgress, Is.EqualTo(4));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Playing));
        }

        [Test]
        public void ApplyValidMove_ReachingTheGoalWithMovesLeft_Wins()
        {
            var progress = new LevelProgress(10, TileType.Red, 3, 100);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red));

            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(progress.MovesRemaining, Is.EqualTo(9));
        }

        [Test]
        public void ApplyValidMove_LastMoveCompletesTheGoal_WinsNotLoses()
        {
            // The plan's example: 1 move left, goal 23 / 25, the move clears 2 Red.
            var progress = new LevelProgress(2, TileType.Red, 25, 100);
            progress.ApplyValidMove(Result(Repeat(TileType.Red, 23)));
            Assert.That(progress.GoalProgress, Is.EqualTo(23));
            Assert.That(progress.MovesRemaining, Is.EqualTo(1));

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Green));

            Assert.That(progress.MovesRemaining, Is.Zero);
            Assert.That(progress.GoalProgress, Is.EqualTo(25));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void ApplyValidMove_LastMoveMissesTheGoal_Loses()
        {
            var progress = new LevelProgress(1, TileType.Red, 25, 100);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red));

            Assert.That(progress.MovesRemaining, Is.Zero);
            Assert.That(progress.GoalProgress, Is.EqualTo(3));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Lost));
        }

        [Test]
        public void ApplyValidMove_GoalProgressIsCapped_ButScoreCountsEverything()
        {
            var progress = new LevelProgress(10, TileType.Red, 3, 100);

            progress.ApplyValidMove(Result(Repeat(TileType.Red, 5)));

            Assert.That(progress.GoalProgress, Is.EqualTo(3));
            Assert.That(progress.Score, Is.EqualTo(500));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void ApplyValidMove_ZeroPointsPerTile_IsAllowed()
        {
            var progress = new LevelProgress(10, TileType.Red, 25, 0);

            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red));

            Assert.That(progress.Score, Is.Zero);
            Assert.That(progress.GoalProgress, Is.EqualTo(3));
        }

        [Test]
        public void ApplyValidMove_AfterWon_ThrowsAndChangesNothing()
        {
            var progress = new LevelProgress(10, TileType.Red, 3, 100);
            progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Won));

            Assert.Throws<InvalidOperationException>(() => progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red)));

            AssertUnchanged(progress, moves: 9, score: 300, goal: 3, status: LevelStatus.Won);
        }

        [Test]
        public void ApplyValidMove_AfterLost_ThrowsAndChangesNothing()
        {
            var progress = new LevelProgress(1, TileType.Red, 25, 100);
            progress.ApplyValidMove(Result(TileType.Green, TileType.Green, TileType.Green));
            Assert.That(progress.Status, Is.EqualTo(LevelStatus.Lost));

            Assert.Throws<InvalidOperationException>(() => progress.ApplyValidMove(Result(TileType.Red, TileType.Red, TileType.Red)));

            AssertUnchanged(progress, moves: 0, score: 300, goal: 0, status: LevelStatus.Lost);
        }

        [Test]
        public void ApplyValidMove_NullResult_ThrowsAndChangesNothing()
        {
            var progress = new LevelProgress(10, TileType.Red, 25, 100);

            Assert.Throws<ArgumentNullException>(() => progress.ApplyValidMove(null));

            AssertUnchanged(progress, moves: 10, score: 0, goal: 0, status: LevelStatus.Playing);
        }

        [Test]
        public void ApplyValidMove_ScoreOverflow_ThrowsAndChangesNothing()
        {
            var progress = new LevelProgress(10, TileType.Red, 25, int.MaxValue);

            Assert.Throws<OverflowException>(() => progress.ApplyValidMove(Result(TileType.Red, TileType.Green)));

            AssertUnchanged(progress, moves: 10, score: 0, goal: 0, status: LevelStatus.Playing);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveStartingMoveCount_Throws(int startingMoveCount)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgress(startingMoveCount, TileType.Red, 25, 100));
            Assert.That(exception.ParamName, Is.EqualTo("startingMoveCount"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveGoalTarget_Throws(int goalTarget)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgress(20, TileType.Red, goalTarget, 100));
            Assert.That(exception.ParamName, Is.EqualTo("goalTarget"));
        }

        [Test]
        public void Constructor_NegativePoints_Throws()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new LevelProgress(20, TileType.Red, 25, -1));
            Assert.That(exception.ParamName, Is.EqualTo("pointsPerClearedTile"));
        }

        [TestCase(TileType.Empty)]
        [TestCase(TileType.HorizontalRocket)]
        [TestCase(TileType.VerticalRocket)]
        [TestCase(TileType.Propeller)]
        [TestCase(TileType.TNT)]
        [TestCase(TileType.LightBall)]
        [TestCase((TileType)99)]
        public void Constructor_NonNormalGoalType_Throws(TileType goalTileType)
        {
            var exception = Assert.Throws<ArgumentException>(() => new LevelProgress(20, goalTileType, 25, 100));
            Assert.That(exception.ParamName, Is.EqualTo("goalTileType"));
        }

        private static ResolutionResult Result(params TileType[] clearedTiles)
        {
            return new ResolutionResult(clearedTiles);
        }

        private static TileType[] Repeat(TileType tileType, int count)
        {
            var tiles = new TileType[count];
            for (int i = 0; i < count; i++)
            {
                tiles[i] = tileType;
            }

            return tiles;
        }

        private static void AssertUnchanged(LevelProgress progress, int moves, int score, int goal, LevelStatus status)
        {
            Assert.That(progress.MovesRemaining, Is.EqualTo(moves));
            Assert.That(progress.Score, Is.EqualTo(score));
            Assert.That(progress.GoalProgress, Is.EqualTo(goal));
            Assert.That(progress.Status, Is.EqualTo(status));
        }
    }
}
