using System.Collections.Generic;

public partial class BoosterController
{
    private readonly HashSet<BoosterType> _used = new HashSet<BoosterType>();

    public bool IsUsed(BoosterType type) => _used.Contains(type);

    private bool CanUse(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Booster0:
                return !IsUsed(type);
        }
        return true;
    }

    private void MarkUsed(BoosterType type)
    {
        _used.Add(type);
        RefreshUsable();
    }

    public void RefreshUsable()
    {
        foreach (var item in items)
        {
            if (item == null) continue;
            item.SetUsable(CanUse(item.Type));
        }
    }
}
