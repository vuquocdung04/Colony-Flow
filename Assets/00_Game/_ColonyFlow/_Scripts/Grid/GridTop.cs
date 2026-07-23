using System.Collections.Generic;
using UnityEngine;

public class GridTop : MonoBehaviour
{
    public FoodObj foodObj;
    public Transform holder;
    public Transform hole;

    [Min(1)] public int gridX = 24;
    [Min(1)] public int gridY = 24;

    public float spacingX = 0f;
    public float spacingZ = 0f;

    [Min(0)] public int entranceSpread = 0;
    public float entranceOffsetX = 0f;
    public float entranceOffsetZ = 0f;

    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 1f, 1f, 0.35f);
    public Color gizmoBorderColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    public Color entranceColor = Color.cyan;
    public Color holeEdgeColor = Color.black;

    [System.NonSerialized] string[] _colors;
    [System.NonSerialized] bool[] _reserved;
    [System.NonSerialized] FoodObj[] _cells;

    [System.NonSerialized] int[] _left;
    [System.NonSerialized] int[] _right;
    [System.NonSerialized] int[] _top;
    [System.NonSerialized] int[] _bottom;

    [System.NonSerialized] int _remaining;

    readonly List<GridTarget> _candidates = new List<GridTarget>();

    public Transform Holder => holder != null ? holder : transform;

    public Vector3 HolePosition => hole != null ? hole.position : Holder.position;

    public Vector3 CellSize => foodObj != null ? foodObj.Size : Vector3.one;

    public int Remaining => _remaining;

    public Vector3 CellCenter(int x, int y) => CellCenter(x, y, CellSize);

    public FoodObj CellAt(int index) =>
        _cells != null && index >= 0 && index < _cells.Length ? _cells[index] : null;

    public bool HasBlock(int x, int y) =>
        InBounds(x, y) && !string.IsNullOrEmpty(_colors[ColonyGridIndex.From(x, y, gridX)]);

    public bool IsReachable(int x, int y) =>
        InBounds(x, y) && (_left[y] == x || _right[y] == x || _top[x] == y || _bottom[x] == y);

    public void Load(TopGridData data)
    {
        Clear();
        if (data == null) return;

        gridX = Mathf.Max(1, data.gridX);
        gridY = Mathf.Max(1, data.gridY);

        int count = gridX * gridY;
        _colors = new string[count];
        _reserved = new bool[count];
        _cells = new FoodObj[count];
        _remaining = 0;

        Vector3 cell = CellSize;
        Quaternion rotation = foodObj != null ? foodObj.transform.rotation : Quaternion.identity;

        if (data.colors != null)
        {
            foreach (KeyValuePair<string, List<int>> pair in data.colors)
            {
                if (pair.Value == null) continue;

                foreach (int index in pair.Value)
                {
                    if (index < 0 || index >= count || !string.IsNullOrEmpty(_colors[index])) continue;

                    _colors[index] = pair.Key;
                    _remaining++;

                    if (foodObj == null) continue;

                    int x = ColonyGridIndex.X(index, gridX);
                    int y = ColonyGridIndex.Y(index, gridX);
                    FoodObj item = Instantiate(foodObj, CellCenter(x, y, cell), rotation, Holder);
                    item.SetColor(pair.Key);
                    _cells[index] = item;
                }
            }
        }

        BuildFrontier();
    }

    public void Clear()
    {
        if (_cells != null)
        {
            foreach (FoodObj item in _cells)
            {
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item.gameObject);
                else DestroyImmediate(item.gameObject);
            }
        }

        _colors = null;
        _reserved = null;
        _cells = null;
        _left = null;
        _right = null;
        _top = null;
        _bottom = null;
        _remaining = 0;
    }

    public bool TryReserve(string hex, out GridTarget target)
    {
        target = GridTarget.None;
        if (_colors == null || string.IsNullOrEmpty(hex)) return false;

        _candidates.Clear();

        for (int y = 0; y < gridY; y++)
        {
            AddCandidate(_left[y], y, hex, GridApproach.Left);
            AddCandidate(_right[y], y, hex, GridApproach.Right);
        }

        for (int x = 0; x < gridX; x++)
        {
            AddCandidate(x, _top[x], hex, GridApproach.Top);
            AddCandidate(x, _bottom[x], hex, GridApproach.Bottom);
        }

        if (_candidates.Count == 0) return false;

        target = _candidates[Random.Range(0, _candidates.Count)];
        _reserved[target.index] = true;
        return true;
    }

    public void Release(int index)
    {
        if (_reserved != null && index >= 0 && index < _reserved.Length) _reserved[index] = false;
    }

    public void Collect(int index, Ant ant)
    {
        if (_colors == null || index < 0 || index >= _colors.Length) return;
        if (string.IsNullOrEmpty(_colors[index])) return;

        int x = ColonyGridIndex.X(index, gridX);
        int y = ColonyGridIndex.Y(index, gridX);

        _colors[index] = null;
        _reserved[index] = false;
        _remaining--;

        SlideRow(x, y);
        SlideColumn(x, y);

        if (_cells[index] == null) return;
        _cells[index].Collect(ant);
        _cells[index] = null;
    }

    void AddCandidate(int x, int y, string hex, GridApproach approach)
    {
        if (x < 0 || y < 0) return;

        int index = ColonyGridIndex.From(x, y, gridX);
        if (_reserved[index] || _colors[index] != hex) return;

        for (int i = 0; i < _candidates.Count; i++)
            if (_candidates[i].index == index) return;

        _candidates.Add(new GridTarget { index = index, x = x, y = y, approach = approach });
    }

    void BuildFrontier()
    {
        _left = new int[gridY];
        _right = new int[gridY];
        _top = new int[gridX];
        _bottom = new int[gridX];

        for (int y = 0; y < gridY; y++) RefreshRow(y);
        for (int x = 0; x < gridX; x++) RefreshColumn(x);
    }

    void RefreshRow(int y)
    {
        _left[y] = -1;
        _right[y] = -1;

        for (int x = 0; x < gridX; x++)
        {
            if (!HasBlock(x, y)) continue;
            if (_left[y] < 0) _left[y] = x;
            _right[y] = x;
        }
    }

    void RefreshColumn(int x)
    {
        _top[x] = -1;
        _bottom[x] = -1;

        for (int y = 0; y < gridY; y++)
        {
            if (!HasBlock(x, y)) continue;
            if (_top[x] < 0) _top[x] = y;
            _bottom[x] = y;
        }
    }

    void SlideRow(int x, int y)
    {
        if (_left[y] == x)
        {
            int next = -1;
            for (int i = x + 1; i < gridX; i++)
                if (HasBlock(i, y)) { next = i; break; }
            _left[y] = next;
        }

        if (_right[y] == x)
        {
            int next = -1;
            for (int i = x - 1; i >= 0; i--)
                if (HasBlock(i, y)) { next = i; break; }
            _right[y] = next;
        }
    }

    void SlideColumn(int x, int y)
    {
        if (_top[x] == y)
        {
            int next = -1;
            for (int i = y + 1; i < gridY; i++)
                if (HasBlock(x, i)) { next = i; break; }
            _top[x] = next;
        }

        if (_bottom[x] == y)
        {
            int next = -1;
            for (int i = y - 1; i >= 0; i--)
                if (HasBlock(x, i)) { next = i; break; }
            _bottom[x] = next;
        }
    }

    public bool TryGetHoleEdge(out bool horizontal, out int edgeFixed)
    {
        horizontal = true;
        edgeFixed = 0;
        if (!TryGetGate(0, 0f, CellSize, out Vector2Int a, out Vector2Int b)) return false;

        horizontal = a.y == b.y;
        edgeFixed = horizontal ? a.y : a.x;
        return true;
    }

    public bool TryGetEntranceGate(out Vector2Int cellA, out Vector2Int cellB, out Vector3 pointA, out Vector3 pointB)
    {
        cellA = cellB = Vector2Int.zero;
        pointA = pointB = Vector3.zero;

        Vector3 cell = CellSize;
        if (!TryGetGate(entranceSpread, entranceOffsetX, cell, out cellA, out cellB)) return false;

        pointA = OffsetOut(CellCenter(cellA.x, cellA.y, cell), cell);
        pointB = OffsetOut(CellCenter(cellB.x, cellB.y, cell), cell);
        return true;
    }

    Vector3 OffsetOut(Vector3 world, Vector3 cell)
    {
        Vector3 local = Holder.InverseTransformPoint(world);
        local.z += entranceOffsetZ * (cell.z + spacingZ);
        return Holder.TransformPoint(local);
    }

    bool TryGetGate(int spread, float offsetX, Vector3 cell, out Vector2Int a, out Vector2Int b)
    {
        a = b = Vector2Int.zero;
        if (hole == null) return false;

        float stepX = cell.x + spacingX;
        float stepZ = cell.z + spacingZ;
        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float totalDepth = gridY * cell.z + (gridY - 1) * spacingZ;
        float originX = -totalWidth * 0.5f + cell.x * 0.5f;
        float originZ = totalDepth * 0.5f - cell.z * 0.5f;

        Vector3 local = Holder.InverseTransformPoint(hole.position);
        local.x += offsetX * stepX;

        float fcol = (local.x - originX) / stepX;
        float frow = (originZ - local.z) / stepZ;

        int maxX = gridX - 1;
        int maxY = gridY - 1;

        float outLeft = -fcol;
        float outRight = fcol - maxX;
        float outTop = -frow;
        float outBottom = frow - maxY;
        float outside = Mathf.Max(Mathf.Max(outLeft, outRight), Mathf.Max(outTop, outBottom));

        bool horizontalEdge;
        int edgeFixed;

        if (outside <= 0f)
        {
            float toLeft = fcol, toRight = maxX - fcol, toTop = frow, toBottom = maxY - frow;
            float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toTop, toBottom));

            if (nearest == toTop) { horizontalEdge = true; edgeFixed = 0; }
            else if (nearest == toBottom) { horizontalEdge = true; edgeFixed = maxY; }
            else if (nearest == toLeft) { horizontalEdge = false; edgeFixed = 0; }
            else { horizontalEdge = false; edgeFixed = maxX; }
        }
        else if (outside == outTop) { horizontalEdge = true; edgeFixed = 0; }
        else if (outside == outBottom) { horizontalEdge = true; edgeFixed = maxY; }
        else if (outside == outLeft) { horizontalEdge = false; edgeFixed = 0; }
        else { horizontalEdge = false; edgeFixed = maxX; }

        if (horizontalEdge)
        {
            int c0 = Mathf.FloorToInt(fcol);
            int left = Mathf.Clamp(c0 - spread, 0, maxX);
            int right = Mathf.Clamp(c0 + 1 + spread, 0, maxX);
            a = new Vector2Int(left, edgeFixed);
            b = new Vector2Int(right, edgeFixed);
        }
        else
        {
            int r0 = Mathf.FloorToInt(frow);
            int near = Mathf.Clamp(r0 - spread, 0, maxY);
            int far = Mathf.Clamp(r0 + 1 + spread, 0, maxY);
            a = new Vector2Int(edgeFixed, near);
            b = new Vector2Int(edgeFixed, far);
        }

        return true;
    }

    public Vector3 EntryPoint(GridTarget target) => EntryPoint(target, CellSize);

    public void BuildApproachPath(Vector3 fromWorld, GridTarget target, List<Vector3> path)
    {
        Vector3 cell = CellSize;
        Vector3 entry = EntryPoint(target, cell);

        AppendPerimeter(fromWorld, entry, path, cell);
        path.Add(entry);
        path.Add(CellCenter(target.x, target.y, cell));
    }

    public void BuildExitPath(GridTarget target, Vector3 toWorld, List<Vector3> path)
    {
        Vector3 cell = CellSize;
        Vector3 entry = EntryPoint(target, cell);

        path.Add(entry);
        AppendPerimeter(entry, toWorld, path, cell);
        path.Add(toWorld);
    }

    Vector3 EntryPoint(GridTarget target, Vector3 cell)
    {
        switch (target.approach)
        {
            case GridApproach.Left: return CellCenter(-1, target.y, cell);
            case GridApproach.Right: return CellCenter(gridX, target.y, cell);
            case GridApproach.Top: return CellCenter(target.x, -1, cell);
            default: return CellCenter(target.x, gridY, cell);
        }
    }

    void AppendPerimeter(Vector3 fromWorld, Vector3 toWorld, List<Vector3> path, Vector3 cell)
    {
        float hx = HalfWidth(cell);
        float hz = HalfDepth(cell);

        Vector3 fromLocal = Holder.InverseTransformPoint(fromWorld);
        Vector3 toLocal = Holder.InverseTransformPoint(toWorld);

        float perimeter = 4f * (hx + hz);
        float from = PerimeterT(fromLocal.x, fromLocal.z, hx, hz);
        float to = PerimeterT(toLocal.x, toLocal.z, hx, hz);

        Vector3 entryLocal = PerimeterPoint(from, hx, hz);
        float offsetX = entryLocal.x - fromLocal.x;
        float offsetZ = entryLocal.z - fromLocal.z;
        if (offsetX * offsetX + offsetZ * offsetZ > 0.0001f)
            path.Add(Holder.TransformPoint(entryLocal));

        float forward = Mathf.Repeat(to - from, perimeter);
        int step = forward <= perimeter - forward ? 1 : 3;

        int edge = EdgeOf(from, hx, hz);
        int endEdge = EdgeOf(to, hx, hz);

        for (int i = 0; i < 4 && edge != endEdge; i++)
        {
            int corner = step == 1 ? edge : (edge + 3) % 4;
            path.Add(Holder.TransformPoint(CornerPoint(corner, hx, hz)));
            edge = (edge + step) % 4;
        }
    }

    static Vector3 PerimeterPoint(float t, float hx, float hz)
    {
        if (t < 2f * hx) return new Vector3(-hx + t, 0f, hz);
        t -= 2f * hx;

        if (t < 2f * hz) return new Vector3(hx, 0f, hz - t);
        t -= 2f * hz;

        if (t < 2f * hx) return new Vector3(hx - t, 0f, -hz);
        t -= 2f * hx;

        return new Vector3(-hx, 0f, -hz + t);
    }

    static float PerimeterT(float x, float z, float hx, float hz)
    {
        float toTop = Mathf.Abs(hz - z);
        float toRight = Mathf.Abs(hx - x);
        float toBottom = Mathf.Abs(z + hz);
        float toLeft = Mathf.Abs(x + hx);
        float nearest = Mathf.Min(Mathf.Min(toTop, toRight), Mathf.Min(toBottom, toLeft));

        if (nearest == toTop) return Mathf.Clamp(x + hx, 0f, 2f * hx);
        if (nearest == toRight) return 2f * hx + Mathf.Clamp(hz - z, 0f, 2f * hz);
        if (nearest == toBottom) return 2f * hx + 2f * hz + Mathf.Clamp(hx - x, 0f, 2f * hx);
        return 4f * hx + 2f * hz + Mathf.Clamp(z + hz, 0f, 2f * hz);
    }

    static int EdgeOf(float t, float hx, float hz)
    {
        if (t < 2f * hx) return 0;
        if (t < 2f * hx + 2f * hz) return 1;
        if (t < 4f * hx + 2f * hz) return 2;
        return 3;
    }

    static Vector3 CornerPoint(int edge, float hx, float hz)
    {
        switch (edge)
        {
            case 0: return new Vector3(hx, 0f, hz);
            case 1: return new Vector3(hx, 0f, -hz);
            case 2: return new Vector3(-hx, 0f, -hz);
            default: return new Vector3(-hx, 0f, hz);
        }
    }

    float HalfWidth(Vector3 cell) => (gridX * cell.x + (gridX - 1) * spacingX) * 0.5f + cell.x * 0.5f + spacingX;

    float HalfDepth(Vector3 cell) => (gridY * cell.z + (gridY - 1) * spacingZ) * 0.5f + cell.z * 0.5f + spacingZ;

    bool InBounds(int x, int y) =>
        _colors != null && x >= 0 && y >= 0 && x < gridX && y < gridY;

    Vector3 CellCenter(int x, int y, Vector3 cell)
    {
        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float totalDepth = gridY * cell.z + (gridY - 1) * spacingZ;
        float originX = -totalWidth * 0.5f + cell.x * 0.5f;
        float originZ = totalDepth * 0.5f - cell.z * 0.5f;

        Vector3 local = new Vector3(
            originX + x * (cell.x + spacingX),
            0f,
            originZ - y * (cell.z + spacingZ));

        return Holder.TransformPoint(local);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 cell = CellSize;
        Vector3 size = new Vector3(cell.x, 0f, cell.z);
        Vector3 scale = Holder.lossyScale;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.color = gizmoColor;
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                Gizmos.matrix = Matrix4x4.TRS(CellCenter(x, y, cell), Holder.rotation, scale);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
        }

        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float totalDepth = gridY * cell.z + (gridY - 1) * spacingZ;

        Gizmos.matrix = Matrix4x4.TRS(Holder.position, Holder.rotation, scale);
        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(totalWidth, 0f, totalDepth));

        DrawHoleGizmos(cell, size, scale);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    void DrawHoleGizmos(Vector3 cell, Vector3 size, Vector3 scale)
    {
        if (hole == null) return;

        if (TryGetHoleEdge(out bool horizontal, out int edgeFixed))
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    bool onEdge = horizontal ? y == edgeFixed : x == edgeFixed;
                    if (!onEdge) continue;

                    Vector3 center = CellCenter(x, y, cell);

                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = holeEdgeColor;
                    Gizmos.DrawLine(hole.position, center);

                    Gizmos.matrix = Matrix4x4.TRS(center, Holder.rotation, scale);
                    Gizmos.DrawWireCube(Vector3.zero, size);
                }
            }
        }

        if (!TryGetEntranceGate(out Vector2Int cellA, out Vector2Int cellB, out Vector3 pointA, out Vector3 pointB)) return;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = entranceColor;
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawLine(pointA, CellCenter(cellA.x, cellA.y, cell));
        Gizmos.DrawLine(pointB, CellCenter(cellB.x, cellB.y, cell));
        Gizmos.DrawWireSphere(pointA, cell.x * 0.2f);
        Gizmos.DrawWireSphere(pointB, cell.x * 0.2f);

        Gizmos.matrix = Matrix4x4.TRS(CellCenter(cellA.x, cellA.y, cell), Holder.rotation, scale);
        Gizmos.DrawWireCube(Vector3.zero, size);

        Gizmos.matrix = Matrix4x4.TRS(CellCenter(cellB.x, cellB.y, cell), Holder.rotation, scale);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
