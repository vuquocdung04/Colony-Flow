using DG.Tweening;
using EventDispatcher;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(GraphicRaycaster))]
public abstract class BaseBox<T> : MonoBehaviour, IPopupBox where T : BaseBox<T>
{
    // ========== SINGLETON & ADDRESSABLES ==========
    public static T Instance { get; private set; }
    private static AsyncOperationHandle<GameObject> handle;
    private static bool isInstantiating;
    private bool _postedOpen;

    public static async Awaitable Setup(Transform parent, System.Action<T> callback)
    {
        string addressableKey = typeof(T).Name;
        var instance = await GetInstanceAsync(addressableKey, parent);
        callback?.Invoke(instance);
    }

    private static async Awaitable<T> GetInstanceAsync(string addressableKey, Transform parent)
    {
        if (Instance != null) return Instance;

        if (isInstantiating)
        {
            await AwaitableEx.WaitUntil(() => Instance != null || !isInstantiating);
            return Instance;
        }

        isInstantiating = true;
        handle = Addressables.InstantiateAsync(addressableKey, parent);
        GameObject obj = await handle.Task;

        if (obj == null)
        {
            Debug.LogError($"[BaseBox] Không tìm thấy key: {addressableKey}");
            isInstantiating = false;
            return null;
        }

        Instance = obj.GetComponent<T>();
        if (Instance == null)
        {
            Addressables.ReleaseInstance(obj);
            isInstantiating = false;
            return null;
        }

        Instance.ForceHide();
        Instance.Init();
        isInstantiating = false;
        return Instance;
    }

    protected abstract void Init();
    protected abstract void InitState();

    protected virtual void OnDestroy()
    {
        PopupStack.Remove(this);

        if (Instance == this)
        {
            Instance = null;
            isInstantiating = false;
        }

        if (handle.IsValid()) Addressables.ReleaseInstance(gameObject);
    }

    // ========== FIELDS ==========
    [Header("UI Animation Settings")]
    [SerializeField] protected RectTransform mainPanel;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected float durationAppeared = 0.3f;
    [SerializeField] protected BoxAnimationType animationType = BoxAnimationType.Scale;
    [SerializeField, Range(0f, 1f)] protected float backdropAlpha = 0.95f;

    private Tween currentTween;
    private IShowAnimation _activeAnim;

    float IPopupBox.BackdropAlpha => backdropAlpha;

    public System.Action OnClosed;

    // ========== SHOW / CLOSE ==========
    public void Show() => Show(BoxAnimationFactory.Get(animationType), false);
    public void Show(bool cover) => Show(BoxAnimationFactory.Get(animationType), cover);

    public void Show(IShowAnimation anim, bool cover)
    {
        ShowInternal(anim);
        PopupStack.Push(this, cover);
    }

    public void ShowRaw() => ShowInternal(BoxAnimationFactory.Get(animationType));
    public void ShowRaw(IShowAnimation anim) => ShowInternal(anim);

    private void ShowInternal(IShowAnimation anim)
    {
        _activeAnim = anim;
        InitState();
        KillCurrentTween();
        transform.SetAsLastSibling();

        SceneUtils.ExecuteInScene(SceneName.GAME_PLAY, () =>
       {
           if (!_postedOpen)
           {
               _postedOpen = true;
               this.PostEvent(EventID.POPUP_OPENED);
           }
       });
        currentTween = anim.PlayShow(mainPanel, canvasGroup, durationAppeared);
    }

    public void Close() => CloseInternal(_activeAnim ?? BoxAnimationFactory.Get(animationType), true);
    public void CloseRaw(IShowAnimation anim) => CloseInternal(anim, false);

    private void CloseInternal(IShowAnimation anim, bool notifyStack)
    {
        KillCurrentTween();
        if (_postedOpen) { _postedOpen = false; this.PostEvent(EventID.POPUP_CLOSED); }

        currentTween = anim.PlayClose(mainPanel, canvasGroup, durationAppeared);
        if (currentTween != null)
            currentTween.OnComplete(() => FinishClose(notifyStack));
        else
            FinishClose(notifyStack);
    }

    private void FinishClose(bool notifyStack)
    {
        canvasGroup.SetCanvasState(false, 0f);

        if (notifyStack)
        {
            var toResume = PopupStack.Pop(this);
            if (toResume is UnityEngine.Object o && o != null) toResume.Resume();
            PopupStack.RefreshBackdrop(PopupStack.IsEmpty);
        }

        InvokeOnClosed();
    }

    void IPopupBox.Suspend()
    {
        KillCurrentTween();
        ForceHide();
    }

    void IPopupBox.Resume()
    {
        KillCurrentTween();
        transform.SetAsLastSibling();
        var anim = _activeAnim ?? BoxAnimationFactory.Get(animationType);
        currentTween = anim.PlayShow(mainPanel, canvasGroup, durationAppeared);
    }
    private void InvokeOnClosed()
    {
        var cb = OnClosed;
        OnClosed = null;
        cb?.Invoke();
    }

    // ========== HELPERS ==========
    private void ForceHide() => canvasGroup.SetCanvasState(false, 0f);

    private void KillCurrentTween()
    {
        currentTween?.Kill();
        currentTween = null;
    }
}