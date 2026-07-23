using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransition : SceneTransition
{
    public override SceneTransitionType Type => SceneTransitionType.Fade;

    [SerializeField] private Graphic overlay;

    public override void SetCovered() => SetAlpha(1f);

    public override async Awaitable Cover(float duration)
    {
        SetAlpha(0f);
        await overlay.DOFade(1f, duration).ToAwaitable();
    }

    public override async Awaitable Reveal(float duration)
    {
        SetAlpha(1f);
        await overlay.DOFade(0f, duration).ToAwaitable();
    }

    private void SetAlpha(float a)
    {
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
