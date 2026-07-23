using UnityEngine;
public class LobbyController : Singleton<LobbyController>
{
    public LobbyScene lobbyScene;
    [Header("UI Layers")]
    public Transform botCanvas;
    public Transform topCanvas;
    
    protected override void OnAwake()
    {
        base.OnAwake();
        Init();
    }

    private void Init()
    {
        lobbyScene.InitAsync().Forget();
        AudioManager.Instance.PlayMusic("Main Menu Music (Cover) 2");

    }
}
