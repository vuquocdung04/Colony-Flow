using UnityEngine;

public abstract class InputMode
{
    protected InputController Controller { get; private set; }

    public virtual void OnEnter(InputController controller)
    {
        Controller = controller;
    }

    public virtual void OnExit()
    {
    }

    public virtual void HandleRay(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        HandleClick(hit);
    }

    public virtual void HandleClick(RaycastHit hit) { }
}