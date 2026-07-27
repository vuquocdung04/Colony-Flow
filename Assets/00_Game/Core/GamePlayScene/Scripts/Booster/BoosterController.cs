using EventDispatcher;
using UnityEngine;

public partial class BoosterController : InitSingleton<BoosterController>
{
    private BoosterItem _active;

    public bool HasActive => _active != null;
    public BoosterType? ActiveType => _active?.Type;

    public override void Init()
    {
        SeedData();
        ApplyConfigToItems();
        RefreshUsable();

        this.RegisterListener(EventID.BOOSTER_USE_REQUEST, OnUseRequest);
        this.RegisterListener(EventID.BOOSTER_DEACTIVATE_REQUEST, OnDeactivateRequest);
        this.RegisterListener(EventID.BOOSTER_BUY_REQUEST, OnBuyRequest);
        GameFlow.Instance.OnStateEntered += OnGameStateChanged;

        //CheckTutorialHighlight();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.BOOSTER_USE_REQUEST, OnUseRequest);
        this.RemoveListener(EventID.BOOSTER_DEACTIVATE_REQUEST, OnDeactivateRequest);
        this.RemoveListener(EventID.BOOSTER_BUY_REQUEST, OnBuyRequest);
        if (GameFlow.Instance != null)
            GameFlow.Instance.OnStateEntered -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState != GameState.Win && newState != GameState.Lose) return;
        if (_active == null) return;
        ForceCancelActiveBooster();
    }

    private void ForceCancelActiveBooster()
    {
        _active.ChangeState(BoosterState.Available);
        _active.SetData(GetQuantity(_active.Type));
        _active = null;

        InputController.Instance.RestoreNormalMode();
    }

    private void OnBuyRequest(object param)
    {
        var type = (BoosterType)param;
        BuyBoosterBox.Setup(GameScene.GetPopupHolder(), box => box.SetupAndShow(type)).Forget();
    }

    private void OnUseRequest(object param)
    {
        var type = (BoosterType)param;
        var item = FindItem(type);
        if (item == null) return;

        CheckAndClearTutorialPhase1(type, item);

        if (_active != null)
        {
            ToastManager.Instance.ShowToast("Another Booster is in use!");
            return;
        }

        if (!CanUse(type))
        {
            ToastManager.Instance.ShowToast("This Booster can't be used now!");
            return;
        }

        _active = item;
        item.ChangeState(BoosterState.InUse);

        if (NeedsTargetSelection(type))
        {
            this.PostEvent(EventID.BOOSTER_TARGET_REQUEST, type);
            return;
        }

        OnBoosterActionSuccess();
    }

    public static bool NeedsTargetSelection(BoosterType type)
        => type == BoosterType.Booster2 || type == BoosterType.Booster3;

    private void OnDeactivateRequest(object param)
    {
        if (_active == null || _active.Type != (BoosterType)param) return;
        Deactivate();
    }

    public void Deactivate()
    {
        if (_active == null) return;
        HandleTutorialCancel(_active.Type);
        _active.ChangeState(BoosterState.Available);
        _active.SetData(GetQuantity(_active.Type));
        _active = null;
        InputController.Instance.RestoreNormalMode();
    }

    public void OnBoosterActionSuccess()
    {
        if (_active == null) return;

        BoosterType type = _active.Type;
        CompletePhase2Tutorial(type);

        Consume(type);
        Deactivate();
        MarkUsed(type);

        this.PostEvent(EventID.BOOSTER_USED, type);
    }
}
