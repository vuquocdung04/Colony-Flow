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

    public float moveDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;

    Anthill _anthill;
    MaterialPropertyBlock _block;
    Tween _move;

    public Transform Root => visual != null ? visual : transform;

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
    }

    public Vector3 StateLocalPosition(AnthillState state) =>
        state == AnthillState.Wait ? WaitLocalPosition : SleepLocalPosition;

    public void ApplyState(AnthillState state, bool instant = false)
    {
        _move?.Kill();

        if (instant)
        {
            Root.localPosition = StateLocalPosition(state);
            return;
        }

        _move = Root.DOLocalMove(StateLocalPosition(state), moveDuration)
                    .SetEase(moveEase)
                    .SetLink(gameObject);
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
