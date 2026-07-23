using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PopupBackdrop : MonoBehaviour, IPopupBackdrop
{
    [SerializeField] private Image image;
    [SerializeField] private float fadeDuration = 0.2f;

    private Tween _tween;

    private void Reset() => image = GetComponent<Image>();

    private void OnEnable() => PopupStack.RegisterBackdrop(this);

    private void OnDisable() => PopupStack.UnregisterBackdrop(this);

    public void ShowBackdrop(float alpha, bool fade)
    {
        image.raycastTarget = true;
        SetAlpha(alpha, fade);
    }

    public void HideBackdrop(bool fade)
    {
        image.raycastTarget = false;
        SetAlpha(0f, fade);
    }

    private void SetAlpha(float target, bool fade)
    {
        _tween?.Kill();

        if (fade && fadeDuration > 0f)
        {
            _tween = image.DOFade(target, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        else
        {
            var c = image.color;
            c.a = target;
            image.color = c;
        }
    }
}
