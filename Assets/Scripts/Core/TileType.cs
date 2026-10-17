namespace Match3.Core
{
    // What occupies a cell. Pure data: colours/sprites live in the Presentation layer (TileView).
    public enum TileType
    {
        // Zero so a freshly allocated array or default(TileType) means "no tile".
        Empty = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Yellow = 4,
        Purple = 5,
        Orange = 6,
        HorizontalRocket = 7,
        VerticalRocket = 8,
        Propeller = 9,
        TNT = 10,
        LightBall = 11
    }
}
