using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HandAnimation : InitSingleton<HandAnimation>
{
    public Camera mainCam;
    public Transform handObj;
    public Transform handUI;
    public float duration = 0.3f;
    public float scaleMultiplier = 1.2f;
    private Tween uiTween;
    private Tween objTween;
    private Vector3 defaultScale;
    public override void Init()
    {
        handObj.gameObject.SetActive(false);
        handUI.gameObject.SetActive(false);

        handObj.localRotation = mainCam.transform.localRotation;
        defaultScale = handUI.localScale;
    }
    public void PlayAnimUI(Transform target)
    {
        PlayHandAnim(handUI, target, ref uiTween);
    }

    public void PlayAnimObj(Transform target)
    {
        PlayHandAnim(handObj, target, ref objTween);
    }


    // For game view top-down(XZ)
    private void PlayHandAnim(Transform hand, Transform target, ref Tween tween)
    {
        StopHandAnim(hand, ref tween);

        hand.position = target.position;
        hand.localScale = defaultScale;
        hand.gameObject.SetActive(true);

        Vector3 targetScale = defaultScale * scaleMultiplier;

        tween = hand.DOScale(targetScale, duration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
    }
    private void StopHandAnim(Transform hand, ref Tween tween)
    {
        if (tween != null)
        {
            tween.Kill();
            tween = null;
        }

        if (hand != null)
        {
            hand.localScale = defaultScale;
            hand.gameObject.SetActive(false);
        }
    }

    public void KillUI() => StopHandAnim(handUI, ref uiTween);
    public void KillObj() => StopHandAnim(handObj, ref objTween);



    public void HighlightUI(GameObject targetUI)
    {
        GameScene.EnableDarkPanel(true);
        var targetCanvas = targetUI.AddComponent<Canvas>();
        targetCanvas.overrideSorting = true;
        targetCanvas.sortingLayerName = "UI";
        targetCanvas.sortingOrder = 1;

        targetUI.AddComponent<GraphicRaycaster>();
    }
    public void RemoveHighlightUI(GameObject targetUI)
    {
        GameScene.EnableDarkPanel(false);
        GraphicRaycaster raycaster = targetUI.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }

        Canvas targetCanvas = targetUI.GetComponent<Canvas>();
        if (targetCanvas != null)
        {
            Destroy(targetCanvas);
        }
    }
}
