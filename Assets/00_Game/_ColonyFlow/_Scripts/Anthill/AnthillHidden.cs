using UnityEngine;

public class AnthillHidden : MonoBehaviour
{
    public Transform hidden;
    public ParticleSystem hiddenReveal;
    public Transform linkedPoint;

    Anthill _anthill;
    AnthillVisual _visual;

    public void Init(Anthill owner, AnthillVisual visual)
    {
        _anthill = owner;
        _visual = visual;
    }

    public void Reveal()
    {
    }
}
