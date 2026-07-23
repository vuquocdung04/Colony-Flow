using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveManager
{
    private const int FileVersion = 1;

    private static readonly Dictionary<string, IBoxedSaveable> saveables = new();
    private static readonly HashSet<string> filenames = new();
    private static readonly HashSet<string> dirtyFiles = new();

    private interface IBoxedSaveable
    {
        string Key { get; }
        string Filename { get; }
        int Version { get; }
        Type StateType { get; }
        object CaptureState();
        void RestoreState(object state);
    }

    private sealed class BoxedSaveable<TState> : IBoxedSaveable
    {
        private readonly ISaveable<TState> inner;

        public BoxedSaveable(ISaveable<TState> saveable) => inner = saveable;

        public string Key => inner.Key;
        public string Filename => inner.Filename;
        public int Version => inner.Version;
        public Type StateType => typeof(TState);

        public object CaptureState() => inner.CaptureState();

        public void RestoreState(object state)
        {
            if (state is null)
            {
                inner.RestoreState(default);
                return;
            }
            inner.RestoreState((TState)state);
        }
    }

    public static void Register<TState>(ISaveable<TState> saveable)
    {
        if (saveable == null)
        {
            Debug.LogError("[SaveManager] Register: saveable is null.");
            return;
        }

        var boxed = new BoxedSaveable<TState>(saveable);
        if (!saveables.TryAdd(boxed.Key, boxed))
        {
            Debug.LogError($"[SaveManager] Duplicate key \"{boxed.Key}\".");
            return;
        }
        filenames.Add(boxed.Filename);
    }

    public static void MarkDirty(string filename) => dirtyFiles.Add(filename);

    private static async Awaitable SaveAsync(string filename, CancellationToken token = default)
    {
        dirtyFiles.Remove(filename);

        var array = new JArray();
        foreach (var saveable in saveables.Values)
        {
            if (saveable.Filename != filename) continue;

            object data;
            try
            {
                data = saveable.CaptureState();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] CaptureState failed for \"{saveable.Key}\": {e}");
                continue;
            }

            array.Add(new JObject
            {
                ["key"] = saveable.Key,
                ["version"] = saveable.Version,
                ["data"] = data == null ? JValue.CreateNull() : JToken.FromObject(data)
            });
        }

        var envelope = new JObject
        {
            ["fileVersion"] = FileVersion,
            ["saveables"] = array
        };

        try
        {
            await SaveIO.WriteAsync(filename, envelope.ToString(Formatting.Indented), token);
        }
        catch (Exception e)
        {
            dirtyFiles.Add(filename);
            Debug.LogError($"[SaveManager] Save \"{filename}\" failed: {e.Message}");
        }
    }

    private static bool isSaving;

    public static async Awaitable SaveDirtyAsync(CancellationToken token = default)
    {
        if (isSaving) return;
        isSaving = true;
        try
        {
            foreach (var filename in new List<string>(dirtyFiles))
                await SaveAsync(filename, token);
        }
        finally
        {
            isSaving = false;
        }
    }

    private static async Awaitable LoadAsync(string filename, CancellationToken token = default)
    {
        string content = null;
        try
        {
            content = await SaveIO.ReadAsync(filename, token);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Read \"{filename}\" failed: {e.Message}");
        }

        var loaded = new Dictionary<string, (int version, JToken data)>();
        if (!string.IsNullOrEmpty(content))
        {
            try
            {
                var envelope = JObject.Parse(content);
                if (envelope["saveables"] is JArray array)
                {
                    foreach (var item in array)
                    {
                        string key = item["key"]?.ToString();
                        if (string.IsNullOrEmpty(key)) continue;
                        loaded[key] = (item["version"]?.Value<int?>() ?? 0, item["data"]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Parse \"{filename}\" failed: {e.Message}");
                loaded.Clear();
            }
        }

        foreach (var saveable in saveables.Values)
        {
            if (saveable.Filename != filename) continue;

            if (!loaded.TryGetValue(saveable.Key, out var entry))
            {
                saveable.RestoreState(null);
                continue;
            }

            if (entry.version != saveable.Version)
            {
                Debug.LogWarning($"[SaveManager] Version mismatch for \"{saveable.Key}\" (file: {entry.version}, code: {saveable.Version}). Using defaults.");
                saveable.RestoreState(null);
                continue;
            }

            try
            {
                object state = entry.data == null || entry.data.Type == JTokenType.Null
                    ? null
                    : entry.data.ToObject(saveable.StateType);
                saveable.RestoreState(state);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Restore failed for \"{saveable.Key}\": {e.Message}. Using defaults.");
                saveable.RestoreState(null);
            }
        }

        dirtyFiles.Remove(filename);
    }

    public static async Awaitable LoadAllAsync(CancellationToken token = default)
    {
        foreach (var filename in filenames)
            await LoadAsync(filename, token);
    }

    public static void DeleteAll()
    {
        foreach (var filename in filenames)
            SaveIO.Delete(filename);
        dirtyFiles.Clear();
    }

#if UNITY_EDITOR
    public readonly struct EditorSaveable
    {
        public readonly string Key;
        public readonly string Filename;
        public readonly Type StateType;
        public readonly object State;

        public EditorSaveable(string key, string filename, Type stateType, object state)
        {
            Key = key;
            Filename = filename;
            StateType = stateType;
            State = state;
        }
    }

    public static List<EditorSaveable> EditorGetAll()
    {
        var list = new List<EditorSaveable>(saveables.Count);
        foreach (var s in saveables.Values)
        {
            object state = null;
            try { state = s.CaptureState(); }
            catch (Exception e) { Debug.LogError($"[SaveManager] Editor capture failed for \"{s.Key}\": {e.Message}"); }

            list.Add(new EditorSaveable(s.Key, s.Filename, s.StateType, state));
        }
        return list;
    }

    public static void EditorApply(string key, object state)
    {
        if (!saveables.TryGetValue(key, out var s)) return;
        s.RestoreState(state);
        MarkDirty(s.Filename);
    }
#endif
}
