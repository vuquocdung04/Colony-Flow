using EventDispatcher;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : InitSingleton<InputController>
{
    private Camera cam;
    private InputMode _currentMode;
    private InputMode _normalMode;
    private InputMode _booster2Mode;
    private InputMode _booster3Mode;
    private InputMode _disabledMode;

    public override void Init()
    {
        cam = GamePlayController.Instance.cameraGameplay;

        _normalMode = new NormalInputMode();
        _booster2Mode = new Booster2InputMode();
        _booster3Mode = new Booster3InputMode();
        _disabledMode = new DisabledInputMode();

        SetMode(_normalMode);

        this.RegisterListener(EventID.INPUT_MODE_REQUEST, OnModeRequest);
        GameFlow.Instance.OnStateEntered += OnGameStateChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        this.RemoveListener(EventID.INPUT_MODE_REQUEST, OnModeRequest);
        GameFlow.Instance.OnStateEntered -= OnGameStateChanged;
    }

    private void OnModeRequest(object param)
    {
        switch ((InputModeId)param)
        {
            case InputModeId.Normal: SetMode(_normalMode); break;
            case InputModeId.Booster2: SetMode(_booster2Mode); break;
            case InputModeId.Booster3: SetMode(_booster3Mode); break;
            case InputModeId.Disabled: SetMode(_disabledMode); break;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        bool shouldDisable = newState == GameState.Win ||
                            newState == GameState.Lose ||
                            newState == GameState.Paused ||
                            newState == GameState.Tutorial;

        if (shouldDisable)
            SetMode(_disabledMode);
        else
            SetMode(_normalMode);
    }

    private void SetMode(InputMode newMode)
    {
        _currentMode?.OnExit();
        _currentMode = newMode;
        _currentMode?.OnEnter(this);
    }

    private void Update()
    {
        if (Pointer.current == null) return;
        if (!Pointer.current.press.wasPressedThisFrame) return;

        HandleClick();
    }

    private void HandleClick()
    {
        Vector2 screenPos = Pointer.current.position.ReadValue();
        _currentMode.HandleRay(cam.ScreenPointToRay(screenPos));
    }
}