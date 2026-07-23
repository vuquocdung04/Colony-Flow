using System.Collections.Generic;
using UnityEngine;

public class GridTop : MonoBehaviour
{
    public FoodObj foodObj;

    [Min(1)] public int gridX = 24;
    [Min(1)] public int gridY = 24;

    public float spacingX = 0f;
    public float spacingZ = 0f;

    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 1f, 1f, 0.35f);
    public Color gizmoBorderColor = new Color(0.2f, 0.9f, 1f, 0.9f);

    public Vector3 CellSize => foodObj != null ? foodObj.Size : Vector3.one;

    public Vector3 CellCenter(int x, int y) => CellCenter(x, y, CellSize);

    [System.NonSerialized] FoodObj[] _cells;

    public FoodObj CellAt(int index) =>
        _cells != null && index >= 0 && index < _cells.Length ? _cells[index] : null;

    public void Load(TopGridData data)
    {
        Clear();
        if (data == null) return;

        gridX = Mathf.Max(1, data.gridX);
        gridY = Mathf.Max(1, data.gridY);
        _cells = new FoodObj[gridX * gridY];

        if (foodObj == null || data.colors == null) return;

        Vector3 cell = CellSize;
        Quaternion rotation = foodObj.transform.rotation;

        foreach (KeyValuePair<string, List<int>> pair in data.colors)
        {
            if (pair.Value == null) continue;
            Color color = ColonyPalette.ToColor(pair.Key);

            foreach (int index in pair.Value)
            {
                if (index < 0 || index >= _cells.Length || _cells[index] != null) continue;

                int x = ColonyGridIndex.X(index, gridX);
                int y = ColonyGridIndex.Y(index, gridX);

                FoodObj item = Instantiate(foodObj, CellCenter(x, y, cell), rotation, transform);
                item.SetColor(color);
                _cells[index] = item;
            }
        }
    }

    public void Clear()
    {
        if (_cells == null) return;

        foreach (FoodObj item in _cells)
        {
            if (item == null) continue;
            if (Application.isPlaying) Destroy(item.gameObject);
            else DestroyImmediate(item.gameObject);
        }
        _cells = null;
    }

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

        return transform.TransformPoint(local);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 cell = CellSize;
        Vector3 size = new Vector3(cell.x, 0f, cell.z);

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Vector3 scale = transform.lossyScale;

        Gizmos.color = gizmoColor;
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                Gizmos.matrix = Matrix4x4.TRS(CellCenter(x, y, cell), transform.rotation, scale);
                Gizmos.DrawWireCube(Vector3.zero, size);
            }
        }

        float totalWidth = gridX * cell.x + (gridX - 1) * spacingX;
        float totalDepth = gridY * cell.z + (gridY - 1) * spacingZ;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(totalWidth, 0f, totalDepth));

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
