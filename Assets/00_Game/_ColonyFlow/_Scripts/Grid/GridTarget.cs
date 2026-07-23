public enum GridApproach
{
    Left,
    Right,
    Top,
    Bottom,
}

public struct GridTarget
{
    public int index;
    public int x;
    public int y;
    public GridApproach approach;

    public static GridTarget None => new GridTarget { index = -1, x = -1, y = -1 };

    public bool IsValid => index >= 0;
}
