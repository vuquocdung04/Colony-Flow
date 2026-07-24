using UnityEngine;

public partial class GridTop
{
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (Application.isPlaying) EnsureLayout();
        else RefreshLayout();

        Vector3 size = new Vector3(_cell.x, 0f, _cell.z);
        Vector3 scale = Holder.lossyScale;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.color = gizmoColor;
        for (int y = 0; y < gridY; y++)
            for (int x = 0; x < gridX; x++)
                DrawCell(x, y, size, scale);

        Gizmos.matrix = Matrix4x4.TRS(Holder.position, Holder.rotation, scale);
        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_totalWidth, 0f, _totalDepth));

        DrawBorderGizmos(size, scale);
        DrawSquareGizmos();
        DrawHoleGizmos(size, scale);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    void DrawBorderGizmos(Vector3 size, Vector3 scale)
    {
        Gizmos.color = borderRingColor;

        for (int x = -BorderRing; x <= gridX; x++)
        {
            DrawCell(x, -BorderRing, size, scale);
            DrawCell(x, gridY, size, scale);
        }

        for (int y = 0; y < gridY; y++)
        {
            DrawCell(-BorderRing, y, size, scale);
            DrawCell(gridX, y, size, scale);
        }

        Vector2 padded = PaddedSize;

        Gizmos.matrix = Matrix4x4.TRS(Holder.position, Holder.rotation, scale);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_halfWidth * 2f, 0f, _halfDepth * 2f));
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(padded.x, 0f, padded.y));
    }

    void DrawSquareGizmos()
    {
        if (squareBound == null) return;

        Bounds bounds = squareBound.bounds;

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = squareBoundColor;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, Holder.position.y, bounds.center.z),
                            new Vector3(bounds.size.x, 0f, bounds.size.z));
    }

    void DrawHoleGizmos(Vector3 size, Vector3 scale)
    {
        if (!_gateReady) return;

        Gizmos.color = holeEdgeColor;

        if (_holeHorizontal)
            for (int x = -BorderRing; x <= gridX; x++) DrawHoleLink(x, _holeRing, size, scale);
        else
            for (int y = -BorderRing; y <= gridY; y++) DrawHoleLink(_holeRing, y, size, scale);

        float radius = _cell.x * 0.2f * Mathf.Abs(scale.x);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = entranceColor;
        Gizmos.DrawLine(_gateA, _gateB);
        Gizmos.DrawLine(_gateA, _gateBaseA);
        Gizmos.DrawLine(_gateB, _gateBaseB);
        Gizmos.DrawLine(_gateBaseA, _gateBaseB);
        Gizmos.DrawWireSphere(_gateA, radius);
        Gizmos.DrawWireSphere(_gateB, radius);
    }

    void DrawHoleLink(int x, int y, Vector3 size, Vector3 scale)
    {
        Vector3 center = CellWorld(x, y);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(_holePosition, center);

        Gizmos.matrix = Matrix4x4.TRS(center, Holder.rotation, scale);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }

    void DrawCell(int x, int y, Vector3 size, Vector3 scale)
    {
        Gizmos.matrix = Matrix4x4.TRS(CellWorld(x, y), Holder.rotation, scale);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
