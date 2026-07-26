using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AntAnimation : MonoBehaviour
{
    public List<Transform> legsLeft = new List<Transform>();
    public List<Transform> legsRight = new List<Transform>();

    public float legAngle = 10f;
    public float legDuration = 0.18f;

    public float jumpPower = 0.5f;
    public float jumpDuration = 0.35f;

    [Range(0f, 1f)] public float scaleDelayRatio = 0.4f;

    Quaternion[] _baseLeft;
    Quaternion[] _baseRight;
    Tween _walk;

    void Awake()
    {
        _baseLeft = Capture(legsLeft);
        _baseRight = Capture(legsRight);
    }

    void OnEnable() => PlayWalk();

    void OnDisable() => StopWalk();

    public void PlayWalk()
    {
        _walk?.Kill();
        _walk = DOVirtual.Float(legAngle, -legAngle, legDuration, ApplyLegs)
                         .SetEase(Ease.InOutSine)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetLink(gameObject);
    }

    public void StopWalk()
    {
        _walk?.Kill();
        _walk = null;
        ApplyLegs(0f);
    }

    public void PlayEnterHole(Vector3 center, Action onComplete)
    {
        StopWalk();

        float delay = jumpDuration * Mathf.Clamp01(scaleDelayRatio);
        float scaleDuration = Mathf.Max(0.01f, jumpDuration - delay);

        DOTween.Sequence()
               .Append(transform.DOJump(center, jumpPower, 1, jumpDuration))
               .Insert(delay, transform.DOScale(0f, scaleDuration).SetEase(Ease.InQuad))
               .OnComplete(() => onComplete?.Invoke())
               .SetLink(gameObject);
    }

    void ApplyLegs(float angle)
    {
        Swing(legsRight, _baseRight, angle);
        Swing(legsLeft, _baseLeft, -angle);
    }

    static void Swing(List<Transform> legs, Quaternion[] bases, float angle)
    {
        if (bases == null) return;

        Quaternion swing = Quaternion.Euler(0f, angle, 0f);
        for (int i = 0; i < legs.Count && i < bases.Length; i++)
        {
            Transform leg = legs[i];
            if (leg != null) leg.localRotation = bases[i] * swing;
        }
    }

    static Quaternion[] Capture(List<Transform> legs)
    {
        Quaternion[] result = new Quaternion[legs.Count];
        for (int i = 0; i < legs.Count; i++)
            result[i] = legs[i] != null ? legs[i].localRotation : Quaternion.identity;

        return result;
    }
}
