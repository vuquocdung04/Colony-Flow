using System;
using DG.Tweening;
using UnityEngine;

public class CookieBlocker : BlockerBase
{
    [Header("Empty - bay lên rồi co về 0")]
    public float riseY = 1f;
    public float riseDuration = 0.25f;

    [Space]
    public float popScale = 1.25f;
    [Tooltip("0 = phình cùng lúc bay lên. Bằng riseDuration = bay lên xong mới phình.")]
    public float popDelay = 0f;
    public float popDuration = 0.25f;

    [Space]
    public float holdDuration = 0.1f;
    public float shrinkDuration = 0.12f;

    Tween _empty;

    protected override bool Accepts(object param) => true;

    protected override void PlayEmpty(Action onComplete)
    {
        _empty?.Kill();

        Transform root = transform;
        Vector3 scale = root.localScale;

        _empty = DOTween.Sequence()
                        .Append(root.DOLocalMoveY(root.localPosition.y + riseY, riseDuration)
                                    .SetEase(Ease.OutQuad))
                        .Insert(popDelay, root.DOScale(scale * popScale, popDuration)
                                              .SetEase(Ease.OutBack))
                        .AppendInterval(holdDuration)
                        .Append(root.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack))
                        .OnComplete(() => onComplete?.Invoke())
                        .SetLink(gameObject);
    }
}
