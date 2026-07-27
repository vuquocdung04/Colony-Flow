using UnityEngine;

public class Booster2InputMode : InputMode
{
    public override void OnEnter(InputController controller)
    {
        base.OnEnter(controller);
        SetPicking(true);
    }

    public override void OnExit() => SetPicking(false);

    public override void HandleClick(RaycastHit hit)
    {
        if (hit.collider == null) return;

        Anthill anthill = hit.collider.GetComponentInParent<Anthill>();
        if (anthill == null || !anthill.BoosterSelect()) return;

        BoosterController.Instance.OnBoosterActionSuccess();
    }

    static void SetPicking(bool value)
    {
        GridBottom bottom = Colony.Instance != null ? Colony.Instance.Bottom : null;
        if (bottom != null) bottom.SetBoosterPicking(value);
    }
}
