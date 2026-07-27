using UnityEngine;

public class Booster3InputMode : InputMode
{
    public override void HandleRay(Ray ray)
    {
        Colony colony = Colony.Instance;
        GridTop top = colony != null ? colony.Top : null;
        if (top == null) return;

        if (!top.TryPickCell(ray, out int x, out int y)) return;

        int index = ColonyGridIndex.From(x, y, top.gridX);
        if (top.IsBlocked(index)) return;

        string hex = top.ColorAt(index);
        if (string.IsNullOrEmpty(hex)) return;

        colony.PickColor(hex);
        BoosterController.Instance.OnBoosterActionSuccess();
    }
}
