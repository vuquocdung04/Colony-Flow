using System;
using DG.Tweening;
using UnityEngine;

public class HungryDoor : BlockerBase
{
    [Header("Refs")]
    public MeshRenderer meshRenderer;
    public int materialIndex;

    [Space, Header("Refs - Empty anim")]
    public Transform doorLeft;
    public Transform doorRight;
    public Transform lockTransform;

    [Space, Header("1. Nhấc lên")]
    public float riseY = 0.5f;
    public float riseDuration = 0.25f;

    [Space, Header("2. Ổ khoá phình rồi biến mất")]
    public float lockScale = 1.3f;
    public float lockDuration = 0.2f;

    [Space, Header("3. Hai cánh bung ra")]
    public float doorAngleLeft = 115f;
    public float doorAngleRight = -115f;
    public float doorDuration = 0.3f;

    [Space, Header("4. Bay lên, phình, co về 0")]
    public float openRiseY = 1f;
    public float openScale = 1.5f;
    public float openDuration = 0.25f;
    public float shrinkDuration = 0.15f;

    public string ColorHex { get; private set; }

    Tween _empty;

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetColor(Color color)
    {
        if (meshRenderer == null) return;

        int slot = Mathf.Clamp(materialIndex, 0, meshRenderer.sharedMaterials.Length - 1);
        ColonyPalette.Tint(meshRenderer, color, slot);
    }

    protected override bool Accepts(object param) =>
        !string.IsNullOrEmpty(ColorHex) &&
        param is string hex &&
        string.Equals(hex, ColorHex, StringComparison.OrdinalIgnoreCase);

    protected override void PlayEmpty(Action onComplete)
    {
        _empty?.Kill();

        Transform root = transform;
        float baseY = root.localPosition.y;
        Vector3 baseScale = root.localScale;

        Sequence sequence = DOTween.Sequence()
                                   .Append(root.DOLocalMoveY(baseY + riseY, riseDuration)
                                               .SetEase(Ease.OutQuad));

        if (lockTransform != null)
        {
            GameObject lockObject = lockTransform.gameObject;

            sequence.Append(lockTransform.DOScale(lockTransform.localScale * lockScale, lockDuration)
                                         .SetEase(Ease.OutBack))
                    .AppendCallback(() => Destroy(lockObject));
        }

        Tween right = Swing(doorRight, doorAngleRight);
        Tween left = Swing(doorLeft, doorAngleLeft);

        if (right != null) sequence.Append(right);

        if (left != null)
        {
            if (right != null) sequence.Join(left);
            else sequence.Append(left);
        }

        _empty = sequence.Append(root.DOLocalMoveY(baseY + riseY + openRiseY, openDuration)
                                     .SetEase(Ease.OutQuad))
                         .Join(root.DOScale(baseScale * openScale, openDuration).SetEase(Ease.OutBack))
                         .Append(root.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack))
                         .OnComplete(() => onComplete?.Invoke())
                         .SetLink(gameObject);
    }

    Tween Swing(Transform door, float angle)
    {
        if (door == null) return null;

        Vector3 euler = door.localEulerAngles;
        return door.DOLocalRotate(new Vector3(euler.x, euler.y, angle), doorDuration).SetEase(Ease.OutBack);
    }
}
