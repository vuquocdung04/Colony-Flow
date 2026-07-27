using UnityEngine;

public class GridController : MonoBehaviour
{
    public TextAsset levelFile;
    public GridTop gridTop;
    public GridBottom gridBottom;

    public ColonyLevelData Data { get; private set; }

    public void Spawn(WaitAreas waitAreas)
    {
        if (levelFile == null)
        {
            Debug.LogWarning("[GridController] Chưa gán levelFile.", this);
            return;
        }

        Spawn(levelFile.text, waitAreas);
    }

    public void Spawn(string json, WaitAreas waitAreas)
    {
        Data = ColonyLevelIO.FromJson(json);
        if (Data == null)
        {
            Debug.LogError("[GridController] Không đọc được level JSON.", this);
            return;
        }

        if (gridTop != null) gridTop.Load(Data.top);
        if (gridBottom != null) gridBottom.Load(Data.bottom, gridTop, waitAreas);
    }

    public void Clear()
    {
        if (gridTop != null) gridTop.Clear();
        if (gridBottom != null) gridBottom.Clear();
        Data = null;
    }
}
