using UnityEngine;

public class Anthill : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public MeshRenderer body;
    public MeshRenderer lid;

    MaterialPropertyBlock _block;

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

    public void SetColor(Color color)
    {
        Apply(body, color);
        Apply(lid, color);
    }

    public void SetColor(string hex) => SetColor(ColonyPalette.ToColor(hex));

    void Apply(MeshRenderer target, Color color)
    {
        if (target == null) return;

        _block ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        target.SetPropertyBlock(_block);
    }
}
