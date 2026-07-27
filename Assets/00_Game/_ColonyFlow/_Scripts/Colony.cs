using EventDispatcher;
using UnityEngine;

public class Colony : InitSingleton<Colony>
{
    [SerializeField] private GridController gridController;
    [SerializeField] private WaitAreas waitAreas;

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
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BOOSTER_ACTION, OnBoosterAction);
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
        }
    }

    private void AddWaitSlot()
    {
        if (waitAreas != null) waitAreas.AddSlot();
    }

    private void SwapRow0()
    {
        if (Bottom != null) Bottom.SwapRow0(Top);
    }
}
