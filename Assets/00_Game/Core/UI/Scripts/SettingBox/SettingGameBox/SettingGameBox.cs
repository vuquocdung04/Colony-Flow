using UnityEngine.UI;

public class SettingGameBox : SettingBaseBox<SettingGameBox>
{
    public Button btnReturnHome;
    public Button btnRestart;
    public Button btnCheat;

    protected override void OnInit()
    {
        btnReturnHome.OnClicked(() =>
            QuitLevelBox.Setup(transform.parent, box => box.SetupAndShow(QuitLevelBox.Mode.Leave, true)).Forget());

        btnRestart.OnClicked(() =>
            QuitLevelBox.Setup(transform.parent, box => box.SetupAndShow(QuitLevelBox.Mode.Restart, true)).Forget());

        btnCheat.OnClicked(() =>
            CheatBox.Setup(transform.parent, box => box.Show()).Forget());
    }
}
