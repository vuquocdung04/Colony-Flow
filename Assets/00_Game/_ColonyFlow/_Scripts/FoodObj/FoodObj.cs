using DG.Tweening;
using UnityEngine;

public class FoodObj : MonoBehaviour
{
    public float carryRotationZ = -140f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public MeshRenderer meshRenderer;
    public Material hiddenMaterial;

    MaterialPropertyBlock _block;
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
        _hidden = value;
        if (meshRenderer == null) return;

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

    public void Reveal() => SetHidden(false);

    void ApplyColor()
    {
        if (meshRenderer == null) return;

        _block ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, _color);
        meshRenderer.SetPropertyBlock(_block);
    }

    void ClearBlock()
    {
        if (meshRenderer == null) return;

        _block ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_block);
        _block.Clear();
        meshRenderer.SetPropertyBlock(_block);
    }

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
