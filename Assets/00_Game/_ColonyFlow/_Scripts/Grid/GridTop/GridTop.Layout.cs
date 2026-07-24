using Sirenix.OdinInspector;
using UnityEngine;

public partial class GridTop
{
    public const int BorderRing = 1;

    [System.NonSerialized] bool _layoutReady;
    [System.NonSerialized] Vector3 _cell = Vector3.one;
    [System.NonSerialized] float _stepX, _stepZ;
    [System.NonSerialized] float _originX, _originZ;
    [System.NonSerialized] float _totalWidth, _totalDepth;
    [System.NonSerialized] float _halfWidth, _halfDepth, _perimeter;
    [System.NonSerialized] Vector3 _holePosition;

    public Vector3 CellSize { get { EnsureLayout(); return _cell; } }

    public Vector3 HolePosition { get { EnsureLayout(); return _holePosition; } }

    public Vector2 PaddedSize
    {
        get
        {
            EnsureLayout();
            return new Vector2(_halfWidth * 2f + _cell.x, _halfDepth * 2f + _cell.z);
        }
    }

    public Vector3 CellCenter(int x, int y)
    {
        EnsureLayout();
        return CellWorld(x, y);
    }

    public void RefreshLayout()
    {
        _cell = foodObj != null ? foodObj.Size : Vector3.one;
        _stepX = _cell.x + spacingX;
        _stepZ = _cell.z + spacingZ;

        _totalWidth = gridX * _cell.x + (gridX - 1) * spacingX;
        _totalDepth = gridY * _cell.z + (gridY - 1) * spacingZ;

        _originX = -_totalWidth * 0.5f + _cell.x * 0.5f;
        _originZ = _totalDepth * 0.5f - _cell.z * 0.5f;

        _halfWidth = _totalWidth * 0.5f + _cell.x * 0.5f + spacingX;
        _halfDepth = _totalDepth * 0.5f + _cell.z * 0.5f + spacingZ;
        _perimeter = 4f * (_halfWidth + _halfDepth);

        _holePosition = hole != null ? hole.position : Holder.position;

        RefreshGate();
        _layoutReady = true;
    }

    [Button(ButtonSizes.Medium), GUIColor(0.45f, 0.85f, 0.5f)]
    public void FitToSquare()
    {
        if (squareBound == null) return;

        RefreshLayout();

        Vector2 padded = PaddedSize;
        if (padded.x <= Mathf.Epsilon || padded.y <= Mathf.Epsilon) return;

        Bounds bounds = squareBound.bounds;
        float scale = Mathf.Min(bounds.size.x / padded.x, bounds.size.z / padded.y);
        if (scale <= Mathf.Epsilon) return;

        Transform target = Holder;
        Transform parent = target.parent;
        float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
        if (parentScale > Mathf.Epsilon) scale /= parentScale;

        target.localScale = new Vector3(scale, scale, scale);

        if (alignToSquareCenter)
            target.position = new Vector3(bounds.center.x, target.position.y, bounds.center.z);

        RefreshLayout();
    }

    void EnsureLayout()
    {
        if (!_layoutReady) RefreshLayout();
    }

    void OnValidate() => _layoutReady = false;

    Vector3 CellLocal(int x, int y) => new Vector3(_originX + x * _stepX, 0f, _originZ - y * _stepZ);

    Vector3 CellWorld(int x, int y) => Holder.TransformPoint(CellLocal(x, y));

    bool InBounds(int x, int y) =>
        _colors != null && x >= 0 && y >= 0 && x < gridX && y < gridY;
}
