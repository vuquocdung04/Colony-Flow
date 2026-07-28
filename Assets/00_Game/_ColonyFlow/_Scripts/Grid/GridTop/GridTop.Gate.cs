using UnityEngine;

public partial class GridTop
{
    [System.NonSerialized] bool _gateReady;
    [System.NonSerialized] Vector3 _gateA, _gateB, _gateBaseA, _gateBaseB;
    [System.NonSerialized] Vector3[] _gateArc;
    [System.NonSerialized] bool _holeHorizontal = true;
    [System.NonSerialized] int _holeRing = -BorderRing;

    public Vector3[] GateArc => _gateArc;

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
        _gateArc = null;
        _holeHorizontal = true;
        _holeRing = -BorderRing;

        if (hole == null) return;

        Vector3 local = Holder.InverseTransformPoint(hole.position);

        float ringZ = local.z > 0f ? _halfDepth : -_halfDepth;
        float x = Mathf.Clamp(local.x, -_halfWidth, _halfWidth);

        Vector3 right = Holder.right;
        Vector3 forward = Holder.forward;

        Vector3 ring = Holder.TransformPoint(new Vector3(x, 0f, ringZ));
        Vector3 center = ring + right * entranceOffsetX + forward * entranceOffsetZ;
        Vector3 span = right * entranceSpread;

        _gateA = center - span;
        _gateB = center + span;
        _gateBaseA = ring - span;
        _gateBaseB = ring + span;

        BuildGateArc(center, right, forward);

        GridApproach side = ResolveSide(local);
        _holeHorizontal = side == GridApproach.Top || side == GridApproach.Bottom;

        if (side == GridApproach.Bottom) _holeRing = gridY - 1 + BorderRing;
        else if (side == GridApproach.Right) _holeRing = gridX - 1 + BorderRing;
        else _holeRing = -BorderRing;

        _gateReady = true;
    }

    void BuildGateArc(Vector3 center, Vector3 right, Vector3 forward)
    {
        int segments = Mathf.Max(1, entranceSub);
        float half = entranceSpread;

        if (segments == 1 || half <= Mathf.Epsilon || Mathf.Abs(entranceRadius) <= Mathf.Epsilon)
        {
            _gateArc = new[] { _gateA, _gateB };
            return;
        }

        float sign = Mathf.Sign(entranceRadius);
        float radius = Mathf.Max(Mathf.Abs(entranceRadius), half);
        float apothem = Mathf.Sqrt(Mathf.Max(0f, radius * radius - half * half));
        float span = Mathf.Asin(Mathf.Clamp01(half / radius));

        Vector3 pivot = center - forward * (apothem * sign);

        _gateArc = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-span, span, i / (float)segments);
            _gateArc[i] = pivot
                        + right * (radius * Mathf.Sin(angle))
                        + forward * (radius * Mathf.Cos(angle) * sign);
        }

        _gateArc[0] = _gateA;
        _gateArc[segments] = _gateB;
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
