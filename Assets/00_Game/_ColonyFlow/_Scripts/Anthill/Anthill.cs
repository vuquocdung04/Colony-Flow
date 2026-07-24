using DG.Tweening;
using UnityEngine;

public enum AnthillState
{
    Wait,
    Sleep,
}

public class Anthill : MonoBehaviour
{
    public AnthillVisual visual;

    public Ant antPrefab;
    public Transform spawnPoint;

    public float spawnInterval = 0.35f;
    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;

    public AnthillState State { get; private set; } = AnthillState.Sleep;

    public string ColorHex { get; private set; }
    public int Capacity { get; private set; }
    public bool IsTaken { get; private set; }

    public bool IsHidden { get; private set; }
    public bool HasRope { get; private set; }
    public bool IsLocked { get; private set; }

    GridBottom _board;
    GridTop _grid;
    WaitAreas _waitAreas;
    Tween _move;
    bool _spawning;
    float _timer;

    public Vector3 Size => visual != null ? visual.Size : Vector3.one;

    void Awake()
    {
        if (visual != null) visual.Init(this);
    }

    public void Bind(GridBottom board) => _board = board;

    public void Setup(string hex, int capacity, GridTop grid, WaitAreas areas)
    {
        _grid = grid;
        _waitAreas = areas;
        Capacity = Mathf.Max(0, capacity);
        SetColor(hex);
        RefreshLabel();
    }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        if (visual != null) visual.SetColor(hex);
    }

    public void SetRow(int row, bool instant = false) =>
        SetState(row == 0 ? AnthillState.Wait : AnthillState.Sleep, instant);

    public void SetState(AnthillState next, bool instant = false)
    {
        if (State == next && !instant) return;

        State = next;
        if (visual != null) visual.ApplyState(next, instant);
    }

    public void MoveTo(Vector3 target, bool instant = false, TweenCallback onComplete = null)
    {
        _move?.Kill();

        if (instant)
        {
            transform.position = target;
            onComplete?.Invoke();
            return;
        }

        _move = transform.DOMove(target, moveDuration)
                         .SetEase(moveEase)
                         .SetLink(gameObject);

        if (onComplete != null) _move.OnComplete(onComplete);
    }

    public bool TrySelect()
    {
        if (IsTaken || State != AnthillState.Wait || _waitAreas == null) return false;
        if (!_waitAreas.TryPlace(this, out Vector3 slotPosition)) return false;

        IsTaken = true;
        if (_board != null) _board.Release(this);

        MoveTo(slotPosition, false, () => _spawning = true);
        return true;
    }

    public void OnEmpty()
    {
        if (_waitAreas != null) _waitAreas.Release(this);
        Destroy(gameObject);
    }

    public void Tick(float delta)
    {
        if (!_spawning || Capacity <= 0 || _grid == null || antPrefab == null) return;

        _timer -= delta;
        if (_timer > 0f) return;
        _timer = spawnInterval;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        if (!_grid.TryReserve(ColorHex, position, out GridTarget target)) return;

        Ant ant = Instantiate(antPrefab, position, antPrefab.transform.rotation);
        ant.SetColor(ColorHex);
        ant.Init(_grid, target);

        Capacity--;
        RefreshLabel();

        if (Capacity > 0) return;

        _spawning = false;
        OnEmpty();
    }

    void RefreshLabel()
    {
        if (visual != null) visual.SetCapacity(Capacity);
    }
}
