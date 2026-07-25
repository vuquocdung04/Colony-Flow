using UnityEngine;

public class AnthillLock : MonoBehaviour
{
    public Transform lockRoot;
    public Transform lockShackle;
    public MeshRenderer body;
    public MeshRenderer shackle;

    Anthill _anthill;
    AnthillVisual _visual;
    MaterialPropertyBlock _block;
    bool _active;

    public bool IsActive => _active;

    public Transform Target => lockRoot != null ? lockRoot : transform;

    public void Init(Anthill owner, AnthillVisual visual)
    {
        _anthill = owner;
        _visual = visual;
    }

    public void SetColor(string hex) => SetColor(ColonyPalette.ToColor(hex));

    public void SetColor(Color color)
    {
        _block ??= new MaterialPropertyBlock();
        ColonyPalette.Tint(body, _block, color);
        ColonyPalette.Tint(shackle, _block, color);
    }

    public void SetLocked(bool value)
    {
        _active = value;
        if (lockRoot != null) lockRoot.gameObject.SetActive(value);
        if (_visual != null) _visual.SetContentActive(!value);
    }

    public void TryUnlock(bool condition)
    {
    }

    public void PlayAnimationUnlock()
    {
    }
}
