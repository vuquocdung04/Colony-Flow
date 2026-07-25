using UnityEngine;

public class AnthillLock : MonoBehaviour
{
    public Transform lockRoot;
    public Transform lockShackle;

    Anthill _anthill;
    AnthillVisual _visual;
    bool _active;

    public void Init(Anthill owner, AnthillVisual visual)
    {
        _anthill = owner;
        _visual = visual;
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
