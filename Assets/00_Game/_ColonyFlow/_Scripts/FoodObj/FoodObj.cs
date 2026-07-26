using DG.Tweening;
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

    Material _realMaterial;
    Color _color = Color.white;
    bool _hasColor;
    bool _hidden;

    public bool IsHidden => _hidden;

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

    public void SetColor(string hex) => SetColor(ColonyPalette.ToColor(hex));

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

    public void Collect(Ant ant)
    {
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
