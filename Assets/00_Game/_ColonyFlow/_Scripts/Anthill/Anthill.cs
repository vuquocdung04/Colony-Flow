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
    [Header("Refs")]
    public AnthillVisual visual;

    [Space, Header("Spawn")]
    public Ant antPrefab;
    public Transform spawnPoint;
    public float spawnInterval = 0.35f;

    [Space, Header("Move")]
    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;

    public AnthillState State { get; private set; } = AnthillState.Sleep;

    public string ColorHex { get; private set; }
    public int Capacity => _spawner != null ? _spawner.Remaining : 0;
    public bool IsTaken { get; private set; }

    public bool IsHidden { get; private set; }
    public bool HasRope { get; private set; }
    public bool IsLocked { get; private set; }
    public string LockColorHex { get; private set; }

    GridBottom _board;
    GridTop _grid;
    WaitAreas _waitAreas;
    AntSpawner _spawner;
    Tween _move;
    bool _keyRequested;
    bool _emptyStarted;

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
        _spawner ??= new AntSpawner(antPrefab, spawnPoint, transform, spawnInterval,
                                    RefreshLabel, TryDestroy);
        SetColor(hex);
        _spawner.Setup(grid, hex, capacity);
    }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        _spawner?.SetColor(hex);
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

        if (changed && !instant && next == AnthillState.Wait) ReachRow0();

        if (visual != null) visual.ApplyState(next, instant);
    }

    public void ReachRow0()
    {
        if (visual != null) visual.OnReachRow0();
        if (IsLocked) RequestKeyUnlock();
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

    public void MoveToSlot(Vector3 slotPosition) => MoveTo(slotPosition, false, BeginSpawn);

    public void Connect(Anthill partner, bool owner)
    {
        if (Link != null && partner != null) Link.Setup(partner, owner);
    }

    public bool TrySelect()
    {
        if (IsTaken || _waitAreas == null) return false;
        if (IsLocked) return Reject();

        LinkGroup group = Link != null ? Link.group : null;
        if (group != null && group.Count > 1) return TrySelectGroup(group);

        if (State != AnthillState.Wait) return Reject();
        if (!_waitAreas.TryPlace(this, out Vector3 slotPosition)) return Reject();

        IsTaken = true;
        if (_board != null) _board.Release(this);

        MoveToSlot(slotPosition);
        return true;
    }

    bool TrySelectGroup(LinkGroup group)
    {
        int gridX = _board != null ? _board.gridX : 1;
        if (!group.CanClick(this, gridX)) return Reject();
        if (_waitAreas.FreeSlots < group.Count) return Reject();

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
            m.MoveToSlot(pos);
        }

        if (_board != null) _board.ReleaseGroup(group.members);
        return true;
    }

    bool Reject()
    {
        PlayWobble();
        return false;
    }

    public void PlayWobble()
    {
        if (visual != null) visual.PlayWobble();
    }

    void BeginSpawn() => _spawner?.Begin();

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
            PlayEmpty();
            return;
        }

        PlayGroupEmpty(group);
    }

    public void PlayEmpty()
    {
        if (!BeginEmpty()) return;

        if (visual != null) visual.PlayEmpty(Remove);
        else Remove();
    }

    void PlayGroupEmpty(LinkGroup group)
    {
        List<Anthill> members = group.members;

        foreach (Anthill member in members)
            if (member != null) member.BeginEmpty();

        if (visual != null) visual.PlayGroupEmpty(members, () => RemoveAll(members));
        else RemoveAll(members);
    }

    bool BeginEmpty()
    {
        if (_emptyStarted) return false;

        _emptyStarted = true;
        _move?.Kill();
        ReleaseSlot();
        return true;
    }

    static void RemoveAll(List<Anthill> members)
    {
        foreach (Anthill member in members)
            if (member != null) member.Remove();
    }

    void Remove() => Destroy(gameObject);

    void ReleaseSlot()
    {
        if (_waitAreas != null) _waitAreas.Release(this);
    }

    public void Tick(float delta) => _spawner?.Tick(delta);

    void RefreshLabel(int capacity)
    {
        if (visual != null) visual.SetCapacity(capacity);
    }
}
