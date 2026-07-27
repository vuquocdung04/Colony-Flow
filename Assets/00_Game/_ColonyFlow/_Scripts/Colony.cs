using UnityEngine;

public class Colony : InitSingleton<Colony>
{
    [SerializeField] private GridController gridController;
    [SerializeField] private WaitAreas waitAreas;

    public GridController Grid => gridController;
    public WaitAreas Waits => waitAreas;

    public GridTop Top => gridController != null ? gridController.gridTop : null;
    public GridBottom Bottom => gridController != null ? gridController.gridBottom : null;

    public override void Init()
    {
        if (gridController == null)
        {
            Debug.LogWarning("[Colony] Chưa gán GridController.", this);
            return;
        }

        gridController.Spawn(waitAreas);
    }

    public bool AddWaitSlot() => waitAreas != null && waitAreas.AddSlot();
}
