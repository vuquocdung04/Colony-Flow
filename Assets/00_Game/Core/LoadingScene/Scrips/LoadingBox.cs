using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBox : MonoBehaviour
{
    public Image fill;
    public CanvasGroup canvasGroup;
    public void Init()
    {
        fill.fillAmount = 0f;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public async Awaitable LoadingAsync(float target, float duration)
    {
        await fill.DOFillAmount(target, duration).SetEase(Ease.Linear).ToAwaitable();
    }

    public async Awaitable CloseAsync(float fadeOutDuration)
    {
        await canvasGroup.DOFade(0f, fadeOutDuration).ToAwaitable();
        gameObject.SetActive(false);
    }
}