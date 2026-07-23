using UnityEngine;

public abstract class SceneTransition : MonoBehaviour
{
    public abstract SceneTransitionType Type { get; }

    [SerializeField] protected CanvasGroup canvasGroup;

    public virtual void Prepare()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public abstract void SetCovered();
    public abstract Awaitable Cover(float duration);
    public abstract Awaitable Reveal(float duration);
}
