using System;
using System.Collections.Generic;
using UnityEngine;

public class GamePrefs : ISaveable<Dictionary<string, object>>
{
    public static GamePrefs Instance { get; } = new();

    private static Dictionary<string, object> cache = new();

    public string Key => "game_prefs";
    public string Filename => SaveFiles.GAME_DATA;
    public int Version => 1;

    public Dictionary<string, object> CaptureState() => new(cache);

    public void RestoreState(Dictionary<string, object> state)
    {
        cache = state ?? new Dictionary<string, object>();
        Debug.Log($"[GamePrefs] Restored {cache.Count} keys.");
    }

    public static void Set<T>(string key, T value)
    {
        if (cache.TryGetValue(key, out object existingValue))
            if (existingValue != null && existingValue.Equals(value)) return;

        cache[key] = value;
        SaveManager.MarkDirty(SaveFiles.GAME_DATA);
    }

    public static T Get<T>(string key, T defaultValue = default)
    {
        if (cache.TryGetValue(key, out object value))
        {
            try
            {
                if (value is Newtonsoft.Json.Linq.JToken token)
                    return token.ToObject<T>();
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public static void ClearAll()
    {
        cache.Clear();
        SaveManager.MarkDirty(SaveFiles.GAME_DATA);
    }
}

public class PrefVar<T>
{
    private string key;
    private T defaultValue;

    public PrefVar(string newKey, T newDefaultValue = default)
    {
        key = newKey;
        defaultValue = newDefaultValue;
    }

    public T Value
    {
        get => GamePrefs.Get<T>(key, defaultValue);
        set => GamePrefs.Set<T>(key, value);
    }

    public static implicit operator T(PrefVar<T> prefVar) => prefVar.Value;
}
