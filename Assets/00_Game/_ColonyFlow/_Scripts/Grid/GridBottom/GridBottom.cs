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
                item.Bind(this);
                item.Setup(pair.Key, slot.Value, gridTop, waitAreas);
                _slots[index] = item;
            }
        }

        ApplyFlags(data.hiddens, ColonyMark.Hidden);
        ApplyFlags(data.locks, ColonyMark.Lock);
        RefreshRows(true);
    }

    enum ColonyMark { Hidden, Lock }

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
