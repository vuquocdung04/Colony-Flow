using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class GridTop : MonoBehaviour
{
    [BoxGroup("Refs")] public FoodObj foodObj;
    [BoxGroup("Refs")] public FoodKey foodKey;
    [BoxGroup("Refs")] public Transform holder;
    [BoxGroup("Refs")] public Transform hole;

    [BoxGroup("Grid"), Min(1)] public int gridX = 24;
    [BoxGroup("Grid"), Min(1)] public int gridY = 24;
    [BoxGroup("Grid")] public float spacingX = 0f;
    [BoxGroup("Grid")] public float spacingZ = 0f;

    [BoxGroup("Entrance"), Min(0f)] public float entranceSpread = 0f;
    [BoxGroup("Entrance")] public float entranceOffsetX = 0f;
    [BoxGroup("Entrance")] public float entranceOffsetZ = 0f;

    [BoxGroup("Fit")] public Renderer squareBound;
    [BoxGroup("Fit")] public bool fitOnLoad = true;
    [BoxGroup("Fit")] public bool alignToSquareCenter = false;

    [BoxGroup("Gizmos")] public bool showGizmos = true;
    [BoxGroup("Gizmos")] public Color gizmoColor = new Color(1f, 1f, 1f, 0.35f);
    [BoxGroup("Gizmos")] public Color gizmoBorderColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    [BoxGroup("Gizmos")] public Color borderRingColor = new Color(1f, 0.55f, 0.1f, 0.75f);
    [BoxGroup("Gizmos")] public Color squareBoundColor = new Color(1f, 1f, 1f, 0.5f);
    [BoxGroup("Gizmos")] public Color entranceColor = Color.cyan;
    [BoxGroup("Gizmos")] public Color holeEdgeColor = Color.black;

    [System.NonSerialized] string[] _colors;
    [System.NonSerialized] bool[] _reserved;
    [System.NonSerialized] bool[] _hidden;
    [System.NonSerialized] int _hiddenRemaining;
    [System.NonSerialized] FoodObj[] _cells;

    [System.NonSerialized] string[] _keyColor;
    [System.NonSerialized] FoodKey[] _keyObjects;
    [System.NonSerialized] int _keyLockedRemaining;
    readonly List<LockRequest> _lockRequests = new List<LockRequest>();

    class LockRequest
    {
        public string color;
        public Transform target;
        public System.Action onDone;
    }

    [System.NonSerialized] bool[] _open;
    [System.NonSerialized] int[] _cameFrom;
    [System.NonSerialized] int[] _dist;

    [System.NonSerialized] int _remaining;

    readonly Queue<int> _bfs = new Queue<int>();
    readonly List<int> _chain = new List<int>();
    readonly List<Vector3> _corridor = new List<Vector3>();

    public Transform Holder => holder != null ? holder : transform;

    public int Remaining => _remaining;

    public FoodObj CellAt(int index) =>
        _cells != null && index >= 0 && index < _cells.Length ? _cells[index] : null;

    public void Load(TopGridData data)
    {
        Clear();
        if (data == null) return;

        gridX = Mathf.Max(1, data.gridX);
        gridY = Mathf.Max(1, data.gridY);

        if (fitOnLoad) FitToSquare();
        RefreshLayout();

        int count = gridX * gridY;
        _colors = new string[count];
        _reserved = new bool[count];
        _hidden = new bool[count];
        _hiddenRemaining = 0;
        _cells = new FoodObj[count];
        _remaining = 0;

        _keyColor = new string[count];
        _keyObjects = new FoodKey[count];
        _keyLockedRemaining = 0;
        _lockRequests.Clear();

        HashSet<int> hiddenSet = data.hiddens != null && data.hiddens.Count > 0
            ? new HashSet<int>(data.hiddens)
            : null;

        HashSet<int> keySet = IndexSet(data.keys);

        Quaternion rotation = foodObj != null ? foodObj.transform.rotation : Quaternion.identity;

        if (data.colors != null)
        {
            foreach (KeyValuePair<string, List<int>> pair in data.colors)
            {
                if (pair.Value == null) continue;

                foreach (int index in pair.Value)
                {
                    if (index < 0 || index >= count || !string.IsNullOrEmpty(_colors[index])) continue;
                    if (keySet != null && keySet.Contains(index)) continue;

                    _colors[index] = pair.Key;
                    _remaining++;

                    if (foodObj == null) continue;

                    int x = ColonyGridIndex.X(index, gridX);
                    int y = ColonyGridIndex.Y(index, gridX);

                    FoodObj item = Instantiate(foodObj, CellWorld(x, y), rotation, Holder);
                    item.SetColor(pair.Key);

                    if (hiddenSet != null && hiddenSet.Contains(index))
                    {
                        _hidden[index] = true;
                        _hiddenRemaining++;
                        item.SetHidden(true);
                    }

                    _cells[index] = item;
                }
            }
        }

        SpawnKeys(data, count);
        RebuildOpen();
    }

    static HashSet<int> IndexSet(Dictionary<string, List<int>> groups)
    {
        if (groups == null || groups.Count == 0) return null;

        HashSet<int> set = new HashSet<int>();
        foreach (KeyValuePair<string, List<int>> pair in groups)
        {
            if (pair.Value == null) continue;
            foreach (int index in pair.Value) set.Add(index);
        }
        return set.Count > 0 ? set : null;
    }

    void SpawnKeys(TopGridData data, int count)
    {
        if (foodKey == null || data.keys == null) return;

        Quaternion rotation = foodKey.transform.rotation;

        foreach (KeyValuePair<string, List<int>> pair in data.keys)
        {
            if (pair.Value == null) continue;

            foreach (int index in pair.Value)
            {
                if (index < 0 || index >= count || !string.IsNullOrEmpty(_keyColor[index])) continue;

                _keyColor[index] = pair.Key;

                int x = ColonyGridIndex.X(index, gridX);
                int y = ColonyGridIndex.Y(index, gridX);

                FoodKey key = Instantiate(foodKey, CellWorld(x, y), rotation, Holder);
                key.SetColor(pair.Key);
                key.SetLocked(true);

                _keyObjects[index] = key;
                _keyLockedRemaining++;
            }
        }
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

        if (_keyObjects != null)
        {
            foreach (FoodKey key in _keyObjects)
            {
                if (key == null) continue;
                if (Application.isPlaying) Destroy(key.gameObject);
                else DestroyImmediate(key.gameObject);
            }
        }

        _colors = null;
        _reserved = null;
        _hidden = null;
        _hiddenRemaining = 0;
        _cells = null;
        _keyColor = null;
        _keyObjects = null;
        _keyLockedRemaining = 0;
        _lockRequests.Clear();
        _open = null;
        _cameFrom = null;
        _dist = null;
        _remaining = 0;
    }

    public bool TryReserve(string hex, Vector3 fromWorld, out GridTarget target)
    {
        target = GridTarget.None;
        if (_colors == null || string.IsNullOrEmpty(hex)) return false;

        EnsureLayout();
        ResolveStraight(fromWorld, out Vector3 boundary, out _, out _, out _);
        float fromT = BoundaryT(boundary);

        float best = float.MaxValue;

        for (int index = 0; index < _colors.Length; index++)
        {
            if (_reserved[index] || _colors[index] != hex) continue;

            int x = ColonyGridIndex.X(index, gridX);
            int y = ColonyGridIndex.Y(index, gridX);
            if (!ChooseApproach(x, y, out GridApproach approach)) continue;

            GridTarget candidate = new GridTarget { index = index, x = x, y = y, approach = approach };
            float cost = TravelCost(fromT, candidate);
            if (cost > best) continue;

            best = cost;
            target = candidate;
        }

        if (!target.IsValid) return false;

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

        _colors[index] = null;
        _reserved[index] = false;
        _remaining--;

        OpenCell(ColonyGridIndex.X(index, gridX), ColonyGridIndex.Y(index, gridX));

        if (_cells[index] == null) return;
        _cells[index].Collect(ant);
        _cells[index] = null;
    }

    public void RequestKey(string colorHex, Transform target, System.Action onDone)
    {
        if (string.IsNullOrEmpty(colorHex))
        {
            onDone?.Invoke();
            return;
        }

        if (TryFlyKey(colorHex, target, onDone)) return;

        _lockRequests.Add(new LockRequest { color = colorHex, target = target, onDone = onDone });
    }

    bool TryFlyKey(string colorHex, Transform target, System.Action onDone)
    {
        int index = FindUnlockedKey(colorHex);
        if (index < 0) return false;

        ConsumeKey(index, target, onDone);
        return true;
    }

    int FindUnlockedKey(string colorHex)
    {
        if (_keyColor == null) return -1;

        for (int i = 0; i < _keyColor.Length; i++)
        {
            if (!SameColor(_keyColor[i], colorHex)) continue;

            FoodKey key = _keyObjects[i];
            if (key != null && !key.IsLocked) return i;
        }
        return -1;
    }

    void ConsumeKey(int index, Transform target, System.Action onDone)
    {
        FoodKey key = _keyObjects[index];
        _keyColor[index] = null;
        _keyObjects[index] = null;

        OpenCell(ColonyGridIndex.X(index, gridX), ColonyGridIndex.Y(index, gridX));

        if (key != null && target != null)
        {
            key.Fly(target.position, () => onDone?.Invoke());
        }
        else
        {
            if (key != null) Destroy(key.gameObject);
            onDone?.Invoke();
        }
    }

    static bool SameColor(string a, string b) =>
        !string.IsNullOrEmpty(a) && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
}
