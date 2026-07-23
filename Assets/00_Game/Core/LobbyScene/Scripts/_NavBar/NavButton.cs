using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public class NavButton : MonoBehaviour
{
    public ENavType navType;
    [Space(5)] [SerializeField] private Button btnMain;

    [SerializeField] RectTransform icon;
    [SerializeField] private RectTransform rectMain;

    public RectTransform RectMain => rectMain;

    public void Init()
    {
        UIUtils.FitToTargetHeight(icon.GetComponent<Image>(), 200f);
    }

    public void SetSize(Vector2 size)
    {
        rectMain.sizeDelta = size;
    }

    public void HandleSelected(bool isSelected, float scaleSelected, float raiseY)
    {
        if (isSelected)
        {
            icon.DOScale(Vector3.one * scaleSelected, 0.15f);
            icon.DOAnchorPosY(raiseY, 0.15f);
        }
        else
        {
            icon.DOScale(Vector3.one, 0.15f);
            icon.DOAnchorPosY(0f, 0.15f);
        }
    }

    public void SetupClick(System.Action callback)
    {
        btnMain.OnClicked(() => callback?.Invoke());
    }


    public void InitSetup()
    {
        rectMain = GetComponent<RectTransform>();
        btnMain = GetComponent<Button>();
        icon = transform.GetComponentInChildren<RectTransform>();
    }
}

public enum ENavType
{
    Shop = 0,
    Lobby = 1,
    Rank = 2,
}
