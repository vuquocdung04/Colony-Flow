using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopBox : BaseBox<ShopBox>
{
    public Button btnClose;


    protected override void Init()
    {
        btnClose.OnClicked(Close);
        SceneUtils.ExecuteInScene(SceneName.LOBBY_SCENE, delegate
        {
            btnClose.SetActive(false);
        });

    }

    protected override void InitState()
    {

    }
}