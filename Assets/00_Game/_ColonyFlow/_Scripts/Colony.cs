using UnityEngine;

public class Colony : InitSingleton<Colony>
{
    [SerializeField] private GridController gridController;
    [SerializeField] private WaitAreas waitAreas;
    [SerializeField] private ColonyBooster colonyBooster;

    public GridController Grid => gridController;
    public WaitAreas Waits => waitAreas;
    public ColonyBooster Booster => colonyBooster;

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
}
