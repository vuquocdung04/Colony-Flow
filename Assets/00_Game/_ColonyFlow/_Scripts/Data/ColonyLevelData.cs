using System.Collections.Generic;
using Newtonsoft.Json;

public static class ColonyGridIndex
{
    public static int From(int x, int y, int gridX) => y * gridX + x;
    public static int X(int index, int gridX) => index % gridX;
    public static int Y(int index, int gridX) => index / gridX;
}

public class TopGridData
{
    [JsonProperty("gridX")] public int gridX = 24;
    [JsonProperty("gridY")] public int gridY = 24;
    [JsonProperty("colors")] public Dictionary<string, List<int>> colors = new Dictionary<string, List<int>>();
    [JsonProperty("hiddens")] public List<int> hiddens = new List<int>();
    [JsonProperty("keys")] public Dictionary<string, List<int>> keys = new Dictionary<string, List<int>>();
}

public class BottomGridData
{
    [JsonProperty("gridX")] public int gridX = 4;
    [JsonProperty("gridY")] public int gridY = 2;
    [JsonProperty("colors")] public Dictionary<string, Dictionary<int, int>> colors = new Dictionary<string, Dictionary<int, int>>();
    [JsonProperty("hiddens")] public List<int> hiddens = new List<int>();
    [JsonProperty("locks")] public Dictionary<string, List<int>> locks = new Dictionary<string, List<int>>();
    [JsonProperty("links")] public List<List<int>> links = new List<List<int>>();
}

public class ColonyLevelData
{
    [JsonProperty("top")] public TopGridData top = new TopGridData();
    [JsonProperty("bottom")] public BottomGridData bottom = new BottomGridData();

    public static ColonyLevelData FromCells(string[] topCells, bool[] topHidden, string[] topKeys, int topX, int topY,
                                            string[] bottomCells, int[] bottomCapacity,
                                            bool[] bottomHidden, string[] bottomLock, int[] bottomLink,
                                            int bottomX, int bottomY)
    {
        ColonyLevelData data = new ColonyLevelData();

        data.top.gridX = topX;
        data.top.gridY = topY;
        data.top.hiddens = Flags(topHidden);
        data.top.keys = ColorGroups(topKeys);
        if (topCells != null)
        {
            for (int i = 0; i < topCells.Length; i++)
            {
                string hex = topCells[i];
                if (string.IsNullOrEmpty(hex)) continue;
                if (!data.top.colors.TryGetValue(hex, out List<int> list))
                {
                    list = new List<int>();
                    data.top.colors[hex] = list;
                }
                list.Add(i);
            }
        }

        data.bottom.gridX = bottomX;
        data.bottom.gridY = bottomY;
        if (bottomCells != null)
        {
            for (int i = 0; i < bottomCells.Length; i++)
            {
                string hex = bottomCells[i];
                if (string.IsNullOrEmpty(hex)) continue;
                if (!data.bottom.colors.TryGetValue(hex, out Dictionary<int, int> slots))
                {
                    slots = new Dictionary<int, int>();
                    data.bottom.colors[hex] = slots;
                }
                slots[i] = bottomCapacity != null && i < bottomCapacity.Length ? bottomCapacity[i] : 0;
            }
        }

        data.bottom.hiddens = Flags(bottomHidden);
        data.bottom.locks = ColorGroups(bottomLock);
        data.bottom.links = Groups(bottomLink);

        return data;
    }

    public string[] TopToCells() => CellsFrom(top.colors, top.gridX, top.gridY);

    public string[] TopKeysToCells() => CellsFrom(top.keys, top.gridX, top.gridY);

    public bool[] TopHiddenFlags()
    {
        bool[] flags = new bool[AtLeastOne(top.gridX) * AtLeastOne(top.gridY)];
        ApplyFlags(top.hiddens, flags);
        return flags;
    }

    public void BottomToCells(out string[] cells, out int[] capacity,
                              out bool[] hidden, out string[] locks, out int[] links)
    {
        int count = AtLeastOne(bottom.gridX) * AtLeastOne(bottom.gridY);
        cells = new string[count];
        capacity = new int[count];
        hidden = new bool[count];
        links = new int[count];

        if (bottom.colors != null)
        {
            foreach (KeyValuePair<string, Dictionary<int, int>> pair in bottom.colors)
            {
                if (pair.Value == null) continue;
                foreach (KeyValuePair<int, int> slot in pair.Value)
                {
                    if (slot.Key < 0 || slot.Key >= count) continue;
                    cells[slot.Key] = pair.Key;
                    capacity[slot.Key] = slot.Value;
                }
            }
        }

        ApplyFlags(bottom.hiddens, hidden);
        locks = ColorCells(bottom.locks, count);

        if (bottom.links == null) return;

        for (int group = 0; group < bottom.links.Count; group++)
        {
            List<int> members = bottom.links[group];
            if (members == null) continue;

            foreach (int index in members)
                if (index >= 0 && index < count) links[index] = group + 1;
        }
    }

    static string[] CellsFrom(Dictionary<string, List<int>> colors, int gridX, int gridY)
    {
        string[] cells = new string[AtLeastOne(gridX) * AtLeastOne(gridY)];
        if (colors == null) return cells;

        foreach (KeyValuePair<string, List<int>> pair in colors)
        {
            if (pair.Value == null) continue;
            foreach (int index in pair.Value)
                if (index >= 0 && index < cells.Length) cells[index] = pair.Key;
        }
        return cells;
    }

    static string[] ColorCells(Dictionary<string, List<int>> colors, int count)
    {
        string[] cells = new string[count];
        if (colors == null) return cells;

        foreach (KeyValuePair<string, List<int>> pair in colors)
        {
            if (pair.Value == null) continue;
            foreach (int index in pair.Value)
                if (index >= 0 && index < count) cells[index] = pair.Key;
        }
        return cells;
    }

    static Dictionary<string, List<int>> ColorGroups(string[] source)
    {
        Dictionary<string, List<int>> groups = new Dictionary<string, List<int>>();
        if (source == null) return groups;

        for (int i = 0; i < source.Length; i++)
        {
            string hex = source[i];
            if (string.IsNullOrEmpty(hex)) continue;

            if (!groups.TryGetValue(hex, out List<int> list))
            {
                list = new List<int>();
                groups[hex] = list;
            }
            list.Add(i);
        }
        return groups;
    }

    static List<int> Flags(bool[] source)
    {
        List<int> list = new List<int>();
        if (source == null) return list;

        for (int i = 0; i < source.Length; i++)
            if (source[i]) list.Add(i);

        return list;
    }

    static List<List<int>> Groups(int[] source)
    {
        List<List<int>> groups = new List<List<int>>();
        if (source == null) return groups;

        SortedDictionary<int, List<int>> byId = new SortedDictionary<int, List<int>>();
        for (int i = 0; i < source.Length; i++)
        {
            int id = source[i];
            if (id <= 0) continue;

            if (!byId.TryGetValue(id, out List<int> members))
            {
                members = new List<int>();
                byId[id] = members;
            }
            members.Add(i);
        }

        foreach (KeyValuePair<int, List<int>> pair in byId) groups.Add(pair.Value);
        return groups;
    }

    static void ApplyFlags(List<int> source, bool[] target)
    {
        if (source == null) return;

        foreach (int index in source)
            if (index >= 0 && index < target.Length) target[index] = true;
    }

    static int AtLeastOne(int value) => value > 0 ? value : 1;
}
