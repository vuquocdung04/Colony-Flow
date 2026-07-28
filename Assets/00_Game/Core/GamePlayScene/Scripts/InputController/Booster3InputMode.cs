using UnityEngine;

public class Booster3InputMode : InputMode
{
    public override void OnEnter(InputController controller)
    {
        base.OnEnter(controller);
        SetPicking(true);
        GameScene.EnableDarkPanel(true);
    }

    public override void OnExit()
    {
        SetPicking(false);
        GameScene.EnableDarkPanel(false);
    }

    public override void HandleRay(Ray ray)
    {
        Colony colony = Colony.Instance;
        GridTop top = colony != null ? colony.Top : null;
        if (top == null || colony.Booster == null) return;

        if (!top.TryPickCell(ray, out int x, out int y)) return;

        int index = ColonyGridIndex.From(x, y, top.gridX);
        if (!top.CanBoosterPick(index)) return;

        colony.Booster.PickColor(top.ColorAt(index));
        BoosterController.Instance.OnBoosterActionSuccess();
    }

    static void SetPicking(bool value)
    {
        GridTop top = Colony.Instance != null ? Colony.Instance.Top : null;
        if (top != null) top.SetBoosterPicking(value);
    }
}
