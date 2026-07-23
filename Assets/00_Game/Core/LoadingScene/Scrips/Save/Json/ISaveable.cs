public static class SaveFiles
{
    public const string GAME_DATA = "game_data";
    public const string PROGRESS = "progress";
}

public interface ISaveable<TState>
{
    string Key { get; }
    string Filename { get; }
    int Version { get; }
    TState CaptureState();
    void RestoreState(TState state);
}
