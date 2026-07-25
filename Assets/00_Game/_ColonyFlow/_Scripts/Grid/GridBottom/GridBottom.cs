using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class GridBottom : MonoBehaviour
{
    [BoxGroup("Refs")] public Anthill anthill;
    [BoxGroup("Refs")] public Transform holder;

    [BoxGroup("Grid"), Min(1)] public int gridX = 4;
    [BoxGroup("Grid"), Min(1)] public int gridY = 2;
    [BoxGroup("Grid")] public float spacingX = 0f;
    [BoxGroup("Grid")] public float spacingZ = 0f;

    [BoxGroup("Gizmos")] public bool showGizmos = true;
    [BoxGroup("Gizmos")] public Color gizmoColor = new Color(1f, 1f, 1f, 0.35f);
    [BoxGroup("Gizmos")] public Color gizmoBorderColor = new Color(1f, 0.75f, 0.2f, 0.9f);

    [System.NonSerialized] Anthill[] _slots;

    public Transform Holder => holder != null ? holder : transform;

    public int SlotCount => gridX * gridY;

    public Anthill SlotAt(int index) =>
        _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : null;

    public void Load(BottomGridData data, GridTop gridTop, WaitAreas waitAreas)
    {
        Clear();
        if (data == null) return;

        gridX = Mathf.Max(1, data.gridX);
        gridY = Mathf.Max(1, data.gridY);
        _slots = new Anthill[SlotCount];

        if (anthill == null || data.colors == null) return;

        Vector3 cell = CellSize;
        Quaternion rotation = anthill.transform.rotation;

        foreach (KeyValuePair<string, Dictionary<int, int>> pair in data.colors)
        {
            if (pair.Value == null) continue;

            foreach (KeyValuePair<int, int> slot in pair.Value)
            {
                int index = slot.Key;
                if (index < 0 || index >= _slots.Length || _slots[index] != null) continue;

                Anthill item = Instantiate(anthill, SlotCenter(index, cell), rotation, Holder);
                item.Bind(this, index);
                item.Setup(pair.Key, slot.Value, gridTop, waitAreas);
                _slots[index] = item;
            }
        }

        ApplyFlags(data.hiddens, ColonyMark.Hidden);
        ApplyFlags(data.locks, ColonyMark.Lock);
        RefreshRows(true);
        ApplyLinks(data.links);
    }

    enum ColonyMark { Hidden, Lock }

    void ApplyLinks(List<List<int>> links)
    {
        if (links == null) return;

        foreach (List<int> group in links)
        {
            if (group == null || group.Count < 2) continue;

            List<int> ordered = OrderChain(group);

            LinkGroup linkGroup = new LinkGroup(this);
            foreach (int index in ordered)
            {
                Anthill member = SlotAt(index);
                if (member != null) linkGroup.members.Add(member);
            }

            foreach (Anthill member in linkGroup.members)
                if (member.Link != null) member.Link.group = linkGroup;

            for (int i = 0; i < linkGroup.members.Count - 1; i++)
            {
                Anthill a = linkGroup.members[i];
                Anthill b = linkGroup.members[i + 1];

                a.Connect(b, true);
                b.Connect(a, false);
            }
        }
    }

    List<int> OrderChain(List<int> group)
    {
        int n = group.Count;
        if (n <= 2) return new List<int>(group);

        Vector2[] pos = new Vector2[n];
        for (int i = 0; i < n; i++)
            pos[i] = new Vector2(ColonyGridIndex.X(group[i], gridX), ColonyGridIndex.Y(group[i], gridX));

        int start = 0;
        float farthest = -1f;
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                float d = (pos[i] - pos[j]).sqrMagnitude;
                if (d > farthest) { farthest = d; start = i; }
            }

        List<int> order = new List<int>(n);
        bool[] used = new bool[n];
        int current = start;

        order.Add(group[current]);
        used[current] = true;

        for (int step = 1; step < n; step++)
        {
            int next = -1;
            float nearest = float.MaxValue;
            for (int k = 0; k < n; k++)
            {
                if (used[k]) continue;
                float d = (pos[current] - pos[k]).sqrMagnitude;
                if (d < nearest) { nearest = d; next = k; }
            }

            if (next < 0) break;
            order.Add(group[next]);
            used[next] = true;
            current = next;
        }

        return order;
    }

    void ApplyFlags(List<int> indices, ColonyMark mark)
    {
        if (indices == null) return;

        foreach (int index in indices)
        {
            Anthill item = SlotAt(index);
            if (item == null) continue;

            if (mark == ColonyMark.Hidden) item.SetHidden(true);
            else item.SetLocked(true);
        }
    }

    public void Clear()
    {
        if (_slots != null)
        {
            foreach (Anthill item in _slots)
            {
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item.gameObject);
                else DestroyImmediate(item.gameObject);
            }
        }

        _slots = null;
    }
}
