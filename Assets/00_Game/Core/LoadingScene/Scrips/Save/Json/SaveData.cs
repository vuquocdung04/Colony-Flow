using System.Collections.Generic;

public abstract class MultiSaveData<TSelf, TId, TRecord> : ISaveable<Dictionary<TId, TRecord>>
    where TSelf : MultiSaveData<TSelf, TId, TRecord>, new()
    where TRecord : class, new()
{
    public static TSelf Instance { get; } = new();

    protected Dictionary<TId, TRecord> records = new();

    public abstract string Key { get; }
    public virtual string Filename => SaveFiles.PROGRESS;
    public virtual int Version => 1;

    public Dictionary<TId, TRecord> CaptureState() => new(records);

    public void RestoreState(Dictionary<TId, TRecord> state) => records = state ?? new();

    public static IReadOnlyDictionary<TId, TRecord> All => Instance.records;

    public static bool Has(TId id) => Instance.records.ContainsKey(id);

    public static TRecord Get(TId id)
    {
        if (!Instance.records.TryGetValue(id, out var record))
        {
            record = new TRecord();
            Instance.records[id] = record;
            MarkDirty();
        }
        return record;
    }

    public static void MarkDirty() => SaveManager.MarkDirty(Instance.Filename);
}

public abstract class SingleSaveData<TSelf, TState> : ISaveable<TState>
    where TSelf : SingleSaveData<TSelf, TState>, new()
    where TState : class, new()
{
    public static TSelf Instance { get; } = new();

    protected TState state = new();

    public abstract string Key { get; }
    public virtual string Filename => SaveFiles.PROGRESS;
    public virtual int Version => 1;

    public TState CaptureState() => state;

    public void RestoreState(TState loaded) => state = loaded ?? new TState();

    public static TState Data => Instance.state;

    public static void MarkDirty() => SaveManager.MarkDirty(Instance.Filename);
}
