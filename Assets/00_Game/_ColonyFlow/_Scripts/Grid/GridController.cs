using Sirenix.OdinInspector;
using UnityEngine;

public class GridController : MonoBehaviour
{
    public TextAsset levelFile;
    public GridTop gridTop;
    public GridBottom gridBottom;

    public ColonyLevelData Data { get; private set; }

    void Awake() => Spawn();

    [Button(ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.5f)]
    public void Spawn()
    {
        if (levelFile == null)
        {
            Debug.LogWarning("[GridController] Chưa gán levelFile.", this);
            return;
        }

        Spawn(levelFile.text);
    }

    public void Spawn(string json)
    {
        Data = ColonyLevelIO.FromJson(json);
        if (Data == null)
        {
            Debug.LogError("[GridController] Không đọc được level JSON.", this);
            return;
        }

        if (gridTop != null) gridTop.Load(Data.top);
        if (gridBottom != null) gridBottom.Load(Data.bottom);
    }

    [Button(ButtonSizes.Medium)]
    public void Clear()
    {
        if (gridTop != null) gridTop.Clear();
        if (gridBottom != null) gridBottom.Clear();
        Data = null;
    }
}
