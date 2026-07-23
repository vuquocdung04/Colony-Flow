using System;
using System.Threading;
using EventDispatcher;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    public static HeartManager Instance { get; private set; }

    public const string FULL_LABEL = "FULL";

    public int MaxHearts { get; private set; }
    public int RefillMinutes { get; private set; }
    public double RefillSeconds { get; private set; }

    public int CurrentHeart => UseProfile.Heart;
    public bool IsUnlimited => UseProfile.IsUnlimitedHeart;
    public bool IsFull => CurrentHeart >= MaxHearts;

    private readonly NormalHeartState _normalState = new();
    private readonly UnlimitedHeartState _unlimitedState = new();
    private HeartState _current;

    private CancellationTokenSource cts;
    private ToastManager toastManager;

    public void Init(ToastManager toastManager)
    {
        Instance = this;
        this.toastManager = toastManager;

        MaxHearts = 5;
        RefillMinutes = 30;
        RefillSeconds = RefillMinutes * 60;

        cts?.Dispose();
        cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        var startState = UseProfile.IsUnlimitedHeart ? (HeartState)_unlimitedState : _normalState;
        ChangeState(startState);

        TickLoop(cts.Token).Forget();
    }

    private async Awaitable TickLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await AwaitableEx.WaitRealtimeAsync(1f, token);
                _current.Tick();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception e) { Debug.LogError($"Lỗi TickLoop: {e}"); }
        }
    }

    public void ChangeState(HeartState next)
    {
        if (_current == next) return;
        _current?.OnExit();
        _current = next;
        _current.OnEnter(this);
    }

    public void SwitchToNormal() => ChangeState(_normalState);
    public void SwitchToUnlimited() => ChangeState(_unlimitedState);

    public double GetTimeToNextHeart() => _current.GetTimeToNextHeart();
    public TimeSpan GetUnlimitedTimeRemaining() => _current.GetUnlimitedTimeRemaining();

    // ========== PUBLIC API ==========

    public bool TryUseHeart()
    {
        // return true; // ← uncomment để tester không bị trừ heart
        return _current.TryUseHeart();
    }

    public bool TryAddHeart(int amount = 1)
    {
        if (IsUnlimited)
        {
            toastManager.ShowToast("You have unlimited hearts");
            return false;
        }

        if (IsFull)
        {
            toastManager.ShowToast("Heart is full");
            return false;
        }

        int newCount = Mathf.Min(CurrentHeart + amount, MaxHearts);
        UseProfile.Heart.Value = newCount;
        return true;
    }

    public void TryAddUnlimited(int minutes)
    {
        _current.AddUnlimited(minutes);
    }

    public void TryShowHeartOffer(Transform parent)
    {
        if (IsUnlimited)
        {
            toastManager.ShowToast("You have unlimited hearts");
            return;
        }

        if (IsFull)
        {
            toastManager.ShowToast("Heart is full");
            return;
        }

        MoreLivesBox.Setup(parent, box => box.Show()).Forget();
    }
    // ========== INTERNAL ==========

    public void RefillOfflineHearts()
    {
        if (UseProfile.Heart >= MaxHearts) return;

        DateTime now = TimeManager.GetCurrentTime();
        TimeSpan timePassed = now - UseProfile.TimeLastOverHeart;

        int heartsGained = (int)(timePassed.TotalSeconds / RefillSeconds);
        if (heartsGained <= 0) return;

        int newCount = UseProfile.Heart + heartsGained;
        if (newCount >= MaxHearts)
        {
            UseProfile.Heart.Value = MaxHearts;
        }
        else
        {
            UseProfile.Heart.Value = newCount;
            UseProfile.TimeLastOverHeart = UseProfile.TimeLastOverHeart
                .Add(TimeSpan.FromSeconds(heartsGained * RefillSeconds));
        }
        this.PostEvent(EventID.CHANGE_HEART);
    }

    public Cooldown HeartTimer()
    {
        if (IsUnlimited)
            return Cooldown.Until(UseProfile.TimeUnlimitedHeart);

        RefillOfflineHearts();
        return IsFull ? Cooldown.Done : Cooldown.InSeconds(GetTimeToNextHeart());
    }
}