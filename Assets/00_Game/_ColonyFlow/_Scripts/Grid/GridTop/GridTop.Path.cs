using System.Collections.Generic;
using UnityEngine;

public partial class GridTop
{
    public Vector3 EntryPoint(GridTarget target)
    {
        EnsureLayout();
        return Holder.TransformPoint(EntryPointLocal(target));
    }

    public int BuildApproachPath(Vector3 fromWorld, GridTarget target, List<Vector3> path)
    {
        EnsureLayout();

        _corridor.Clear();
        Vector3 mouth = BuildCorridor(target, _corridor);

        Vector3 boundary = AppendStraightEntry(fromWorld, path);
        AppendPerimeter(boundary, mouth, path);

        int mouthIndex = path.Count;
        path.Add(mouth);
        for (int i = 0; i < _corridor.Count; i++) path.Add(_corridor[i]);
        path.Add(CellWorld(target.x, target.y));

        return mouthIndex;
    }

    public void BuildReturnPath(List<Vector3> entryPath, int mouthIndex, Vector3 toWorld, List<Vector3> path)
    {
        EnsureLayout();

        if (entryPath == null || mouthIndex < 0 || mouthIndex >= entryPath.Count)
        {
            path.Add(toWorld);
            return;
        }

        for (int i = entryPath.Count - 2; i >= mouthIndex; i--)
            path.Add(entryPath[i]);

        AppendPerimeter(entryPath[mouthIndex], toWorld, path);
        path.Add(toWorld);
    }

    public Vector3 StraightHit(Vector3 fromWorld)
    {
        EnsureLayout();
        ResolveStraight(fromWorld, out Vector3 boundary, out _, out _, out _);
        return boundary;
    }

    float BoundaryT(Vector3 boundary)
    {
        Vector3 local = Holder.InverseTransformPoint(boundary);
        return PerimeterT(local.x, local.z, _halfWidth, _halfDepth);
    }

    float TravelCost(float fromT, GridTarget target)
    {
        FaceNeighbor(target.x, target.y, target.approach, out int nx, out int ny);

        float corridor = 0f;
        Vector3 mouthLocal;

        if (InBounds(nx, ny))
        {
            TraceToBorder(ColonyGridIndex.From(nx, ny, gridX), out int borderIndex, out corridor);
            mouthLocal = ExteriorMouthLocal(ColonyGridIndex.X(borderIndex, gridX), ColonyGridIndex.Y(borderIndex, gridX));
        }
        else
        {
            mouthLocal = EntryPointLocal(target);
        }

        float to = PerimeterT(mouthLocal.x, mouthLocal.z, _halfWidth, _halfDepth);
        float forward = Mathf.Repeat(to - fromT, _perimeter);
        return corridor + Mathf.Min(forward, _perimeter - forward);
    }

    Vector3 BuildCorridor(GridTarget target, List<Vector3> corridor)
    {
        FaceNeighbor(target.x, target.y, target.approach, out int nx, out int ny);
        if (!InBounds(nx, ny)) return Holder.TransformPoint(EntryPointLocal(target));

        TraceToBorder(ColonyGridIndex.From(nx, ny, gridX), out int borderIndex, out _);

        for (int i = _chain.Count - 1; i >= 0; i--)
            corridor.Add(CellWorld(ColonyGridIndex.X(_chain[i], gridX), ColonyGridIndex.Y(_chain[i], gridX)));

        return Holder.TransformPoint(ExteriorMouthLocal(ColonyGridIndex.X(borderIndex, gridX), ColonyGridIndex.Y(borderIndex, gridX)));
    }

    Vector3 ExteriorMouthLocal(int bx, int by)
    {
        if (by == gridY - 1) return CellLocal(bx, gridY);
        if (bx == gridX - 1) return CellLocal(gridX, by);
        if (bx == 0) return CellLocal(-1, by);
        return CellLocal(bx, -1);
    }

