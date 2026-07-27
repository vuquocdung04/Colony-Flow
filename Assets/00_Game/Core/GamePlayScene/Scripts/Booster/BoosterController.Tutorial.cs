using UnityEngine;

public partial class BoosterController
{
    public int GetCurrentTutorialBoosterIndex()
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null) continue;
            var cfg = GetConfig(items[i].Type);
            if (cfg != null && cfg.levelUnlock == CurrentLevel) return i;
        }
        return -1;
    }

    private bool IsTutorialActive(BoosterType type)
    {
        var cfg = GetConfig(type);
        return cfg != null && CurrentLevel == cfg.levelUnlock && !IsTutorialDone(type);
    }

    public void CheckTutorialHighlight()
    {
        BoosterItem target = null;

        foreach (var item in items)
        {
            if (item == null || !IsTutorialActive(item.Type)) continue;
            target = item;
            break;
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

        if (!NeedsTargetSelection(type)) SetTutorialDone(type, true);
    }

    private void CompletePhase2Tutorial(BoosterType type)
    {
        if (IsTutorialDone(type)) return;

        SetTutorialDone(type, true);
        HandAnimation.Instance.KillObj();
    }

    private void HandleTutorialCancel(BoosterType type)
    {
        if (!IsTutorialActive(type)) return;

        HandAnimation.Instance.KillObj();
        SetTutorialDone(type, true);
    }
}
