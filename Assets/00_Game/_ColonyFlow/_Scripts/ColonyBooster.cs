using EventDispatcher;
using UnityEngine;

public class ColonyBooster : MonoBehaviour
{
    private string _pickedColor;

    private void OnEnable()
    {
        this.RegisterListener(EventID.BOOSTER_ACTION, OnAction);
        this.RegisterListener(EventID.BOOSTER_TARGET_REQUEST, OnTargetRequest);
    }

    private void OnDisable()
    {
        this.RemoveListener(EventID.BOOSTER_ACTION, OnAction);
        this.RemoveListener(EventID.BOOSTER_TARGET_REQUEST, OnTargetRequest);
    }

    public void PickColor(string hex) => _pickedColor = hex;

    private void OnAction(object param)
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

    private void OnTargetRequest(object param)
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

    private void AddWaitSlot()
    {
        WaitAreas waits = Colony.Instance != null ? Colony.Instance.Waits : null;
        if (waits != null) waits.AddSlot();
    }

    private void SwapRow0()
    {
        Colony colony = Colony.Instance;
        if (colony != null && colony.Bottom != null) colony.Bottom.SwapRow0(colony.Top);
    }

    private void BeginPickAnthill(BoosterType type)
    {
        GridBottom bottom = Colony.Instance != null ? Colony.Instance.Bottom : null;

        if (bottom == null || bottom.CountBoosterTargets() == 0)
        {
            ToastManager.Instance.ShowToast("No anthill can be moved!");
            this.PostEvent(EventID.BOOSTER_DEACTIVATE_REQUEST, type);
            return;
        }

        InputController.Instance.SetBooster2Mode();
    }

    private void ClearColor()
    {
        string hex = _pickedColor;
        _pickedColor = null;

        Colony colony = Colony.Instance;
        if (string.IsNullOrEmpty(hex) || colony == null) return;

        float duration = BoosterFxPanel.Instance != null
            ? BoosterFxPanel.Instance.GetActionDuration(BoosterType.Booster3)
            : 0f;

        if (colony.Top != null) colony.Top.ClearColor(hex, duration);
        if (colony.Bottom != null) colony.Bottom.ClearColor(hex);
        if (colony.Waits != null) colony.Waits.ClearColor(hex);

        Ant.ClearColor(hex);
    }
}
