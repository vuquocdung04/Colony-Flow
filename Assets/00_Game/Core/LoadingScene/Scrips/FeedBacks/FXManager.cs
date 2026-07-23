using System;
using System.Collections.Generic;
using UnityEngine;
public partial class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }
    public CoinFlyFx coinFlyPrefab;
    public int coinCount = 10;
    public float spawnInterval = 0.05f;
    public float spreadRadius = 0.5f;
    public void Init()
    {
        Instance = this;
        HideAllTransitions();
    }

    public async Awaitable SpawnCoinFly(Vector3 spawnPos, Transform target, Action onEachArrived = null, Action onComplete = null)
    {
        var tasks = new List<Awaitable>(coinCount);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
            Vector3 pos = spawnPos + new Vector3(offset.x, offset.y, 0f);

            var coin = Instantiate(coinFlyPrefab, pos, Quaternion.identity);
            tasks.Add(coin.MoveTo(target, onEachArrived));

            if (spawnInterval > 0f && i < coinCount - 1)
                await Awaitable.WaitForSecondsAsync(spawnInterval);
        }

        await AwaitableEx.WhenAll(tasks);
        onComplete?.Invoke();
    }


}