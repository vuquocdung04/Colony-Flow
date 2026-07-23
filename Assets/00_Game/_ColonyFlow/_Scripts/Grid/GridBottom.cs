using System.Collections.Generic;
using UnityEngine;

public class GridBottom : MonoBehaviour
{
    public Anthill anthill;
    public Transform holder;

    [Min(1)] public int gridX = 4;
    [Min(1)] public int gridY = 2;

    public float spacingX = 0f;
    public float spacingZ = 0f;

    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 1f, 1f, 0.35f);
    public Color gizmoBorderColor = new Color(1f, 0.75f, 0.2f, 0.9f);

    public Transform Holder => holder != null ? holder : transform;

    public int SlotCount => gridX * gridY;

    public Vector3 CellSize => anthill != null ? anthill.Size : Vector3.one;

    public Vector3 CellCenter(int x, int y) => CellCenter(x, y, CellSize);

    public Vector3 SlotCenter(int index) => SlotCenter(index, CellSize);

    [System.NonSerialized] Anthill[] _slots;
    [System.NonSerialized] int[] _capacity;

    public Anthill SlotAt(int index) =>
        _slots != null && index >= 0 && index < _slots.Length ? _slots[index] : null;

    public int CapacityAt(int index) =>
        _capacity != null && index >= 0 && index < _capacity.Length ? _capacity[index] : 0;

    public void Load(BottomGridData data, GridTop gridTop, WaitAreas waitAreas)
    {
        Clear();
        if (data == null) return;

        gridX = Mathf.Max(1, data.gridX);
        gridY = Mathf.Max(1, data.gridY);
        _slots = new Anthill[SlotCount];
        _capacity = new int[SlotCount];

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
                item.Setup(pair.Key, slot.Value, gridTop, waitAreas);
                _slots[index] = item;
                _capacity[index] = slot.Value;
            }
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
        _capacity = null;
    }

    Vector3 SlotCenter(int index, Vector3 cell) =>
        CellCenter(ColonyGridIndex.X(index, gridX), ColonyGridIndex.Y(index, gridX), cell);

    Vector3 CellCenter(int x, int y, Vector3 cell)
    {
        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float originX = -totalWidth * 0.5f + cell.x * 0.5f;

        Vector3 local = new Vector3(
            originX + x * (cell.x + spacingX),
            0f,
            -y * (cell.z + spacingZ));

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
        Vector3 borderCenter = Holder.TransformPoint(new Vector3(0f, 0f, cell.z * 0.5f - totalDepth * 0.5f));

        Gizmos.matrix = Matrix4x4.TRS(borderCenter, Holder.rotation, scale);
        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(totalWidth, 0f, totalDepth));

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
