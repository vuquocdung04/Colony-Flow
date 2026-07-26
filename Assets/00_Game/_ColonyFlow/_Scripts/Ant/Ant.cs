using System;
using System.Collections.Generic;
using UnityEngine;

public enum AntState
{
    FindFood,
    Hold,
    FindHole,
    EnterHole,
}

public class Ant : MonoBehaviour
{
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    public Transform foodHolder;
    public AntAnimation anim;

    public float moveSpeed = 3f;
    public float turnSpeed = 540f;
    public float arriveDistance = 0.05f;
    public float holdDuration = 0.4f;
    public float spread = 0.12f;
    public float holeEnterDistance = 0.5f;

    public event Action<Ant, AntState> StateChanged;

    public AntState State { get; private set; } = AntState.FindFood;
    public string ColorHex { get; private set; }

    GridTop _grid;
    GridTarget _target;

    readonly List<Vector3> _path = new List<Vector3>();
    readonly List<Vector3> _returnPath = new List<Vector3>();
    int _waypoint;
    int _mouthIndex;
    float _holdTimer;

    float _spreadOffset;
    int _spreadFrom;
    int _spreadTo;
    Vector3 _pathStart;

    Vector3 _baseEuler;
    float _yaw;

    public void SetMaterial(Material material)
    {
        if (material == null) return;

        foreach (MeshRenderer target in meshRenderers)
            if (target != null) target.sharedMaterial = material;
    }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        Color color = ColonyPalette.ToColor(hex);

        foreach (MeshRenderer target in meshRenderers)
            ColonyPalette.Tint(target, color);
    }

    public void Init(GridTop grid, GridTarget target)
    {
        _grid = grid;
        _target = target;

        _baseEuler = transform.eulerAngles;
        _yaw = _baseEuler.y;

        if (_grid == null || !_target.IsValid)
        {
            Debug.LogWarning("[Ant] Init thiếu grid hoặc target.", this);
            Destroy(gameObject);
            return;
        }

        _path.Clear();
        _waypoint = 0;
        _pathStart = transform.position;
        _spreadOffset = UnityEngine.Random.Range(-spread, spread);
        _mouthIndex = _grid.BuildApproachPath(transform.position, _target, _path);
        SetSpreadRange(0, _mouthIndex);
        SetState(AntState.FindFood);
    }

    void Update()
    {
        switch (State)
        {
            case AntState.FindFood:
                if (FollowPath()) OnReachFood();
                break;

            case AntState.Hold:
                _holdTimer -= Time.deltaTime;
                if (_holdTimer <= 0f) OnHoldFinished();
                break;

            case AntState.FindHole:
                if (NearHole() || FollowPath()) EnterHole();
                break;
        }
    }

    bool NearHole()
    {
        if (holeEnterDistance <= 0f || _waypoint < _path.Count - 1) return false;

        Vector3 hole = _grid.HolePosition;
        Vector3 position = transform.position;
        float dx = hole.x - position.x;
        float dz = hole.z - position.z;
        return dx * dx + dz * dz <= holeEnterDistance * holeEnterDistance;
    }

    void EnterHole()
    {
        SetState(AntState.EnterHole);

        if (anim == null)
        {
            Destroy(gameObject);
            return;
        }

        anim.PlayEnterHole(_grid.HolePosition, () => Destroy(gameObject));
    }

    void OnReachFood()
    {
        _grid.Collect(_target.index, this);
        _holdTimer = holdDuration;
        SetState(AntState.Hold);
    }

    void OnHoldFinished()
    {
        _returnPath.Clear();
        _grid.BuildReturnPath(_path, _mouthIndex, _grid.HolePosition, _returnPath);

        int corridorCount = _path.Count - 1 - _mouthIndex;

        _path.Clear();
        _path.AddRange(_returnPath);
        _returnPath.Clear();
        _waypoint = 0;
        _pathStart = transform.position;

        SetSpreadRange(corridorCount, _path.Count - 1);
        SetState(AntState.FindHole);
    }

    void SetState(AntState next)
    {
        State = next;
        StateChanged?.Invoke(this, next);
    }

    bool FollowPath()
    {
        if (_waypoint >= _path.Count) return true;

        if (MoveTowards(Waypoint(_waypoint))) _waypoint++;
        return _waypoint >= _path.Count;
    }

    void SetSpreadRange(int from, int to)
    {
        _spreadFrom = Mathf.Max(0, from);
        _spreadTo = Mathf.Min(to, _path.Count);
    }

    Vector3 Waypoint(int index)
    {
        Vector3 point = _path[index];
        if (index < _spreadFrom || index >= _spreadTo) return point;

        Vector3 previous = index > 0 ? _path[index - 1] : _pathStart;
        Vector3 direction = new Vector3(point.x - previous.x, 0f, point.z - previous.z);
        if (direction.sqrMagnitude <= 0.0001f) return point;

        direction.Normalize();
        return point + new Vector3(direction.z, 0f, -direction.x) * _spreadOffset;
    }

    bool MoveTowards(Vector3 target)
    {
        Vector3 position = transform.position;
        Vector3 flatPosition = new Vector3(position.x, 0f, position.z);
        Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
        Vector3 offset = flatTarget - flatPosition;

        if (offset.sqrMagnitude <= arriveDistance * arriveDistance) return true;

        float targetYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(_baseEuler.x, _yaw, _baseEuler.z);

        Vector3 next = Vector3.MoveTowards(flatPosition, flatTarget, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(next.x, position.y, next.z);
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (_path == null || _path.Count == 0) return;

        Gizmos.color = Color.yellow;
        Vector3 previous = transform.position;
        for (int i = _waypoint; i < _path.Count; i++)
        {
            Vector3 point = Waypoint(i);
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawWireSphere(point, 0.1f);
            previous = point;
        }
    }
}
