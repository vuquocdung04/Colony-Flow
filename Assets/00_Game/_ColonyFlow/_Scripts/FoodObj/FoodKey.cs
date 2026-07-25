using DG.Tweening;
using UnityEngine;

public class FoodKey : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public MeshRenderer meshRenderer;
    public Material lockedMaterial;

    [Header("Spin")]
    public float spinSpeed = 120f;

    [Header("Fly")]
    public float jumpPower = 2f;
    public float jumpDuration = 0.6f;
    public Ease jumpEase = Ease.OutQuad;

    MaterialPropertyBlock _block;
    Material _realMaterial;
    Color _color = Color.white;
    bool _hasColor;
    bool _locked;
    Tween _spin;

    public bool IsLocked => _locked;
    public string ColorHex { get; private set; }

    public void SetColor(string hex)
    {
        ColorHex = hex;
        SetColor(ColonyPalette.ToColor(hex));
    }

    public void SetColor(Color color)
    {
        _color = color;
        _hasColor = true;
        if (_locked) return;
        ApplyColor();
    }

    public void SetLocked(bool value)
    {
        _locked = value;
        if (meshRenderer == null) return;

        if (value)
        {
            if (lockedMaterial != null)
            {
                if (_realMaterial == null) _realMaterial = meshRenderer.sharedMaterial;
                meshRenderer.sharedMaterial = lockedMaterial;
                ClearBlock();
            }
            StopSpin();
        }
        else
        {
            if (_realMaterial != null) meshRenderer.sharedMaterial = _realMaterial;
            if (_hasColor) ApplyColor();
            StartSpin();
        }
    }

    public void Unlock() => SetLocked(false);

    public void Fly(Vector3 worldTarget, TweenCallback onArrive)
    {
        StopSpin();
        transform.SetParent(null, true);

        transform.DOJump(worldTarget, jumpPower, 1, jumpDuration)
                 .SetEase(jumpEase)
                 .SetLink(gameObject)
                 .OnComplete(() =>
                 {
                     onArrive?.Invoke();
                     Destroy(gameObject);
                 });
    }

    void StartSpin()
    {
        StopSpin();
        if (spinSpeed <= 0f) return;

        _spin = transform.DOLocalRotate(new Vector3(0f, 360f, 0f), 360f / spinSpeed, RotateMode.LocalAxisAdd)
                         .SetEase(Ease.Linear)
                         .SetLoops(-1)
                         .SetLink(gameObject);
    }

    void StopSpin()
    {
        _spin?.Kill();
        _spin = null;
    }

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
}
