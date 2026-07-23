using UnityEngine;

public partial class BoosterController
{
    public readonly int[] TutorialLevels = { 3, 6, 9 };

    public int GetCurrentTutorialBoosterIndex()
        => System.Array.IndexOf(TutorialLevels, CurrentLevel);

    public void CheckTutorialHighlight()
    {
        BoosterItem target = null;

        foreach (var item in items)
        {
            var cfg = GetConfig(item.Type);
            if (cfg == null) continue;
            if (CurrentLevel == cfg.levelUnlock && !IsTutorialDone(item.Type))
            {
                target = item;
                break;
            }
        }

        if (target != null)
        {
            BoosterUnlockBox.Setup(GameScene.GetPopupHolder(), box => box.Show()).Forget();
        }
    }

    private void CheckAndClearTutorialPhase1(BoosterType type, BoosterItem item)
    {
        if (IsTutorialDone(type)) return;

        HandAnimation.Instance.RemoveHighlightUI(item.gameObject);
        HandAnimation.Instance.KillUI();

        if (type == BoosterType.Booster0) SetTutorialDone(type, true);
    }

    private void SetupPhase2Tutorial(BoosterType type)
    {
        if (type == BoosterType.Booster1 && CurrentLevel == 6
            && !IsTutorialDone(BoosterType.Booster1))
        {
            // Transform targetObj = GamePlayController.Instance.gameScene.GetTutorialTarget();
            // if (targetObj != null) HandAnimation.Instance.PlayAnimObj(targetObj);
        }
    }

    private void CompletePhase2Tutorial(BoosterType type)
    {
        if (type == BoosterType.Booster1 && !IsTutorialDone(BoosterType.Booster1))
        {
            SetTutorialDone(BoosterType.Booster1, true);
            HandAnimation.Instance.KillObj();
        }
    }

    private void HandleTutorialCancel(BoosterType type)
    {
        if (type == BoosterType.Booster1 && CurrentLevel == 6 && !IsTutorialDone(BoosterType.Booster1))
        {
            HandAnimation.Instance.KillObj();
            SetTutorialDone(BoosterType.Booster1, true);
        }

        if (type == BoosterType.Booster2 && CurrentLevel == 9 && !IsTutorialDone(BoosterType.Booster2))
        {
            HandAnimation.Instance.KillObj();
            SetTutorialDone(BoosterType.Booster2, true);
        }
    }
}
