using System;
using UnityEngine;

public static class ColonyPalette
{
    public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    static MaterialPropertyBlock _editorBlock;

    public static readonly string[] Hex =
    {
        "#1C2A47",
        "#FDFBFC",
        "#2891E4",
        "#F73468",
        "#F5A623",
        "#F8E71C",
        "#7ED321",
        "#00B39B",
        "#9B51E0",
        "#8B5E3C",
    };

    public static readonly string[] Names =
    {
        "Navy", "White", "Blue", "Pink", "Orange",
        "Yellow", "Green", "Teal", "Purple", "Brown",
    };

    public static int Count => Hex.Length;

    public static string HexAt(int index) => index >= 0 && index < Hex.Length ? Hex[index] : null;

    public static string NameAt(int index) => index >= 0 && index < Names.Length ? Names[index] : "Empty";

    public static int IndexOf(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return -1;
        for (int i = 0; i < Hex.Length; i++)
            if (string.Equals(Hex[i], hex, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    public static Color ToColor(string hex)
    {
        if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color color)) return color;
        return Color.magenta;
    }

    public static string ToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

    public static void Tint(Renderer target, Color color) => Tint(target, color, -1);

    public static void Tint(Renderer target, Color color, int submesh)
    {
        if (target == null) return;

        if (!Application.isPlaying)
        {
            EditorTint(target, color, submesh);
            return;
        }

        Material material = submesh < 0 ? target.material : SubMaterial(target, submesh);
        if (material != null) material.SetColor(BaseColorId, color);
    }

    public static void ClearTint(Renderer target)
    {
        if (target == null || Application.isPlaying) return;

        _editorBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(_editorBlock);
        _editorBlock.Clear();
        target.SetPropertyBlock(_editorBlock);
    }

    static Material SubMaterial(Renderer target, int submesh)
    {
        Material[] materials = target.materials;
        return submesh >= 0 && submesh < materials.Length ? materials[submesh] : null;
    }

    static void EditorTint(Renderer target, Color color, int submesh)
    {
        _editorBlock ??= new MaterialPropertyBlock();

        if (submesh < 0)
        {
            target.GetPropertyBlock(_editorBlock);
            _editorBlock.SetColor(BaseColorId, color);
            target.SetPropertyBlock(_editorBlock);
            return;
        }

        target.GetPropertyBlock(_editorBlock, submesh);
        _editorBlock.SetColor(BaseColorId, color);
        target.SetPropertyBlock(_editorBlock, submesh);
    }
}
