using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public enum ColonyPaintMode
{
    Color,
    Hidden,
    Lock,
    Link,
}

public class ColonyLevelPainter : OdinEditorWindow
{
    const string DefaultFolder = "Assets/00_Game/_ColonyFlow/Levels";
    const int MaxGrid = 64;
    const int MaxLinkId = 9;

    const float CheckerShade = 0.93f;

    static readonly Color BorderColor = new Color(0.10f, 0.10f, 0.10f, 1f);
    static readonly Color EmptyColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    static readonly Color EmptyDark = new Color(0.13f, 0.13f, 0.13f, 1f);
    static readonly Color EmptyLight = new Color(0.24f, 0.24f, 0.24f, 1f);
    static readonly Color HiddenDot = new Color(0.04f, 0.04f, 0.04f, 1f);
    static readonly Color LockDot = new Color(0.98f, 0.78f, 0.12f, 1f);
    static readonly Color GuideColor = new Color(1f, 1f, 1f, 0.35f);
    static readonly Color CenterGuideColor = new Color(1f, 1f, 1f, 0.8f);
    static readonly Color ModeOnColor = new Color(0.45f, 0.9f, 1f, 1f);

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
        ColonyLevelData data = ColonyLevelData.FromCells(
            _topCells, _topHidden, _topX, _topY,
            _botCells, _botCapacity, _botHidden, _botLock, _botLink, _botX, _botY);
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
        _topHidden = data.TopHiddenFlags();

        _botX = Mathf.Clamp(data.bottom.gridX, 1, MaxGrid);
        _botY = Mathf.Clamp(data.bottom.gridY, 1, MaxGrid);
        data.BottomToCells(out _botCells, out _botCapacity, out _botHidden, out _botLock, out _botLink);

