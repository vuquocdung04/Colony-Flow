using System.Collections.Generic;
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
    public string LockColorHex { get; private set; }

    GridBottom _board;
    GridTop _grid;
    WaitAreas _waitAreas;
    Tween _move;
    bool _spawning;
    bool _keyRequested;
    float _timer;

    public Transform LockTarget => visual != null ? visual.LockTarget : transform;

    public int Index { get; private set; }

    public LinkedLine Link => visual != null ? visual.linkView : null;

    public bool IsHiddenNow => visual != null && visual.IsHiddenActive;

    public bool IsLowered => State == AnthillState.Sleep || IsHiddenNow;

    public Vector3 Size => visual != null ? visual.Size : Vector3.one;

    void Awake()
    {
        if (visual != null) visual.Init(this);
    }

    public void Bind(GridBottom board, int index)
    {
        _board = board;
        Index = index;
    }

    public void SetIndex(int index) => Index = index;

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

    public void SetHidden(bool value)
    {
        IsHidden = value;
        if (visual != null) visual.SetHidden(value);
    }

    public void SetLocked(bool value, string colorHex = null)
    {
        IsLocked = value;
        if (colorHex != null) LockColorHex = colorHex;

        if (visual == null) return;

        visual.SetLocked(value);
        if (value && !string.IsNullOrEmpty(LockColorHex))
            visual.SetLockColor(ColonyPalette.ToColor(LockColorHex));
    }

    public void RequestKeyUnlock()
    {
        if (_keyRequested || !IsLocked || _grid == null || string.IsNullOrEmpty(LockColorHex)) return;

        _keyRequested = true;
        _grid.RequestKey(LockColorHex, LockTarget, OnKeyArrived);
    }

    void OnKeyArrived()
    {
        if (_board != null) _board.ConsumeLock(this);
        else Destroy(gameObject);
    }

    public void SetRow(int row, bool instant = false) =>
        SetState(row == 0 ? AnthillState.Wait : AnthillState.Sleep, instant);

    public void SetState(AnthillState next, bool instant = false)
    {
        bool changed = State != next;
        if (!changed && !instant) return;

        State = next;

        if (changed && !instant && next == AnthillState.Wait)
        {
            if (visual != null) visual.OnReachRow0();
            if (IsLocked) RequestKeyUnlock();
        }

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

        if (visual != null) _move.OnUpdate(visual.RefreshLinks);
        if (onComplete != null) _move.OnComplete(onComplete);
    }

    public void Connect(Anthill partner, bool owner)
    {
        if (Link != null && partner != null) Link.Setup(partner, owner);
    }

    public bool TrySelect()
    {
        if (IsTaken || IsLocked || _waitAreas == null) return false;

        LinkGroup group = Link != null ? Link.group : null;
        if (group != null && group.Count > 1) return TrySelectGroup(group);

        if (State != AnthillState.Wait) return false;
        if (!_waitAreas.TryPlace(this, out Vector3 slotPosition)) return false;

        IsTaken = true;
        if (_board != null) _board.Release(this);

        MoveTo(slotPosition, false, BeginSpawn);
        return true;
    }

    bool TrySelectGroup(LinkGroup group)
    {
        int gridX = _board != null ? _board.gridX : 1;
        if (!group.CanClick(this, gridX)) return false;
        if (_waitAreas.FreeSlots < group.Count) return false;

        List<Anthill> ordered = new List<Anthill>(group.members);
        ordered.Sort((a, b) =>
        {
            int ax = ColonyGridIndex.X(a.Index, gridX);
            int bx = ColonyGridIndex.X(b.Index, gridX);
            return ax != bx ? ax.CompareTo(bx) : a.Index.CompareTo(b.Index);
        });

        foreach (Anthill m in ordered)
        {
            if (m == null || m.IsTaken) continue;
            if (!_waitAreas.TryPlace(m, out Vector3 pos)) continue;

            m.IsTaken = true;
            m.MoveTo(pos, false, m.BeginSpawn);
        }

        if (_board != null) _board.ReleaseGroup(group.members);
        return true;
    }

    void BeginSpawn() => _spawning = true;

    public bool CanDestroy()
    {
        if (Capacity > 0) return false;

        LinkGroup group = Link != null ? Link.group : null;
        return group == null || group.AllEmpty();
    }

    void TryDestroy()
    {
        if (!CanDestroy()) return;

        LinkGroup group = Link != null ? Link.group : null;
        if (group == null)
        {
            OnEmpty();
            return;
        }

        foreach (Anthill member in group.members)
            if (member != null) member.OnEmpty();
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
        TryDestroy();
    }

    void RefreshLabel()
    {
        if (visual != null) visual.SetCapacity(Capacity);
    }
}
