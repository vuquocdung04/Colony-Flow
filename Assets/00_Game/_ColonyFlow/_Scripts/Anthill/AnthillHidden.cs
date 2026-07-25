using UnityEngine;

public class AnthillHidden : MonoBehaviour
{
    public Transform hidden;
    public ParticleSystem hiddenReveal;

    Anthill _anthill;
    AnthillVisual _visual;
    bool _active;

    public bool IsActive => _active;

    public void Init(Anthill owner, AnthillVisual visual)
    {
        _anthill = owner;
        _visual = visual;
    }

    public void SetHidden(bool value)
    {
        _active = value;
        if (hidden != null) hidden.gameObject.SetActive(value);
        if (_visual != null) _visual.SetContentActive(!value);
    }

    public bool TryUnlock(bool condition)
    {
        if (!_active || !condition) return false;

        _active = false;
        if (hiddenReveal != null) hiddenReveal.Play();
        if (hidden != null) hidden.gameObject.SetActive(false);
        if (_visual != null) _visual.SetContentActive(true);
        return true;
    }
}
