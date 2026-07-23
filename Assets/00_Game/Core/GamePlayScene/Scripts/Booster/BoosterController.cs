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

        if (!CanUseBooster(type)) return;

        _active = item;
        item.ChangeState(BoosterState.InUse);
    }

    private bool CanUseBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Booster0:
                return true;

            case BoosterType.Booster1:
                return true;

            case BoosterType.Booster2:
                return true;
        }
        return true;
    }

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
        CompletePhase2Tutorial(_active.Type);

        Consume(_active.Type);
        Deactivate();
    }
}
