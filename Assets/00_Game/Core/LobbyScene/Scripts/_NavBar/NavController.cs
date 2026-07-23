using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class NavController : MonoBehaviour
{
    public static NavController Instance { get; private set; }
    [SerializeField] private RectTransform selector;
    [SerializeField] private float scaleSelected = 1.3f;
    [SerializeField] private float raiseY = 100f;
    public List<NavButton> navButtons;
    private Vector2 sizeButton;
    private NavButton currentNavSelected;

    public void Init()
    {
        Instance = this;

        foreach (var nav in navButtons)
        {
            nav.Init();
            nav.SetupClick(delegate
            {
                if (nav != currentNavSelected)
                {
                    UpdateNavButtonState(nav);
                }
            });
        }
        InitAfterLayoutAsync().Forget();
    }
    private async Awaitable InitAfterLayoutAsync()
    {
        await Awaitable.EndOfFrameAsync(destroyCancellationToken);
        InitNavButtonStateWith(ENavType.Lobby);
    }
    private void InitSize()
    {
        int countNavBar = navButtons.Count;
        if (countNavBar == 0) return;

        float totalWidth = GetComponent<RectTransform>().rect.width;
        float height = 250;
        float width = totalWidth / countNavBar;
        sizeButton = new Vector2(width, height);

        foreach (var nav in navButtons)
        {
            nav.SetSize(sizeButton);
        }
    }
    public void NavigateTo(ENavType type)
    {
        var target = navButtons.Find(n => n.navType == type);
        if (target == null || target == currentNavSelected) return;
        UpdateNavButtonState(target);
    }
    private void InitNavButtonStateWith(ENavType type)
    {
        InitSize();
        foreach (var t in navButtons)
        {
            bool isSelected = t.navType == type;
            if (isSelected)
            {
                currentNavSelected = t;
                MoveSelector(t, false);
            }
            t.HandleSelected(isSelected, scaleSelected, raiseY);
        }
    }
    private void UpdateNavButtonState(NavButton navButton)
    {
        foreach (var t in navButtons)
        {
            t.HandleSelected(false, scaleSelected, raiseY);
        }
        HandleScreenSliding(navButton);
        currentNavSelected = navButton;
        MoveSelector(navButton, true);
        navButton.HandleSelected(true, scaleSelected, raiseY);
    }
    private void MoveSelector(NavButton target, bool animate)
    {
        if (selector == null) return;

        float targetX = ToSelectorAnchoredX(target.RectMain);
        selector.DOKill();
        if (animate)
        {
            selector.DOAnchorPosX(targetX, 0.25f).SetEase(Ease.OutCubic);
        }
        else
        {
            selector.anchoredPosition = new Vector2(targetX, selector.anchoredPosition.y);
        }
    }
    private float ToSelectorAnchoredX(RectTransform target)
    {
        var parent = selector.parent as RectTransform;
        if (parent == null) return selector.anchoredPosition.x;

        float anchorOffset = selector.anchoredPosition.x - selector.localPosition.x;
        float localX = parent.InverseTransformPoint(target.position).x;
        return localX + anchorOffset;
    }
    private void HandleScreenSliding(NavButton clicked)
    {
        bool clickedIsRight = clicked.transform.localPosition.x > currentNavSelected.transform.localPosition.x;

        var outAnim = clickedIsRight ? BoxAnimationFactory.SlideToLeft : BoxAnimationFactory.SlideToRight;
        var inAnim = clickedIsRight ? BoxAnimationFactory.SlideFromRight : BoxAnimationFactory.SlideFromLeft;

        ClosePrevBox(currentNavSelected.navType, outAnim);
        OpenCurrentBox(clicked.navType, inAnim);
    }
    private void OpenCurrentBox(ENavType type, IShowAnimation anim)
    {
        switch (type)
        {
            case ENavType.Shop:
                ShopBox.Instance.ShowRaw(anim);
                break;
            case ENavType.Lobby:
                LobbyBox.Instance.ShowRaw(anim);
                break;
            case ENavType.Rank:
                RankBox.Instance.ShowRaw(anim);
                break;
        }
    }
    private void ClosePrevBox(ENavType type, IShowAnimation anim)
    {
        switch (type)
        {
            case ENavType.Shop:
                if (ShopBox.Instance != null) ShopBox.Instance.CloseRaw(anim);
                break;
            case ENavType.Lobby:
                if (LobbyBox.Instance != null) LobbyBox.Instance.CloseRaw(anim);
                break;
            case ENavType.Rank:
                if (RankBox.Instance != null) RankBox.Instance.CloseRaw(anim);
                break;
        }
    }

    [ContextMenu("Setup Nav button")]
    private void Setup()
    {
        navButtons.Clear();
        navButtons = GetComponentsInChildren<NavButton>().ToList();

        foreach (var t in navButtons)
        {
            t.InitSetup();
        }
    }
}
