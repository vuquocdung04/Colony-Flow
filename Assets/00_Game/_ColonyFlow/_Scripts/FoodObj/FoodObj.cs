using DG.Tweening;
using EventDispatcher;
using UnityEngine;

public class FoodObj : MonoBehaviour
{
    [Header("Refs")]
    public MeshRenderer meshRenderer;
    public Material hiddenMaterial;

    [Space, Header("Fx - lộ ra khỏi vùng ẩn")]
    public ParticleSystem revealFx;

    [Space, Header("Carry - kiến cắp đi")]
    public float carryRotationZ = -140f;

    [Space, Header("Clear - booster hút vào")]
    public Ease clearEase = Ease.InQuad;
    [Range(0f, 1f)] public float shrinkPortion = 0.35f;
    public float spinAngle = 540f;
    public float clearLift = 1f;
    [Range(0f, 1f)] public float liftPortion = 0.25f;
    public Ease liftEase = Ease.OutQuad;

    const float ShrinkMultiplier = 0.5f;

    Material _realMaterial;
    Color _color = Color.white;
    bool _hasColor;
    bool _hidden;

    public bool IsHidden => _hidden;

    public string ColorHex { get; private set; }

    public Vector3 Size
    {
        get
        {
            if (meshRenderer == null) return Vector3.one;
            Vector3 size = meshRenderer.bounds.size;
            return new Vector3(
                size.x > Mathf.Epsilon ? size.x : 1f,
                size.y,
                size.z > Mathf.Epsilon ? size.z : 1f);
        }
    }

    public void SetColor(Color color)
    {
        _color = color;
        _hasColor = true;
        if (_hidden) return;
        ApplyColor();
    }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetHidden(bool value)
    {
        bool wasHidden = _hidden;
        _hidden = value;

        if (meshRenderer != null)
        {
            if (value)
            {
                if (hiddenMaterial != null)
                {
                    if (_realMaterial == null) _realMaterial = meshRenderer.sharedMaterial;
                    meshRenderer.sharedMaterial = hiddenMaterial;
                    ClearBlock();
                }
            }
            else
            {
                if (_realMaterial != null) meshRenderer.sharedMaterial = _realMaterial;
                if (_hasColor) ApplyColor();
            }
        }

        if (wasHidden && !value) ColonyFx.Play(revealFx);
    }

    public void Reveal() => SetHidden(false);

    void ApplyColor() => ColonyPalette.Tint(meshRenderer, _color);

    void ClearBlock() => ColonyPalette.ClearTint(meshRenderer);

    public void Clear(Vector3 center, float delay, float duration)
    {
        NotifyDestroyed();

        if (!gameObject.activeInHierarchy || duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null, true);

        Vector3 spin = Random.onUnitSphere * spinAngle;
        Vector3 shrinkScale = transform.localScale * ShrinkMultiplier;
        float shrink = duration * shrinkPortion;
        float lift = clearLift > 0f ? duration * liftPortion : 0f;

        Sequence sequence = DOTween.Sequence().SetLink(gameObject);

        if (lift > 0f)
        {
            float liftY = transform.position.y + clearLift;
            sequence.Insert(delay, transform.DOMoveY(liftY, lift).SetEase(liftEase));
        }

        sequence.Insert(delay + lift, transform.DOMove(center, duration - lift).SetEase(clearEase));
        sequence.Insert(delay, transform.DORotate(spin, duration, RotateMode.LocalAxisAdd).SetEase(Ease.Linear));
        sequence.Insert(delay + duration - shrink, transform.DOScale(shrinkScale, shrink).SetEase(Ease.InQuad));

        sequence.OnComplete(() => Destroy(gameObject));
    }

    void NotifyDestroyed()
    {
        if (!string.IsNullOrEmpty(ColorHex)) this.PostEvent(EventID.BLOCK_DESTROYED, ColorHex);
    }

    public void Collect(Ant ant)
    {
        NotifyDestroyed();

        Transform holder = ant != null ? ant.foodHolder : null;
        if (holder == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(holder, true);
        transform.DOLocalRotate(new Vector3(0f, 0f, carryRotationZ), ant.holdDuration)
                 .SetLink(gameObject);
    }
}
