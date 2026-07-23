using UnityEngine.UI;

public class SettingLobbyBox : SettingBaseBox<SettingLobbyBox>
{
    public Button btnCloseByPanel;

    protected override void OnInit()
    {
        btnCloseByPanel.OnClicked(Close);
    }
}
