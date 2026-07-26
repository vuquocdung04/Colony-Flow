using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class AnthillVisual : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;
    public Transform waitPoint;
    public Transform sleepPoint;

    [Space, Header("Renderers")]
    public MeshRenderer body;
    public MeshRenderer lid;
    public TMP_Text capacityLabel;

    [Space, Header("Views")]
    public AnthillHidden hidden;
    public AnthillLock lockView;
    public LinkedLine linkView;

    [Space, Header("Move")]
    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;

    [Space, Header("Empty - bay lên rồi co về 0")]
    public float emptyRiseY = 1f;
    public float emptyRiseDuration = 0.25f;
    public float emptyScale = 1.2f;
    [Tooltip("0 = phình cùng lúc bay lên. Bằng emptyRiseDuration = bay lên xong mới phình.")]
    public float emptyPopDelay = 0f;
    public float emptyPopDuration = 0.25f;
    public float emptyHoldDuration = 0.1f;
    public float emptyShrinkDuration = 0.12f;

    [Space, Header("Wobble - click không được")]
    public float wobbleAngle = 12f;
    public float wobbleDuration = 0.4f;
    public int wobbleVibrato = 10;
    public float wobbleElasticity = 1f;

    Anthill _anthill;
    Tween _move;
    Tween _empty;
    Tween _wobble;

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
        ColonyPalette.Tint(body, color);
        ColonyPalette.Tint(lid, color);
    }

    public void PlayWobble()
    {
        if (_empty != null && _empty.IsActive()) return;

        _wobble?.Complete();
        _wobble = transform.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle), wobbleDuration,
                                            wobbleVibrato, wobbleElasticity)
                           .OnUpdate(RefreshLinks)
                           .SetLink(gameObject);
    }

    public void PlayEmpty(Action onComplete)
    {
        _move?.Kill();
        _empty?.Kill();
        _wobble?.Complete();

        Transform root = transform;
        Vector3 scale = root.localScale;

        _empty = DOTween.Sequence()
                        .Append(root.DOLocalMoveY(root.localPosition.y + emptyRiseY, emptyRiseDuration)
                                    .SetEase(Ease.OutQuad))
                        .Insert(emptyPopDelay, root.DOScale(scale * emptyScale, emptyPopDuration)
                                                   .SetEase(Ease.OutBack))
                        .AppendInterval(emptyHoldDuration)
                        .Append(root.DOScale(Vector3.zero, emptyShrinkDuration).SetEase(Ease.InBack))
                        .OnUpdate(RefreshLinks)
                        .OnComplete(() => onComplete?.Invoke())
                        .SetLink(gameObject);
    }

    public void PlayGroupEmpty(List<Anthill> members, Action onComplete)
    {
        int pending = 0;

        foreach (Anthill member in members)
            if (member != null && member.visual != null) pending++;

        if (pending == 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (Anthill member in members)
        {
            if (member == null || member.visual == null) continue;

            member.visual.PlayEmpty(() =>
            {
                pending--;
                if (pending == 0) onComplete?.Invoke();
            });
        }
    }

    public void SetCapacity(int capacity)
    {
        if (capacityLabel == null) return;

        capacityLabel.text = capacity.ToString();
        capacityLabel.gameObject.SetActive(capacity > 0);
    }

    Vector3 LocalPoint(Transform point)
    {
        if (point == null) return Root.localPosition;
        if (Root.parent == null) return point.position;

        return Root.parent.InverseTransformPoint(point.position);
    }
}
