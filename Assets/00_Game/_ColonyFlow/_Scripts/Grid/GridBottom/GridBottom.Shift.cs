using System;
using UnityEngine;

public partial class GridBottom
{
    public void Release(Anthill item)
    {
        if (_slots == null || item == null) return;

        int index = Array.IndexOf(_slots, item);
        if (index < 0) return;

        _slots[index] = null;
        ShiftColumn(ColonyGridIndex.X(index, gridX));
    }

    public void RefreshRows(bool instant = false)
    {
        if (_slots == null) return;

        for (int x = 0; x < gridX; x++) ShiftColumn(x, instant);
    }

    void ShiftColumn(int x, bool instant = false)
    {
        if (_slots == null) return;

        Vector3 cell = CellSize;
        int row = 0;

        for (int y = 0; y < gridY; y++)
        {
            int from = ColonyGridIndex.From(x, y, gridX);
            Anthill item = _slots[from];
            if (item == null) continue;

            if (y != row)
            {
                int to = ColonyGridIndex.From(x, row, gridX);
                _slots[to] = item;
                _slots[from] = null;
                item.MoveTo(SlotCenter(to, cell), instant);
            }

            item.SetRow(row, instant);
            row++;
        }
    }
}
