using UnityEngine;

public partial class GridBottom
{
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
