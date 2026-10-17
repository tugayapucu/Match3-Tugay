using System;

namespace Match3.Core
{
    // The rules of one level: moves, score, a single collection goal, and the Won / Lost outcome.
    // Pure C#: it only receives facts about finished moves, so it is fully testable without Unity.
    public sealed class LevelProgress
    {
        private readonly int pointsPerClearedTile;

        public LevelProgress(int startingMoveCount, TileType goalTileType, int goalTarget, int pointsPerClearedTile)
        {
            if (startingMoveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingMoveCount), startingMoveCount, "Starting move count must be positive.");
            }

            // Only normal colors are collectible; palette membership is the composition root's check.
            if (!TileRules.IsNormalMatchItem(goalTileType))
            {
                throw new ArgumentException("The goal must be a normal collectible color.", nameof(goalTileType));
            }

            if (goalTarget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(goalTarget), goalTarget, "The goal target must be positive.");
            }

            if (pointsPerClearedTile < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pointsPerClearedTile), pointsPerClearedTile, "Points per cleared tile cannot be negative.");
            }

            MovesRemaining = startingMoveCount;
            GoalTileType = goalTileType;
            GoalTarget = goalTarget;
            this.pointsPerClearedTile = pointsPerClearedTile;
            Status = LevelStatus.Playing;
        }

        public int MovesRemaining { get; private set; }
        public int Score { get; private set; }
        public TileType GoalTileType { get; }
        public int GoalTarget { get; }
        // Capped at GoalTarget; Score still counts every cleared tile.
        public int GoalProgress { get; private set; }
        public LevelStatus Status { get; private set; }

        // Called once per valid player move, after its whole resolution (all cascades) has finished.
        public void ApplyValidMove(ResolutionResult result)
        {
            // Check everything first, so a rejected call changes nothing.
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // A finished level accepting another move is a caller bug: fail loudly instead of ignoring it.
            if (Status != LevelStatus.Playing)
            {
                throw new InvalidOperationException($"The level is already {Status}; no more moves can be applied.");
            }

            // Compute first, assign after: if the checked score overflows, nothing has changed yet.
            // checked: an overflowing score fails loudly instead of silently wrapping to a negative number.
            int newScore = checked(Score + result.ClearedCount * pointsPerClearedTile);
            int newGoalProgress = Math.Min(GoalTarget, GoalProgress + result.CountOf(GoalTileType));

            // One player action costs exactly one move, however many cascades it caused.
            MovesRemaining--;
            Score = newScore;
            GoalProgress = newGoalProgress;

            // Win is checked first, so a last move that completes the goal is a win, not a loss.
            if (GoalProgress >= GoalTarget)
            {
                Status = LevelStatus.Won;
            }
            else if (MovesRemaining == 0)
            {
                Status = LevelStatus.Lost;
            }
        }
    }
}
