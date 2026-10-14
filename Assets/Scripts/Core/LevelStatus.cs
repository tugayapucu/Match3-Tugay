namespace Match3.Core
{
    // Whether the level can continue. Separate from the input lock: this describes the level, not one move.
    public enum LevelStatus
    {
        Playing,
        Won,
        Lost
    }
}
