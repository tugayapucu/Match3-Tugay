using System;
using Match3.Core;
using UnityEngine;

namespace Match3.Presentation
{
    // Inputs to one session. Gameplay validation belongs to the existing session construction path.
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField] private int startingMoveCount;
        [SerializeField] private TileType goalTileType;
        [SerializeField] private int goalTarget;
        [SerializeField] private int seed;

        public LevelDefinition(int startingMoveCount, TileType goalTileType, int goalTarget, int seed)
        {
            this.startingMoveCount = startingMoveCount;
            this.goalTileType = goalTileType;
            this.goalTarget = goalTarget;
            this.seed = seed;
        }

        public int StartingMoveCount => startingMoveCount;
        public TileType GoalTileType => goalTileType;
        public int GoalTarget => goalTarget;
        public int Seed => seed;
    }
}
