using System.Collections.Generic;
using UnityEngine;

public partial class GridBottom
{
    public bool CanSwapRow0() => SwapPool().Count >= 2;

    public bool SwapRow0(GridTop gridTop)
    {
        List<int> pool = SwapPool();
        if (pool.Count < 2) return false;

        List<Anthill> items = new List<Anthill>(pool.Count);
        foreach (int index in pool) items.Add(_slots[index]);

        Shuffle(items);
        ForceReachableInRow0(gridTop, pool, items);

        Vector3 cell = CellSize;

        for (int i = 0; i < pool.Count; i++)
        {
            int index = pool[i];
            Anthill item = items[i];
            int row = ColonyGridIndex.Y(index, gridX);

            _slots[index] = item;
            item.SetIndex(index);
            item.MoveTo(SlotCenter(index, cell), true);
            item.SetRow(row, true);

            if (row == 0) item.ReachRow0();
        }

        return true;
    }

    List<int> SwapPool()
    {
        List<int> pool = new List<int>();
        if (_slots == null) return pool;

        for (int index = 0; index < _slots.Length; index++)
        {
            Anthill item = _slots[index];
            if (item == null || item.IsTaken || item.IsLocked) continue;
            if (item.Link != null && item.Link.group != null) continue;

            pool.Add(index);
        }

        return pool;
    }

    void ForceReachableInRow0(GridTop gridTop, List<int> pool, List<Anthill> items)
    {
        if (gridTop == null) return;

        List<int> row0 = new List<int>();

        for (int i = 0; i < pool.Count; i++)
        {
            if (ColonyGridIndex.Y(pool[i], gridX) != 0) continue;
            if (gridTop.HasReachableColor(items[i].ColorHex)) return;

            row0.Add(i);
        }

        if (row0.Count == 0) return;

        for (int i = 0; i < items.Count; i++)
        {
            if (!gridTop.HasReachableColor(items[i].ColorHex)) continue;

            int target = row0[Random.Range(0, row0.Count)];
            Anthill temp = items[target];
            items[target] = items[i];
            items[i] = temp;
            return;
        }
    }

    static void Shuffle(List<Anthill> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Anthill temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }
}
