public partial class BoosterController
{
    private void ApplyInstantEffect(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Booster0:
                AddWaitSlot();
                break;
        }
    }

    private void AddWaitSlot()
    {
        Colony colony = Colony.Instance;

        if (colony == null || !colony.AddWaitSlot())
        {
            ToastManager.Instance.ShowToast("Can't add a slot right now!");
            Deactivate();
            return;
        }

        OnBoosterActionSuccess();
    }
}
