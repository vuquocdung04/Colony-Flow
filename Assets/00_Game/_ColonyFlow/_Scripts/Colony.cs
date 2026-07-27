using EventDispatcher;
using UnityEngine;

public class Colony : InitSingleton<Colony>
{
    [SerializeField] private GridController gridController;
    [SerializeField] private WaitAreas waitAreas;

    private string _pickedColor;

    public GridController Grid => gridController;
    public WaitAreas Waits => waitAreas;

    public GridTop Top => gridController != null ? gridController.gridTop : null;
    public GridBottom Bottom => gridController != null ? gridController.gridBottom : null;

    public override void Init()
    {

        if (gridController == null)
        {
            Debug.LogWarning("[Colony] Chưa gán GridController.", this);
            return;
        }

        gridController.Spawn(waitAreas);
        this.RegisterListener(EventID.BOOSTER_ACTION, OnBoosterAction);
        this.RegisterListener(EventID.BOOSTER_TARGET_REQUEST, OnBoosterTargetRequest);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BOOSTER_ACTION, OnBoosterAction);
        this.RemoveListener(EventID.BOOSTER_TARGET_REQUEST, OnBoosterTargetRequest);
    }

    private void OnBoosterAction(object param)
    {
        switch ((BoosterType)param)
        {
            case BoosterType.Booster0:
                AddWaitSlot();
                break;

            case BoosterType.Booster1:
                SwapRow0();
                break;

            case BoosterType.Booster3:
                ClearColor();
                break;
        }
    }

    private void OnBoosterTargetRequest(object param)
    {
        BoosterType type = (BoosterType)param;

        switch (type)
        {
            case BoosterType.Booster2:
                BeginPickAnthill(type);
                break;

            case BoosterType.Booster3:
                InputController.Instance.SetBooster3Mode();
                break;
        }
    }

    private void BeginPickAnthill(BoosterType type)
    {
        if (Bottom == null || Bottom.CountBoosterTargets() == 0)
        {
            ToastManager.Instance.ShowToast("No anthill can be moved!");
            this.PostEvent(EventID.BOOSTER_DEACTIVATE_REQUEST, type);
            return;
        }

        InputController.Instance.SetBooster2Mode();
    }

    private void AddWaitSlot()
    {
        if (waitAreas != null) waitAreas.AddSlot();
    }

    private void SwapRow0()
    {
        if (Bottom != null) Bottom.SwapRow0(Top);
    }

    public void PickColor(string hex) => _pickedColor = hex;

    private void ClearColor()
    {
        string hex = _pickedColor;
        _pickedColor = null;
        if (string.IsNullOrEmpty(hex)) return;

        if (Top != null) Top.ClearColor(hex);
        if (Bottom != null) Bottom.ClearColor(hex);
        if (waitAreas != null) waitAreas.ClearColor(hex);

        Ant.ClearColor(hex);
    }
}
