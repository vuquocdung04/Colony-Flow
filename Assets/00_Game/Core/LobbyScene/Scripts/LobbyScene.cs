using UnityEngine;
using UnityEngine.UI;

public class LobbyScene : MonoBehaviour
{
    public NavController navController;
    public Button btnHeart;

    public Button btnCoin;
    public async Awaitable InitAsync()
    {
        navController.Init();

        await PreLoad();

        btnHeart.OnClicked(delegate
        {
            HeartManager.Instance.TryShowHeartOffer(LobbyController.Instance.topCanvas);
        });
        btnCoin.OnClicked(delegate
        {
            navController.NavigateTo(ENavType.Shop);
        });
    }

    private static async Awaitable PreLoad()
    {
        var lobbyTcs = new AwaitableCompletionSource();
        var shopTcs = new AwaitableCompletionSource();
        var rankTcs = new AwaitableCompletionSource();
        var holder = LobbyController.Instance.botCanvas;
        LobbyBox.Setup(holder, box =>
        {
            box.ShowRaw();
            lobbyTcs.TrySetResult();
        }).Forget();

        ShopBox.Setup(holder, _ => shopTcs.TrySetResult()).Forget();

        RankBox.Setup(holder, _ => rankTcs.TrySetResult()).Forget();

        await AwaitableEx.WhenAll(lobbyTcs.Awaitable, shopTcs.Awaitable, rankTcs.Awaitable);

        FXManager.Instance.isNextSceneReady = true;
    }

    public void NavigateTo(ENavType type) => navController.NavigateTo(type);
}