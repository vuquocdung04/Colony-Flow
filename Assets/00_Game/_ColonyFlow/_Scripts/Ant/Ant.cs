using System;
using System.Collections.Generic;
using UnityEngine;

public enum AntState
{
    FindFood,
    Hold,
    FindHole,
}

public class Ant : MonoBehaviour
{
    public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    public Transform foodHolder;

    public float moveSpeed = 3f;
    public float turnSpeed = 540f;
    public float arriveDistance = 0.05f;
    public float holdDuration = 0.4f;

    public event Action<Ant, AntState> StateChanged;

    public AntState State { get; private set; } = AntState.FindFood;
    public string ColorHex { get; private set; }

    MaterialPropertyBlock _block;
    GridTop _grid;
    GridTarget _target;

    readonly List<Vector3> _path = new List<Vector3>();
    int _waypoint;
    float _holdTimer;

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

        _block ??= new MaterialPropertyBlock();
        foreach (MeshRenderer target in meshRenderers)
            ColonyPalette.Tint(target, _block, color);
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
        _grid.BuildApproachPath(transform.position, _target, _path);
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
                if (FollowPath()) Destroy(gameObject);
                break;
        }
    }

    void OnReachFood()
    {
        _grid.Collect(_target.index, this);
        _holdTimer = holdDuration;
        SetState(AntState.Hold);
    }

    void OnHoldFinished()
    {
        _path.Clear();
        _waypoint = 0;
        _grid.BuildExitPath(_target, _grid.HolePosition, _path);
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

        if (MoveTowards(_path[_waypoint])) _waypoint++;
        return _waypoint >= _path.Count;
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
            Gizmos.DrawLine(previous, _path[i]);
            Gizmos.DrawWireSphere(_path[i], 0.1f);
            previous = _path[i];
        }
    }
}
