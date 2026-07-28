public partial class GridBottom
{
    public int CountBoosterTargets()
    {
        if (_slots == null) return 0;

        int count = 0;
        foreach (Anthill item in _slots)
            if (item != null && item.CanBoosterSelect()) count++;

        return count;
    }

    public void SetBoosterPicking(bool value)
    {
        if (_slots == null) return;

        foreach (Anthill item in _slots)
        {
            if (item == null) continue;

            bool selectable = item.CanBoosterSelect();
            item.SetPickable(!value || selectable);
            HighlightObject.Set(item.gameObject, value && selectable);
        }
    }
}
