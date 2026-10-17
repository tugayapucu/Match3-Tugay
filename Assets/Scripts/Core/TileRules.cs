namespace Match3.Core
{
    // A cell can be occupied without holding a normal matching or collectible color.
    public static class TileRules
    {
        public static bool IsEmpty(TileType type)
        {
            return type == TileType.Empty;
        }

        public static bool IsNormalMatchItem(TileType type)
        {
            switch (type)
            {
                case TileType.Red:
                case TileType.Green:
                case TileType.Blue:
                case TileType.Yellow:
                case TileType.Purple:
                case TileType.Orange:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSpecial(TileType type)
        {
            switch (type)
            {
                case TileType.HorizontalRocket:
                case TileType.VerticalRocket:
                case TileType.Propeller:
                case TileType.TNT:
                case TileType.LightBall:
                    return true;
                default:
                    return false;
            }
        }
    }
}
