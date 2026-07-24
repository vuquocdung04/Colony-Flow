using UnityEngine;

public class AnthillLock : MonoBehaviour
{
    public Transform lockRoot;
    public Transform lockShackle;

    Anthill _anthill;
    AnthillVisual _visual;

    public void Init(Anthill owner, AnthillVisual visual)
    {
        _anthill = owner;
        _visual = visual;
    }

    public void Unlock()
    {
    }
}
