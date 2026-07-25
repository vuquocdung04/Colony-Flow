using UnityEngine;

public class AnthillHidden : MonoBehaviour
{
    public Transform hidden;
    public ParticleSystem hiddenReveal;
    public Transform linkedPoint;

    Anthill _anthill;
    AnthillVisual _visual;
    bool _active;

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

    public void TryUnlock(bool condition)
    {
        if (!_active || !condition) return;

        _active = false;
        if (hiddenReveal != null) hiddenReveal.Play();
        if (hidden != null) hidden.gameObject.SetActive(false);
        if (_visual != null) _visual.SetContentActive(true);
    }
}
