using DG.Tweening;
using UnityEngine;

public class ScaleAnim : IShowAnimation
{
    public Tween PlayShow(RectTransform panel, CanvasGroup cg, float duration)
    {
        panel.localScale = Vector3.zero;
        cg.SetCanvasState(true, 1f);
        return panel.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
    }

    public Tween PlayClose(RectTransform panel, CanvasGroup cg, float duration)
    {
        cg.SetCanvasState(false, 1f);
        return panel.DOScale(Vector3.zero, duration * 0.8f).SetEase(Ease.InBack);
    }
}