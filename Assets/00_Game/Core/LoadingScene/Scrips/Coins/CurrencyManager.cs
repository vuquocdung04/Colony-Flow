using System;
using EventDispatcher;
using UnityEngine;
public enum CurrencyType
{
    Coin,
    Gem
}
public static class NumberFormatter
{
    public static string Format(double value)
    {
        double abs = System.Math.Abs(value);
        if (abs < 10000) return ((long)value).ToString();
        if (abs < 1_000_000) return $"{value / 1_000.0:0.0}K";
        if (abs < 1_000_000_000) return $"{value / 1_000_000.0:0.0}M";
        if (abs < 1_000_000_000_000) return $"{value / 1_000_000_000.0:0.0}B";
        return $"{value / 1_000_000_000_000.0:0.0}T";
    }
}

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private ToastManager toastManager;

    public void Init(ToastManager toastManager)
    {
        Instance = this;
        this.toastManager = toastManager;
    }

    public int Get(CurrencyType type) => GetPref(type).Value;

    public bool TrySpend(CurrencyType type, int amount)
    {
        if (amount <= 0) return true;

        var pref = GetPref(type);
        if (pref.Value < amount)
        {
            toastManager.ShowToast($"Not enough {type}");
            return false;
        }

        pref.Value -= amount;
        PostChangeEvent(type);
        return true;
    }

    public void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;

        var pref = GetPref(type);
        pref.Value += amount;
        PostChangeEvent(type);
    }

    public bool CanAfford(CurrencyType type, int amount)
    {
        return GetPref(type).Value >= amount;
    }

    private PrefVar<int> GetPref(CurrencyType type) => type switch
    {
        CurrencyType.Coin => UseProfile.Coin,
        //CurrencyType.Gem => UseProfile.Gem,  // thêm vào UseProfile khi cần
        _ => throw new ArgumentException($"Unknown currency: {type}")
    };

    private void PostChangeEvent(CurrencyType type)
    {
        EventID eventId = type switch
        {
            CurrencyType.Coin => EventID.CHANGE_COIN,
            //CurrencyType.Gem => EventID.CHANGE_GEM,  // thêm vào EventID khi cần
            _ => EventID.CHANGE_COIN
        };
        this.PostEvent(eventId);
    }
}