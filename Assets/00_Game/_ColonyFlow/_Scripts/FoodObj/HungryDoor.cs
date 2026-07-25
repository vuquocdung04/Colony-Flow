using System;
using UnityEngine;

public class HungryDoor : BlockerBase
{
    public MeshRenderer meshRenderer;
    public int materialIndex;

    public string ColorHex { get; private set; }

    MaterialPropertyBlock _block;

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetColor(Color color)
    {
        if (meshRenderer == null) return;

        int slot = Mathf.Clamp(materialIndex, 0, meshRenderer.sharedMaterials.Length - 1);

        _block ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_block, slot);
        _block.SetColor(ColonyPalette.BaseColorId, color);
        meshRenderer.SetPropertyBlock(_block, slot);
    }

    protected override bool Accepts(object param) =>
        !string.IsNullOrEmpty(ColorHex) &&
        param is string hex &&
        string.Equals(hex, ColorHex, StringComparison.OrdinalIgnoreCase);
}
