using UnityEngine;

public class FoodObj : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public MeshRenderer meshRenderer;

    MaterialPropertyBlock _block;

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
        if (meshRenderer == null) return;

        _block ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(_block);
    }

    public void SetColor(string hex) => SetColor(ColonyPalette.ToColor(hex));
}
