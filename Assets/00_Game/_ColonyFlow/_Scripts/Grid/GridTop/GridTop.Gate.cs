using UnityEngine;

public partial class GridTop
{
    [System.NonSerialized] bool _gateReady;
    [System.NonSerialized] Vector3 _gateA, _gateB, _gateBaseA, _gateBaseB;
    [System.NonSerialized] bool _holeHorizontal = true;
    [System.NonSerialized] int _holeRing = -BorderRing;

    public bool TryGetHoleEdge(out bool horizontal, out int ringFixed)
    {
        EnsureLayout();

        horizontal = _holeHorizontal;
        ringFixed = _holeRing;
        return _gateReady;
    }

    public bool TryGetEntranceGate(out Vector3 pointA, out Vector3 pointB)
    {
        EnsureLayout();

        pointA = _gateA;
        pointB = _gateB;
        return _gateReady;
    }

    public bool TryGetEntranceGate(out Vector3 pointA, out Vector3 pointB, out Vector3 baseA, out Vector3 baseB)
    {
        EnsureLayout();

        pointA = _gateA;
        pointB = _gateB;
        baseA = _gateBaseA;
        baseB = _gateBaseB;
        return _gateReady;
    }

    void RefreshGate()
    {
        _gateReady = false;
        _gateA = _gateB = _gateBaseA = _gateBaseB = Vector3.zero;
        _holeHorizontal = true;
        _holeRing = -BorderRing;

        if (hole == null) return;

        Vector3 local = Holder.InverseTransformPoint(hole.position);
        local.x += entranceOffsetX * _stepX;

        float ringZ = local.z > 0f ? _halfDepth : -_halfDepth;
        float z = ringZ + entranceOffsetZ * _stepZ;
        float half = (entranceSpread + 0.5f) * _stepX;
        float x = Mathf.Clamp(local.x, -_halfWidth, _halfWidth);

        _gateA = Holder.TransformPoint(new Vector3(x - half, 0f, z));
        _gateB = Holder.TransformPoint(new Vector3(x + half, 0f, z));
        _gateBaseA = Holder.TransformPoint(new Vector3(Mathf.Clamp(x - half, -_halfWidth, _halfWidth), 0f, ringZ));
        _gateBaseB = Holder.TransformPoint(new Vector3(Mathf.Clamp(x + half, -_halfWidth, _halfWidth), 0f, ringZ));

        GridApproach side = ResolveSide(local);
        _holeHorizontal = side == GridApproach.Top || side == GridApproach.Bottom;

        if (side == GridApproach.Bottom) _holeRing = gridY - 1 + BorderRing;
        else if (side == GridApproach.Right) _holeRing = gridX - 1 + BorderRing;
        else _holeRing = -BorderRing;

        _gateReady = true;
    }

    GridApproach ResolveSide(Vector3 local)
    {
        float fcol = (local.x - _originX) / _stepX;
        float frow = (_originZ - local.z) / _stepZ;

        int maxX = gridX - 1;
        int maxY = gridY - 1;

        float outLeft = -fcol;
        float outRight = fcol - maxX;
        float outTop = -frow;
        float outBottom = frow - maxY;
        float outside = Mathf.Max(Mathf.Max(outLeft, outRight), Mathf.Max(outTop, outBottom));

        if (outside > 0f)
        {
            if (outside == outTop) return GridApproach.Top;
            if (outside == outBottom) return GridApproach.Bottom;
            if (outside == outLeft) return GridApproach.Left;
            return GridApproach.Right;
        }

        float toLeft = fcol, toRight = maxX - fcol, toTop = frow, toBottom = maxY - frow;
        float nearest = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toTop, toBottom));

        if (nearest == toTop) return GridApproach.Top;
        if (nearest == toBottom) return GridApproach.Bottom;
        if (nearest == toLeft) return GridApproach.Left;
        return GridApproach.Right;
    }
}