    Vector3 EntryPointLocal(GridTarget target)
    {
        switch (target.approach)
        {
            case GridApproach.Left: return CellLocal(-1, target.y);
            case GridApproach.Right: return CellLocal(gridX, target.y);
            case GridApproach.Top: return CellLocal(target.x, -1);
            default: return CellLocal(target.x, gridY);
        }
    }

    Vector3 AppendStraightEntry(Vector3 fromWorld, List<Vector3> path)
    {
        ResolveStraight(fromWorld, out Vector3 boundary, out bool hitGate, out Vector3 gateHit, out Vector3 gatePost);

        if (hitGate)
        {
            path.Add(gateHit);
            path.Add(gatePost);
            return gatePost;
        }

        path.Add(boundary);
        return boundary;
    }

    void ResolveStraight(Vector3 fromWorld, out Vector3 boundary,
                         out bool hitGate, out Vector3 gateHit, out Vector3 gatePost)
    {
        Vector3 local = Holder.InverseTransformPoint(fromWorld);
        float x = Mathf.Clamp(local.x, -_halfWidth, _halfWidth);
        float z = local.z > 0f ? _halfDepth : -_halfDepth;
        Vector3 edge = Holder.TransformPoint(new Vector3(x, 0f, z));

        hitGate = false;
        gateHit = gatePost = Vector3.zero;

        if (_gateReady)
        {
            Vector2 from = new Vector2(fromWorld.x, fromWorld.z);
            Vector2 to = new Vector2(edge.x, edge.z);
            Vector2 a = new Vector2(_gateA.x, _gateA.z);
            Vector2 b = new Vector2(_gateB.x, _gateB.z);

            if (SegmentHit(from, to, a, b, out Vector2 hit, out float u))
            {
                hitGate = true;
                gateHit = new Vector3(hit.x, Mathf.Lerp(_gateA.y, _gateB.y, u), hit.y);
                gatePost = (hit - a).sqrMagnitude <= (hit - b).sqrMagnitude ? _gateA : _gateB;
                boundary = gateHit;
                return;
            }
        }

        boundary = edge;
    }

    void AppendPerimeter(Vector3 fromWorld, Vector3 toWorld, List<Vector3> path)
    {
        float hx = _halfWidth;
        float hz = _halfDepth;

        Vector3 fromLocal = Holder.InverseTransformPoint(fromWorld);
        Vector3 toLocal = Holder.InverseTransformPoint(toWorld);

        float from = PerimeterT(fromLocal.x, fromLocal.z, hx, hz);
        float to = PerimeterT(toLocal.x, toLocal.z, hx, hz);

        Vector3 entryLocal = PerimeterPoint(from, hx, hz);
        float offsetX = entryLocal.x - fromLocal.x;
        float offsetZ = entryLocal.z - fromLocal.z;
        if (offsetX * offsetX + offsetZ * offsetZ > 0.0001f)
            path.Add(Holder.TransformPoint(entryLocal));

        float forward = Mathf.Repeat(to - from, _perimeter);
        int step = forward <= _perimeter - forward ? 1 : 3;

        int edge = EdgeOf(from, hx, hz);
        int endEdge = EdgeOf(to, hx, hz);

        for (int i = 0; i < 4 && edge != endEdge; i++)
        {
            int corner = step == 1 ? edge : (edge + 3) % 4;
            path.Add(Holder.TransformPoint(CornerPoint(corner, hx, hz)));
            edge = (edge + step) % 4;
        }
    }

    static bool SegmentHit(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit, out float u)
    {
        hit = Vector2.zero;
        u = 0f;

        Vector2 ab = b - a;
        Vector2 cd = d - c;
        float denominator = ab.x * cd.y - ab.y * cd.x;
        if (Mathf.Abs(denominator) < 1e-6f) return false;

        Vector2 ac = c - a;
        float t = (ac.x * cd.y - ac.y * cd.x) / denominator;
        u = (ac.x * ab.y - ac.y * ab.x) / denominator;
        if (t < 0f || t > 1f || u < 0f || u > 1f) return false;

        hit = a + t * ab;
        return true;
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
}
