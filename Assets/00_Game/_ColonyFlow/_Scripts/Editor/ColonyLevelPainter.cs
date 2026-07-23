using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class ColonyLevelPainter : OdinEditorWindow
{
    const string DefaultFolder = "Assets/00_Game/_ColonyFlow/Levels";
    const int MaxGrid = 64;

    static readonly Color BorderColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    static readonly Color EmptyColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    [MenuItem("Tools/Colony Flow/Level Painter")]
    static void Open()
    {
        ColonyLevelPainter window = GetWindow<ColonyLevelPainter>();
        window.titleContent = new GUIContent("Level Painter");
        window.minSize = new Vector2(900f, 620f);
    }

    [PropertyOrder(0)]
    [HorizontalGroup("File", 110f), LabelWidth(40f)]
    [MinValue(0)]
    public int level = 1;

    [PropertyOrder(0)]
    [HorizontalGroup("File"), LabelWidth(46f)]
    [FolderPath(RequireExistingPath = false)]
    public string folder = DefaultFolder;

    [PropertyOrder(1)]
    [HorizontalGroup("Actions", 0.5f)]
    [Button("SAVE", ButtonSizes.Large), GUIColor(0.45f, 0.85f, 0.5f)]
    void SaveLevel()
    {
        EnsureArrays();

        string directory = ResolveFolder();
        string path = FilePath(directory);

        if (File.Exists(path) &&
            !EditorUtility.DisplayDialog("Ghi đè level?", path + "\n\nFile này đã tồn tại.", "Ghi đè", "Huỷ"))
            return;

        Directory.CreateDirectory(directory);
        ColonyLevelData data = ColonyLevelData.FromCells(_topCells, _topX, _topY, _botCells, _botCapacity, _botX, _botY);
        File.WriteAllText(path, ColonyLevelIO.ToJson(data));

        AssetDatabase.Refresh();
        Debug.Log("[LevelPainter] Saved " + path);
    }

    [PropertyOrder(1)]
    [HorizontalGroup("Actions")]
    [Button("LOAD", ButtonSizes.Large)]
    void LoadLevel()
    {
        string path = FilePath(ResolveFolder());
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Không tìm thấy file", path, "OK");
            return;
        }

        ColonyLevelData data = ColonyLevelIO.FromJson(File.ReadAllText(path));
        if (data == null)
        {
            EditorUtility.DisplayDialog("File lỗi", "Không đọc được JSON:\n" + path, "OK");
            return;
        }

        _topX = Mathf.Clamp(data.top.gridX, 1, MaxGrid);
        _topY = Mathf.Clamp(data.top.gridY, 1, MaxGrid);
        _topCells = data.TopToCells();

        _botX = Mathf.Clamp(data.bottom.gridX, 1, MaxGrid);
        _botY = Mathf.Clamp(data.bottom.gridY, 1, MaxGrid);
        data.BottomToCells(out _botCells, out _botCapacity);

        Repaint();
    }

    [SerializeField, HideInInspector] int _brush;
    [SerializeField, HideInInspector] int _capacityValue = 5;
    [SerializeField, HideInInspector] float _cellSize = 20f;
    [SerializeField, HideInInspector] bool _showIndex;

    [SerializeField, HideInInspector] int _topX = 24;
    [SerializeField, HideInInspector] int _topY = 24;
    [SerializeField, HideInInspector] string[] _topCells;

    [SerializeField, HideInInspector] int _botX = 4;
    [SerializeField, HideInInspector] int _botY = 2;
    [SerializeField, HideInInspector] string[] _botCells;
    [SerializeField, HideInInspector] int[] _botCapacity;

    GUIStyle _cellLabel;

    GUIStyle CellLabel
    {
        get
        {
            _cellLabel ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            return _cellLabel;
        }
    }

    [PropertyOrder(2)]
    [OnInspectorGUI]
    void DrawBody()
    {
        EnsureArrays();

        EditorGUILayout.Space(4f);
        DrawOptionsBar();
        DrawPalette();
        EditorGUILayout.HelpBox("Chuột trái: tô màu    •    Chuột phải: xoá    •    Giữ và kéo để tô liên tục", MessageType.None);
        EditorGUILayout.Space(6f);

        EditorGUILayout.BeginHorizontal();
        DrawTopPanel();
        GUILayout.Space(12f);
        DrawBottomPanel();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawOptionsBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File", Path.GetFileName(FilePath(ResolveFolder())), EditorStyles.miniBoldLabel, GUILayout.Width(220f));
        GUILayout.Space(12f);

        EditorGUIUtility.labelWidth = 60f;
        _cellSize = EditorGUILayout.Slider("Cell size", _cellSize, 10f, 34f, GUILayout.Width(220f));
        GUILayout.Space(12f);
        EditorGUIUtility.labelWidth = 74f;
        _showIndex = EditorGUILayout.Toggle("Show index", _showIndex, GUILayout.Width(94f));
        EditorGUIUtility.labelWidth = 0f;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawPalette()
    {
        Event current = Event.current;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush", GUILayout.Width(40f));

        for (int i = 0; i < ColonyPalette.Count; i++)
        {
            Rect rect = GUILayoutUtility.GetRect(30f, 26f, GUILayout.Width(30f), GUILayout.Height(26f));
            EditorGUI.DrawRect(rect, _brush == i ? Color.white : BorderColor);
            EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f),
                               ColonyPalette.ToColor(ColonyPalette.HexAt(i)));

            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                _brush = i;
                current.Use();
                Repaint();
            }
        }

        GUILayout.Space(10f);
        EditorGUILayout.LabelField($"{ColonyPalette.NameAt(_brush)}   {ColonyPalette.HexAt(_brush)}", GUILayout.Width(160f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawTopPanel()
    {
        float width = Mathf.Max(280f, _topX * _cellSize + 26f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
        EditorGUILayout.LabelField("GRID TOP", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 52f;
        int nextX = Mathf.Clamp(EditorGUILayout.IntField("Grid X", _topX), 1, MaxGrid);
        int nextY = Mathf.Clamp(EditorGUILayout.IntField("Grid Y", _topY), 1, MaxGrid);
        EditorGUIUtility.labelWidth = 0f;

        if (nextX != _topX || nextY != _topY)
        {
            _topCells = Resize(_topCells, _topX, _topY, nextX, nextY);
            _topX = nextX;
            _topY = nextY;
        }

        EditorGUILayout.Space(4f);
        DrawGrid(_topX, _topY, _topCells, null);
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField($"Đã tô {PaintedCount(_topCells)} / {_topCells.Length} ô", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawBottomPanel()
    {
        float width = Mathf.Max(280f, _botX * _cellSize + 26f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(width));
        EditorGUILayout.LabelField("GRID BOTTOM", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 52f;
        int nextX = Mathf.Clamp(EditorGUILayout.IntField("Grid X", _botX), 1, MaxGrid);
        int nextY = Mathf.Clamp(EditorGUILayout.IntField("Grid Y", _botY), 1, MaxGrid);
        EditorGUIUtility.labelWidth = 0f;

        if (nextX != _botX || nextY != _botY)
        {
            _botCells = Resize(_botCells, _botX, _botY, nextX, nextY);
            _botCapacity = Resize(_botCapacity, _botX, _botY, nextX, nextY);
            _botX = nextX;
            _botY = nextY;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 62f;
        _capacityValue = Mathf.Max(0, EditorGUILayout.IntField("Capacity", _capacityValue));
        EditorGUIUtility.labelWidth = 0f;
        if (GUILayout.Button("Auto Capacity", GUILayout.Width(110f))) AutoCapacity();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);
        DrawGrid(_botX, _botY, _botCells, _botCapacity);
        EditorGUILayout.Space(2f);

        int totalCapacity = 0;
        for (int i = 0; i < _botCapacity.Length; i++)
            if (!string.IsNullOrEmpty(_botCells[i])) totalCapacity += _botCapacity[i];

        int painted = PaintedCount(_topCells);
        EditorGUILayout.LabelField($"Tổng capacity {totalCapacity} / {painted} ô top", EditorStyles.miniLabel);
        if (totalCapacity != painted)
            EditorGUILayout.HelpBox("Capacity không khớp số ô ở Grid Top.", MessageType.Warning);

        EditorGUILayout.EndVertical();
    }

    void DrawGrid(int cols, int rows, string[] cells, int[] capacity)
    {
        Event current = Event.current;

        for (int y = 0; y < rows; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < cols; x++)
            {
                int index = y * cols + x;
                Rect rect = GUILayoutUtility.GetRect(_cellSize, _cellSize, GUILayout.Width(_cellSize), GUILayout.Height(_cellSize));
                DrawCell(rect, index, cells, capacity);
                HandleCellInput(rect, index, cells, capacity, current);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawCell(Rect rect, int index, string[] cells, int[] capacity)
    {
        string hex = cells[index];
        bool painted = !string.IsNullOrEmpty(hex);
        Color fill = painted ? ColonyPalette.ToColor(hex) : EmptyColor;

        EditorGUI.DrawRect(rect, BorderColor);
        EditorGUI.DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), fill);

        if (_cellSize < 15f) return;

        string label = null;
        if (capacity != null && painted) label = capacity[index].ToString();
        else if (_showIndex) label = index.ToString();
        if (label == null) return;

        CellLabel.fontSize = Mathf.Clamp(Mathf.RoundToInt(_cellSize * 0.44f), 7, 14);
        CellLabel.normal.textColor = Luminance(fill) > 0.55f ? Color.black : Color.white;
        GUI.Label(rect, label, CellLabel);
    }

    void HandleCellInput(Rect rect, int index, string[] cells, int[] capacity, Event current)
    {
        if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag) return;
        if (!rect.Contains(current.mousePosition)) return;

        if (current.button == 0)
        {
            cells[index] = ColonyPalette.HexAt(_brush);
            if (capacity != null) capacity[index] = _capacityValue;
        }
        else if (current.button == 1)
        {
            cells[index] = null;
            if (capacity != null) capacity[index] = 0;
        }
        else
        {
            return;
        }

        current.Use();
        Repaint();
    }

    void AutoCapacity()
    {
        EnsureArrays();

        Dictionary<string, int> totals = new Dictionary<string, int>();
        foreach (string hex in _topCells)
        {
            if (string.IsNullOrEmpty(hex)) continue;
            totals.TryGetValue(hex, out int count);
            totals[hex] = count + 1;
        }

        Dictionary<string, List<int>> slots = new Dictionary<string, List<int>>();
        for (int i = 0; i < _botCells.Length; i++)
        {
            string hex = _botCells[i];
            if (string.IsNullOrEmpty(hex))
            {
                _botCapacity[i] = 0;
                continue;
            }
            if (!slots.TryGetValue(hex, out List<int> list))
            {
                list = new List<int>();
                slots[hex] = list;
            }
            list.Add(i);
        }

        foreach (KeyValuePair<string, List<int>> pair in slots)
        {
            List<int> list = pair.Value;
            totals.TryGetValue(pair.Key, out int total);

            int share = total / list.Count;
            int firstWithExtra = list.Count - total % list.Count;
            for (int i = 0; i < list.Count; i++)
                _botCapacity[list[i]] = share + (i >= firstWithExtra ? 1 : 0);
        }

        Repaint();
    }

    void EnsureArrays()
    {
        _topX = Mathf.Clamp(_topX, 1, MaxGrid);
        _topY = Mathf.Clamp(_topY, 1, MaxGrid);
        _botX = Mathf.Clamp(_botX, 1, MaxGrid);
        _botY = Mathf.Clamp(_botY, 1, MaxGrid);

        if (_topCells == null || _topCells.Length != _topX * _topY)
            _topCells = Resize(_topCells, _topX, _topY, _topX, _topY);
        if (_botCells == null || _botCells.Length != _botX * _botY)
            _botCells = Resize(_botCells, _botX, _botY, _botX, _botY);
        if (_botCapacity == null || _botCapacity.Length != _botX * _botY)
            _botCapacity = Resize(_botCapacity, _botX, _botY, _botX, _botY);
    }

    string ResolveFolder() => string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder.Trim();

    string FilePath(string directory) =>
        Path.Combine(directory, ColonyLevelIO.FileName(level)).Replace('\\', '/');

    static T[] Resize<T>(T[] source, int oldX, int oldY, int newX, int newY)
    {
        T[] next = new T[newX * newY];
        if (source == null || source.Length != oldX * oldY) return next;

        int copyX = Mathf.Min(oldX, newX);
        int copyY = Mathf.Min(oldY, newY);
        for (int y = 0; y < copyY; y++)
            for (int x = 0; x < copyX; x++)
                next[y * newX + x] = source[y * oldX + x];

        return next;
    }

    static int PaintedCount(string[] cells)
    {
        int count = 0;
        foreach (string hex in cells)
            if (!string.IsNullOrEmpty(hex)) count++;
        return count;
    }

    static float Luminance(Color color) => 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
}
