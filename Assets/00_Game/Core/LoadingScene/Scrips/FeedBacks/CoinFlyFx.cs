using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CoinFlyFx : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public List<Sprite> sprites;
    public float frameDuration = 0.1f;
    public float scaleUpDuration = 0.2f;
    public float flyDelay = 0.1f;
    public float moveDuration = 0.5f;

    Vector3 originalScale;

    void OnEnable()
    {
        originalScale = transform.localScale;
        AnimateSprites().Forget();
    }

    async Awaitable AnimateSprites()
    {
        if (sprites == null || sprites.Count == 0) return;

        var token = destroyCancellationToken;
        int index = 0;
        while (!token.IsCancellationRequested)
        {
            spriteRenderer.sprite = sprites[index];
            index = (index + 1) % sprites.Count;
            await Awaitable.WaitForSecondsAsync(frameDuration, token);
        }
    }

    public async Awaitable MoveTo(Transform target, Action onArrived = null)
    {
        var token = destroyCancellationToken;

        transform.localScale = Vector3.zero;
        await transform.DOScale(originalScale, scaleUpDuration).SetEase(Ease.OutBack).ToAwaitable(token);

        await Awaitable.WaitForSecondsAsync(flyDelay, token);

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        await transform.DOMove(target.position, moveDuration).SetEase(Ease.InQuad).ToAwaitable(token);

        onArrived?.Invoke();
        Destroy(gameObject);
    }
}