        Repaint();
    }

    [SerializeField, HideInInspector] int _brush;
    [SerializeField, HideInInspector] ColonyPaintMode _mode = ColonyPaintMode.Color;
    [SerializeField, HideInInspector] int _linkId = 1;
    [SerializeField, HideInInspector] int _capacityValue = 5;
    [SerializeField, HideInInspector] float _cellSize = 20f;
    [SerializeField, HideInInspector] float _botCellSize = 40f;
    [SerializeField, HideInInspector] int _guideStep = 4;
    [SerializeField, HideInInspector] bool _showIndex;

    [SerializeField, HideInInspector] int _topX = 24;
    [SerializeField, HideInInspector] int _topY = 24;
    [SerializeField, HideInInspector] string[] _topCells;
    [SerializeField, HideInInspector] bool[] _topHidden;

    [SerializeField, HideInInspector] int _botX = 4;
    [SerializeField, HideInInspector] int _botY = 2;
    [SerializeField, HideInInspector] string[] _botCells;
    [SerializeField, HideInInspector] int[] _botCapacity;
    [SerializeField, HideInInspector] bool[] _botHidden;
    [SerializeField, HideInInspector] bool[] _botLock;
    [SerializeField, HideInInspector] int[] _botLink;

    Rect[] _topRects;
    Rect[] _botRects;
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
        DrawModeBar();
        EditorGUILayout.HelpBox(HelpText(), MessageType.None);
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
        _cellSize = EditorGUILayout.Slider("Top cell", _cellSize, 10f, 34f, GUILayout.Width(220f));
        GUILayout.Space(12f);
        EditorGUIUtility.labelWidth = 70f;
        _botCellSize = EditorGUILayout.Slider("Bottom cell", _botCellSize, 20f, 80f, GUILayout.Width(230f));
        GUILayout.Space(12f);
        EditorGUIUtility.labelWidth = 44f;
        _guideStep = Mathf.Clamp(EditorGUILayout.IntField("Guide", _guideStep, GUILayout.Width(90f)), 0, MaxGrid);
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

    void DrawModeBar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mode", GUILayout.Width(40f));

        DrawModeButton(ColonyPaintMode.Color, "COLOR");
        DrawModeButton(ColonyPaintMode.Hidden, "HIDDEN");
        DrawModeButton(ColonyPaintMode.Lock, "LOCK");
        DrawModeButton(ColonyPaintMode.Link, "LINK");

        GUILayout.Space(12f);

        EditorGUIUtility.labelWidth = 46f;
        using (new EditorGUI.DisabledScope(_mode != ColonyPaintMode.Link))
            _linkId = Mathf.Clamp(EditorGUILayout.IntField("Link #", _linkId, GUILayout.Width(96f)), 1, MaxLinkId);
        EditorGUIUtility.labelWidth = 0f;

        Rect swatch = GUILayoutUtility.GetRect(26f, 18f, GUILayout.Width(26f), GUILayout.Height(18f));
        EditorGUI.DrawRect(swatch, BorderColor);
        EditorGUI.DrawRect(new Rect(swatch.x + 1f, swatch.y + 1f, swatch.width - 2f, swatch.height - 2f), LinkColor(_linkId));

        GUILayout.Space(10f);
        if (GUILayout.Button("Clear links", EditorStyles.miniButton, GUILayout.Width(90f)))
        {
            System.Array.Clear(_botLink, 0, _botLink.Length);
            Repaint();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    void DrawModeButton(ColonyPaintMode mode, string label)
    {
        Color previous = GUI.backgroundColor;
        if (_mode == mode) GUI.backgroundColor = ModeOnColor;

        if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Width(76f), GUILayout.Height(20f)))
        {
            _mode = mode;
            Repaint();
        }

        GUI.backgroundColor = previous;
    }

    string HelpText() => _mode switch
    {
        ColonyPaintMode.Hidden => "HIDDEN  •  Chuột trái: đánh dấu ẩn    •    Chuột phải: bỏ    •    Giữ và kéo để chọn nhiều ô",
        ColonyPaintMode.Lock => "LOCK  •  Chuột trái: khoá ô    •    Chuột phải: mở    •    Giữ và kéo để chọn nhiều ô",
        ColonyPaintMode.Link => $"LINK #{_linkId}  •  Chuột trái: nối ô vào nhóm {_linkId}    •    Chuột phải: bỏ khỏi nhóm",
        _ => "COLOR  •  Chuột trái: tô màu    •    Chuột phải: xoá ô    •    Giữ và kéo để tô liên tục",
    };

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
            _topHidden = Resize(_topHidden, _topX, _topY, nextX, nextY);
            _topX = nextX;
            _topY = nextY;
        }

        EditorGUILayout.Space(4f);
        DrawGrid(_topX, _topY, _topCells, null, _cellSize, false);
        DrawGuides(_topX, _topY, _topRects);
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField($"Đã tô {PaintedCount(_topCells)} / {_topCells.Length} ô    •    Hidden {CountFlags(_topHidden)}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    void DrawBottomPanel()
    {
        float width = Mathf.Max(280f, _botX * _botCellSize + 26f);
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
            _botHidden = Resize(_botHidden, _botX, _botY, nextX, nextY);
            _botLock = Resize(_botLock, _botX, _botY, nextX, nextY);
            _botLink = Resize(_botLink, _botX, _botY, nextX, nextY);
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
        DrawGrid(_botX, _botY, _botCells, _botCapacity, _botCellSize, true);
        DrawLinkLines();
        EditorGUILayout.Space(2f);

        int totalCapacity = 0;
        for (int i = 0; i < _botCapacity.Length; i++)
            if (!string.IsNullOrEmpty(_botCells[i])) totalCapacity += _botCapacity[i];

        int painted = PaintedCount(_topCells);
        EditorGUILayout.LabelField($"Tổng capacity {totalCapacity} / {painted} ô top", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Hidden {CountFlags(_botHidden)}    Lock {CountFlags(_botLock)}    Link {LinkGroups().Count} nhóm", EditorStyles.miniLabel);
        if (totalCapacity != painted)
            EditorGUILayout.HelpBox("Capacity không khớp số ô ở Grid Top.", MessageType.Warning);

        EditorGUILayout.EndVertical();
    }

    void DrawGrid(int cols, int rows, string[] cells, int[] capacity, float cellSize, bool bottom)
    {
        Event current = Event.current;
        Rect[] rects = EnsureRects(cells.Length, bottom);

        for (int y = 0; y < rows; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < cols; x++)
            {
                int index = y * cols + x;
                Rect rect = GUILayoutUtility.GetRect(cellSize, cellSize, GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                rects[index] = rect;

                DrawCell(rect, index, cells, capacity, cellSize, bottom, ((x + y) & 1) == 1);
                HandleCellInput(rect, index, cells, capacity, bottom, current);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }

    Rect[] EnsureRects(int count, bool bottom)
    {
        if (bottom)
        {
            if (_botRects == null || _botRects.Length != count) _botRects = new Rect[count];
            return _botRects;
        }

        if (_topRects == null || _topRects.Length != count) _topRects = new Rect[count];
        return _topRects;
    }

    void DrawCell(Rect rect, int index, string[] cells, int[] capacity, float cellSize, bool bottom, bool alt)
    {
        string hex = cells[index];
        bool painted = !string.IsNullOrEmpty(hex);
        Color fill = painted ? ColonyPalette.ToColor(hex) : EmptyColor;
        Rect inner = rect;

        if (bottom)
        {
            EditorGUI.DrawRect(rect, BorderColor);
            inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            EditorGUI.DrawRect(inner, fill);
        }
        else
        {
            if (!painted) fill = alt ? EmptyLight : EmptyDark;
            else if (alt) fill = new Color(fill.r * CheckerShade, fill.g * CheckerShade, fill.b * CheckerShade, 1f);

            EditorGUI.DrawRect(rect, fill);
        }

        if (bottom && _botLink[index] > 0)
            DrawFrame(inner, LinkColor(_botLink[index]), Mathf.Max(2f, cellSize * 0.08f));

        if (cellSize < 15f) return;

        if (bottom)
        {
            float radius = Mathf.Clamp(cellSize * 0.11f, 3f, 7f);
            float margin = radius + 3f;

            if (_botHidden[index])
                DrawDot(new Vector2(inner.xMax - margin, inner.yMax - margin), radius, HiddenDot);
            if (_botLock[index])
                DrawDot(new Vector2(inner.xMax - margin, inner.y + margin), radius, LockDot);
        }
        else if (_topHidden[index])
        {
            float radius = Mathf.Clamp(cellSize * 0.18f, 2.5f, 6f);
            DrawDot(rect.center, radius, HiddenDot);
        }

        string label = null;
        if (capacity != null && painted) label = capacity[index].ToString();
        else if (_showIndex) label = index.ToString();
        if (label == null) return;

        CellLabel.fontSize = Mathf.Clamp(Mathf.RoundToInt(cellSize * 0.44f), 7, 16);
        CellLabel.normal.textColor = Luminance(fill) > 0.55f ? Color.black : Color.white;
        GUI.Label(rect, label, CellLabel);
    }

    static void DrawDot(Vector2 center, float radius, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;

        Color previous = Handles.color;
        Vector3 position = new Vector3(center.x, center.y, 0f);

        Handles.color = Luminance(color) > 0.5f ? new Color(0f, 0f, 0f, 0.65f) : new Color(1f, 1f, 1f, 0.75f);
        Handles.DrawSolidDisc(position, Vector3.forward, radius + 1.4f);

        Handles.color = color;
        Handles.DrawSolidDisc(position, Vector3.forward, radius);

        Handles.color = previous;
    }

    static void DrawFrame(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    void DrawGuides(int cols, int rows, Rect[] rects)
    {
        if (Event.current.type != EventType.Repaint || rects == null || rects.Length != cols * rows) return;

        Rect first = rects[0];
        Rect last = rects[rects.Length - 1];
        float left = first.xMin;
        float right = last.xMax;
        float top = first.yMin;
        float bottom = last.yMax;

        if (_guideStep > 1)
        {
            for (int x = _guideStep; x < cols; x += _guideStep)
                EditorGUI.DrawRect(new Rect(rects[x].xMin - 1f, top, 1f, bottom - top), GuideColor);

            for (int y = _guideStep; y < rows; y += _guideStep)
                EditorGUI.DrawRect(new Rect(left, rects[y * cols].yMin - 1f, right - left, 1f), GuideColor);
        }

        float centerX = cols % 2 == 0 ? rects[cols / 2].xMin - 1f : rects[cols / 2].center.x - 1f;
        float centerY = rows % 2 == 0 ? rects[rows / 2 * cols].yMin - 1f : rects[rows / 2 * cols].center.y - 1f;

        EditorGUI.DrawRect(new Rect(centerX, top, 2f, bottom - top), CenterGuideColor);
        EditorGUI.DrawRect(new Rect(left, centerY, right - left, 2f), CenterGuideColor);

        DrawFrame(new Rect(left, top, right - left, bottom - top), GuideColor, 1f);
    }

    void DrawLinkLines()
    {
        if (Event.current.type != EventType.Repaint || _botRects == null) return;

        SortedDictionary<int, List<int>> groups = LinkGroups();
        if (groups.Count == 0) return;

        Color previous = Handles.color;

        foreach (KeyValuePair<int, List<int>> pair in groups)
        {
            List<int> members = pair.Value;
            if (members.Count < 2) continue;

            Vector3[] points = new Vector3[members.Count];
            for (int i = 0; i < members.Count; i++)
            {
                Rect rect = _botRects[members[i]];
                points[i] = new Vector3(rect.center.x, rect.center.y, 0f);
            }

            Color color = LinkColor(pair.Key);
            Handles.color = new Color(color.r, color.g, color.b, 0.4f);
            Handles.DrawAAPolyLine(Mathf.Max(6f, _botCellSize * 0.22f), points);
        }

        Handles.color = previous;
    }

    SortedDictionary<int, List<int>> LinkGroups()
    {
        SortedDictionary<int, List<int>> groups = new SortedDictionary<int, List<int>>();
        if (_botLink == null) return groups;

        for (int i = 0; i < _botLink.Length; i++)
        {
            int id = _botLink[i];
            if (id <= 0) continue;

            if (!groups.TryGetValue(id, out List<int> members))
            {
                members = new List<int>();
                groups[id] = members;
            }
            members.Add(i);
        }

        return groups;
    }

    void HandleCellInput(Rect rect, int index, string[] cells, int[] capacity, bool bottom, Event current)
    {
        if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag) return;
        if (!rect.Contains(current.mousePosition)) return;
        if (current.button != 0 && current.button != 1) return;

        bool paint = current.button == 0;

        if (!bottom && _mode == ColonyPaintMode.Hidden)
        {
            _topHidden[index] = paint;
        }
        else if (!bottom || _mode == ColonyPaintMode.Color)
        {
            if (paint)
            {
                cells[index] = ColonyPalette.HexAt(_brush);
                if (capacity != null) capacity[index] = _capacityValue;
            }
            else
            {
                cells[index] = null;
                if (capacity != null) capacity[index] = 0;
                if (bottom) ClearMarks(index);
                else _topHidden[index] = false;
            }
        }
        else
        {
            switch (_mode)
            {
                case ColonyPaintMode.Hidden: _botHidden[index] = paint; break;
                case ColonyPaintMode.Lock: _botLock[index] = paint; break;
                case ColonyPaintMode.Link: _botLink[index] = paint ? _linkId : 0; break;
            }
        }

        current.Use();
        Repaint();
    }

    void ClearMarks(int index)
    {
        _botHidden[index] = false;
        _botLock[index] = false;
        _botLink[index] = 0;
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
        if (_topHidden == null || _topHidden.Length != _topX * _topY)
            _topHidden = Resize(_topHidden, _topX, _topY, _topX, _topY);
        if (_botCells == null || _botCells.Length != _botX * _botY)
            _botCells = Resize(_botCells, _botX, _botY, _botX, _botY);
        if (_botCapacity == null || _botCapacity.Length != _botX * _botY)
            _botCapacity = Resize(_botCapacity, _botX, _botY, _botX, _botY);
        if (_botHidden == null || _botHidden.Length != _botX * _botY)
            _botHidden = Resize(_botHidden, _botX, _botY, _botX, _botY);
        if (_botLock == null || _botLock.Length != _botX * _botY)
            _botLock = Resize(_botLock, _botX, _botY, _botX, _botY);
        if (_botLink == null || _botLink.Length != _botX * _botY)
            _botLink = Resize(_botLink, _botX, _botY, _botX, _botY);
    }

    string ResolveFolder() => string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder.Trim();

    string FilePath(string directory) =>
        Path.Combine(directory, ColonyLevelIO.FileName(level)).Replace('\\', '/');

    static Color LinkColor(int id) => ColonyPalette.ToColor(ColonyPalette.HexAt(Mathf.Clamp(id, 0, ColonyPalette.Count - 1)));

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

    static int CountFlags(bool[] flags)
    {
        int count = 0;
        if (flags == null) return count;

        foreach (bool flag in flags)
            if (flag) count++;
        return count;
    }

    static float Luminance(Color color) => 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
}
