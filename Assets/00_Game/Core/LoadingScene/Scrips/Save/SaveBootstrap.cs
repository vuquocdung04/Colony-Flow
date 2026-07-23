using System;
using UnityEngine;

public class SaveBootstrap : MonoBehaviour
{
    [SerializeField] private float autoSaveInterval = 10f;

    public static bool IsReady { get; private set; }

    public async Awaitable Init()
    {
        SaveManager.Register(GamePrefs.Instance);
        SaveManager.Register(BoosterData.Instance);

        await SaveManager.LoadAllAsync(destroyCancellationToken);

        IsReady = true;
        Debug.Log("[SaveBootstrap] Init done.");

        AutoSaveLoop().Forget();
    }

    private async Awaitable AutoSaveLoop()
    {
        try
        {
            while (true)
            {
                await Awaitable.WaitForSecondsAsync(autoSaveInterval, destroyCancellationToken);
                SaveNow();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    public static void SaveNow()
    {
        if (!IsReady) return;
        _ = SaveManager.SaveDirtyAsync();
    }

    public static void DeleteAllData()
    {
        GamePrefs.ClearAll();
        SaveManager.DeleteAll();
    }
}
