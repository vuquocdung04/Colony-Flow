using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class AnthillVisual : MonoBehaviour
{
    public Transform visual;
    public Transform waitPoint;
    public Transform sleepPoint;

    public MeshRenderer body;
    public MeshRenderer lid;
    public TMP_Text capacityLabel;

    public AnthillHidden hidden;
    public AnthillLock lockView;
    public LinkedLine linkView;

    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;

    Anthill _anthill;
    MaterialPropertyBlock _block;
    Tween _move;

    public Transform Root => visual != null ? visual : transform;

    public bool IsHiddenActive => hidden != null && hidden.IsActive;

    public Vector3 WaitLocalPosition => LocalPoint(waitPoint);

    public Vector3 SleepLocalPosition => LocalPoint(sleepPoint);

    public Vector3 Size
    {
        get
        {
            if (lid == null) return Vector3.one;

            Vector3 size = lid.bounds.size;
            return new Vector3(
                size.x > Mathf.Epsilon ? size.x : 1f,
                size.y,
                size.z > Mathf.Epsilon ? size.z : 1f);
        }
    }

    public void Init(Anthill owner)
    {
        _anthill = owner;

        if (hidden != null) hidden.Init(owner, this);
        if (lockView != null) lockView.Init(owner, this);
        if (linkView != null) linkView.Init(owner);
    }

    public Vector3 StateLocalPosition(AnthillState state) =>
        state == AnthillState.Wait ? WaitLocalPosition : SleepLocalPosition;

    public void ApplyState(AnthillState state, bool instant = false)
    {
        _move?.Kill();

        if (linkView != null) linkView.ApplyState(state == AnthillState.Sleep || IsHiddenActive);

        if (instant)
        {
            Root.localPosition = StateLocalPosition(state);
            RefreshLinks();
            return;
        }

        _move = Root.DOLocalMove(StateLocalPosition(state), moveDuration)
                    .SetEase(moveEase)
                    .SetLink(gameObject)
                    .OnUpdate(RefreshLinks);
    }

    public void RefreshLinks()
    {
        if (linkView != null) linkView.RefreshAllLinks();
    }

    public void SetContentActive(bool value)
    {
        if (visual != null) visual.gameObject.SetActive(value);
    }

    public void SetHidden(bool value)
    {
        if (hidden != null) hidden.SetHidden(value);
    }

    public void SetLocked(bool value)
    {
        if (lockView != null) lockView.SetLocked(value);
    }

    public void SetLockColor(Color color)
    {
        if (lockView != null) lockView.SetColor(color);
    }

    public Transform LockTarget => lockView != null ? lockView.Target : Root;

    public void OnReachRow0()
    {
        if (hidden != null && hidden.TryUnlock(true) && linkView != null) linkView.RefreshAllLooks();
        if (lockView != null) lockView.TryUnlock(false);
    }

    public void SetColor(string hex) => SetColor(ColonyPalette.ToColor(hex));

    public void SetColor(Color color)
    {
        _block ??= new MaterialPropertyBlock();
        ColonyPalette.Tint(body, _block, color);
        ColonyPalette.Tint(lid, _block, color);
    }

    public void SetCapacity(int capacity)
    {
        if (capacityLabel != null) capacityLabel.text = capacity.ToString();
    }

    Vector3 LocalPoint(Transform point)
    {
        if (point == null) return Root.localPosition;
        if (Root.parent == null) return point.position;

        return Root.parent.InverseTransformPoint(point.position);
    }
}
