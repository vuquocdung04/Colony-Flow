using UnityEngine;

public partial class GridTop
{
    public bool HasBlock(int x, int y)
    {
        if (!InBounds(x, y)) return false;

        int index = ColonyGridIndex.From(x, y, gridX);
        if (!string.IsNullOrEmpty(_colors[index])) return true;
        return _keyColor != null && !string.IsNullOrEmpty(_keyColor[index]);
    }

    public bool IsReachable(int x, int y) =>
        HasBlock(x, y) && ChooseApproach(x, y, out _);

    void RebuildOpen()
    {
        int count = gridX * gridY;
        if (_open == null || _open.Length != count)
        {
            _open = new bool[count];
            _cameFrom = new int[count];
            _dist = new int[count];
        }
        else System.Array.Clear(_open, 0, count);

        _bfs.Clear();

        for (int x = 0; x < gridX; x++)
        {
            SeedIfEmpty(x, 0);
            SeedIfEmpty(x, gridY - 1);
        }
        for (int y = 0; y < gridY; y++)
        {
            SeedIfEmpty(0, y);
            SeedIfEmpty(gridX - 1, y);
        }

        Drain();
        RevealExposed();
        UnlockExposedKeys();
    }

    void OpenCell(int x, int y)
    {
        if (_open == null || !InBounds(x, y)) return;
        if (!TrySeed(x, y)) return;

        Drain();
        RevealExposed();
        UnlockExposedKeys();
    }

    void RevealExposed()
    {
        if (_hiddenRemaining <= 0 || _hidden == null) return;

        for (int index = 0; index < _hidden.Length; index++)
        {
            if (!_hidden[index]) continue;

            int x = ColonyGridIndex.X(index, gridX);
            int y = ColonyGridIndex.Y(index, gridX);
            if (!IsReachable(x, y)) continue;

            _hidden[index] = false;
            _hiddenRemaining--;
            if (_cells != null && _cells[index] != null) _cells[index].Reveal();
        }
    }

    void UnlockExposedKeys()
    {
        if (_keyLockedRemaining <= 0 || _keyColor == null) return;

        for (int index = 0; index < _keyColor.Length; index++)
        {
            if (string.IsNullOrEmpty(_keyColor[index])) continue;

            FoodKey key = _keyObjects[index];
            if (key == null || !key.IsLocked) continue;

            int x = ColonyGridIndex.X(index, gridX);
            int y = ColonyGridIndex.Y(index, gridX);
            if (!IsReachable(x, y)) continue;

            key.Unlock();
            _keyLockedRemaining--;
            FulfillPending(_keyColor[index], index);
        }
    }

    void FulfillPending(string colorHex, int keyIndex)
    {
        if (_lockRequests.Count == 0 || string.IsNullOrEmpty(_keyColor[keyIndex])) return;

        for (int r = 0; r < _lockRequests.Count; r++)
        {
            LockRequest request = _lockRequests[r];
            if (!SameColor(request.color, colorHex)) continue;

            _lockRequests.RemoveAt(r);
            ConsumeKey(keyIndex, request.target, request.onDone);
            return;
        }
    }

    void Drain()
    {
        while (_bfs.Count > 0)
        {
            int current = _bfs.Dequeue();
            int cx = ColonyGridIndex.X(current, gridX);
            int cy = ColonyGridIndex.Y(current, gridX);

            Relax(cx, cy + 1, current);
            Relax(cx + 1, cy, current);
            Relax(cx - 1, cy, current);
            Relax(cx, cy - 1, current);
        }
    }

    bool TrySeed(int x, int y)
    {
        int index = ColonyGridIndex.From(x, y, gridX);
        if (_open[index] || HasBlock(x, y)) return false;

        int best = x == 0 || y == 0 || x == gridX - 1 || y == gridY - 1 ? 0 : -1;
        int from = -1;

        TryParent(x, y + 1, ref best, ref from);
        TryParent(x + 1, y, ref best, ref from);
        TryParent(x - 1, y, ref best, ref from);
        TryParent(x, y - 1, ref best, ref from);

        if (best < 0) return false;

        _open[index] = true;
        _dist[index] = best;
        _cameFrom[index] = from;
        _bfs.Enqueue(index);
        return true;
    }

    void TryParent(int x, int y, ref int best, ref int from)
    {
        if (!InBounds(x, y)) return;

        int index = ColonyGridIndex.From(x, y, gridX);
        if (!_open[index]) return;

        int cost = _dist[index] + 1;
        if (best >= 0 && cost >= best) return;

        best = cost;
        from = index;
    }

    void SeedIfEmpty(int x, int y)
    {
        if (HasBlock(x, y)) return;
        int index = ColonyGridIndex.From(x, y, gridX);
        if (_open[index]) return;

        _open[index] = true;
        _cameFrom[index] = -1;
        _dist[index] = 0;
        _bfs.Enqueue(index);
    }

    void Relax(int x, int y, int from)
    {
        if (!InBounds(x, y) || HasBlock(x, y)) return;

        int index = ColonyGridIndex.From(x, y, gridX);
        int cost = _dist[from] + 1;
        if (_open[index] && _dist[index] <= cost) return;

        _open[index] = true;
        _cameFrom[index] = from;
        _dist[index] = cost;
        _bfs.Enqueue(index);
    }

    bool ChooseApproach(int x, int y, out GridApproach approach)
    {
        approach = GridApproach.Bottom;
        if (FaceCost(x, y, GridApproach.Bottom) >= 0) return true;

        int left = FaceCost(x, y, GridApproach.Left);
        int right = FaceCost(x, y, GridApproach.Right);
        if (left >= 0 || right >= 0)
        {
            if (left < 0) approach = GridApproach.Right;
            else if (right < 0) approach = GridApproach.Left;
            else approach = left <= right ? GridApproach.Left : GridApproach.Right;
            return true;
        }

        if (FaceCost(x, y, GridApproach.Top) >= 0) { approach = GridApproach.Top; return true; }
        return false;
    }

    int FaceCost(int x, int y, GridApproach approach)
    {
        FaceNeighbor(x, y, approach, out int nx, out int ny);
        if (!InBounds(nx, ny)) return 0;

        int index = ColonyGridIndex.From(nx, ny, gridX);
        return _open[index] ? _dist[index] + 1 : -1;
    }

    void TraceToBorder(int index, out int borderIndex, out float length)
    {
        _chain.Clear();
        length = 0f;

        int guard = _open.Length;
        int previous = -1;

        while (index >= 0 && guard-- > 0)
        {
            if (previous >= 0)
                length += ColonyGridIndex.X(index, gridX) == ColonyGridIndex.X(previous, gridX) ? _stepZ : _stepX;

            _chain.Add(index);
            previous = index;
            index = _cameFrom[index];
        }

        borderIndex = _chain[_chain.Count - 1];
    }

    static void FaceNeighbor(int x, int y, GridApproach approach, out int nx, out int ny)
    {
        nx = x;
        ny = y;
        switch (approach)
        {
            case GridApproach.Bottom: ny = y + 1; break;
            case GridApproach.Top: ny = y - 1; break;
            case GridApproach.Right: nx = x + 1; break;
            default: nx = x - 1; break;
        }
    }
}
