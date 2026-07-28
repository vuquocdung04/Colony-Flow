using UnityEngine;

public class HighlightObject : MonoBehaviour
{
    public const string LayerName = "HighlightObj";

    static int _layer = -1;

    Transform[] _nodes;
    int[] _layers;
    bool _active;

    public bool IsActive => _active;

    public static int Layer
    {
        get
        {
            if (_layer < 0) _layer = LayerMask.NameToLayer(LayerName);
            return _layer;
        }
    }

    public static void Set(GameObject target, bool value)
    {
        if (target == null) return;

        HighlightObject highlight = target.GetComponent<HighlightObject>();

        if (highlight == null)
        {
            if (!value) return;
            highlight = target.AddComponent<HighlightObject>();
        }

        highlight.SetHighlight(value);
    }

    public void SetHighlight(bool value)
    {
        if (value == _active) return;

        if (value) Apply();
        else Restore();

        _active = value;
    }

    void Apply()
    {
        if (Layer < 0)
        {
            Debug.LogWarning($"[HighlightObject] Chưa có layer '{LayerName}'.", this);
            return;
        }

        _nodes = GetComponentsInChildren<Transform>(true);
        _layers = new int[_nodes.Length];

        for (int i = 0; i < _nodes.Length; i++)
        {
            _layers[i] = _nodes[i].gameObject.layer;
            _nodes[i].gameObject.layer = Layer;
        }
    }

    void Restore()
    {
        if (_nodes == null) return;

        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i] == null) continue;
            _nodes[i].gameObject.layer = _layers[i];
        }

        _nodes = null;
        _layers = null;
    }

    void OnDisable() => SetHighlight(false);
}
