
using EventDispatcher;
using UnityEngine;

public class GamePlayController : Singleton<GamePlayController>
{
    public Camera cameraUI;
    public Camera cameraGameplay;
    public GameScene gameScene;
    public Colony colony;
    public BoosterController boosterController;
    public HandAnimation handAnimation;
    public GameFlow gameFlow;
    public InputController inputController;

    protected override void OnAwake()
    {
        base.OnAwake();
        InitInstances();
        Init().Forget();
    }

    // Bind toàn bộ Instance trước, để các Init() bên dưới truy cập chéo lẫn nhau được.
    private void InitInstances()
    {
        gameScene.InitInstance();
        colony.InitInstance();
        handAnimation.InitInstance();
        boosterController.InitInstance();
        inputController.InitInstance();
        gameFlow.InitInstance();
    }

    private async Awaitable Init()
    {
        gameScene.Init();
        colony.Init();
        handAnimation.Init();
        boosterController.Init();
        inputController.Init();
        gameFlow.Init();
        gameFlow.RequestPause();

        if (AudioManager.Instance)
            AudioManager.Instance.PlayMusic("Normal Level Music (Cover) 1");

        await Awaitable.EndOfFrameAsync(destroyCancellationToken);
        await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
        if (FXManager.Instance)
            FXManager.Instance.isNextSceneReady = true;
        await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
        gameFlow.RequestResume();
    }
}
