using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class IrisTransition : SceneTransition
{
    public override SceneTransitionType Type => SceneTransitionType.Iris;

    [SerializeField] private RawImage rawImage;
    [SerializeField] private float coveredRadius = 0f;
    [SerializeField] private float openRadius = 1.2f;

    private Material mat;
    private Material Mat => mat != null ? mat : mat = rawImage.material;

    public override void SetCovered()
    {
        Mat.SetFloat("_IsInvert", 1f);
        Mat.SetFloat("_Radius", coveredRadius);
    }

    public override async Awaitable Cover(float duration)
    {
        Mat.SetFloat("_IsInvert", 0f);
        Mat.SetFloat("_Radius", coveredRadius);
        await Mat.DOFloat(openRadius, "_Radius", duration).ToAwaitable();
    }

    public override async Awaitable Reveal(float duration)
    {
        Mat.SetFloat("_IsInvert", 1f);
        Mat.SetFloat("_Radius", coveredRadius);
        await Mat.DOFloat(openRadius, "_Radius", duration).ToAwaitable();
    }
}
