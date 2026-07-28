public readonly struct BoosterActionInfo
{
    public readonly BoosterType Type;
    public readonly float Duration;

    public BoosterActionInfo(BoosterType type, float duration)
    {
        Type = type;
        Duration = duration;
    }
}
