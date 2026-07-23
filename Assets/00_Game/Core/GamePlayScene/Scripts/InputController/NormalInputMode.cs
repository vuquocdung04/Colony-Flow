using EventDispatcher;
using UnityEngine;

public class NormalInputMode : InputMode
{
    public override void HandleClick(RaycastHit hit)
    {
        if (hit.collider == null) return;

        Anthill anthill = hit.collider.GetComponentInParent<Anthill>();
        if (anthill == null) return;

        anthill.TrySelect();
    }


}