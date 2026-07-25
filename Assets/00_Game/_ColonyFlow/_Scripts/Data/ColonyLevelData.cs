using System.Collections.Generic;
using Newtonsoft.Json;

public static class ColonyGridIndex
{
    public static int From(int x, int y, int gridX) => y * gridX + x;
    public static int X(int index, int gridX) => index % gridX;
    public static int Y(int index, int gridX) => index / gridX;
}

[System.Serializable]
public class ColonyRegion
{
    public int x0;
    public int y0;
    public int x1;
    public int y1;
    public string color;
    public int capacity;

    public bool Contains(int x, int y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

    public bool Overlaps(ColonyRegion other) =>
        other != null && x0 <= other.x1 && other.x0 <= x1 && y0 <= other.y1 && other.y0 <= y1;

    public List<int> Indices(int gridX)
    {
        List<int> list = new List<int>();
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                list.Add(ColonyGridIndex.From(x, y, gridX));
        return list;
    }

    public static ColonyRegion FromCorners(int ax, int ay, int bx, int by) => new ColonyRegion
    {
        x0 = ax < bx ? ax : bx,
        y0 = ay < by ? ay : by,
        x1 = ax > bx ? ax : bx,
        y1 = ay > by ? ay : by,
    };

    public static ColonyRegion FromIndices(List<int> indices, int gridX)
    {
        if (indices == null || indices.Count == 0 || gridX <= 0) return null;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (int index in indices)
        {
            int x = ColonyGridIndex.X(index, gridX);
            int y = ColonyGridIndex.Y(index, gridX);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        return new ColonyRegion { x0 = minX, y0 = minY, x1 = maxX, y1 = maxY };
    }
}

public class TopGridData
{
    [JsonProperty("gridX")] public int gridX = 24;
    [JsonProperty("gridY")] public int gridY = 24;
    [JsonProperty("colors")] public Dictionary<string, List<int>> colors = new Dictionary<string, List<int>>();
    [JsonProperty("hiddens")] public List<int> hiddens = new List<int>();
    [JsonProperty("keys")] public Dictionary<string, List<int>> keys = new Dictionary<string, List<int>>();
    [JsonProperty("hungryDoor")] public Dictionary<string, Dictionary<int, List<List<int>>>> hungryDoor = new Dictionary<string, Dictionary<int, List<List<int>>>>();
    [JsonProperty("cookie")] public Dictionary<int, List<List<int>>> cookie = new Dictionary<int, List<List<int>>>();
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

    public static ColonyLevelData FromCells(string[] topCells, bool[] topHidden, string[] topKeys,
                                            List<ColonyRegion> topDoors, List<ColonyRegion> topCookies,
                                            int topX, int topY,
                                            string[] bottomCells, int[] bottomCapacity,
                                            bool[] bottomHidden, string[] bottomLock, int[] bottomLink,
                                            int bottomX, int bottomY)
    {
        ColonyLevelData data = new ColonyLevelData();

        data.top.gridX = topX;
        data.top.gridY = topY;
        data.top.hiddens = Flags(topHidden);
        data.top.keys = ColorGroups(topKeys);
        data.top.hungryDoor = DoorGroups(topDoors, topX);
        data.top.cookie = CookieGroups(topCookies, topX);
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

    public List<ColonyRegion> TopDoorRegions()
    {
        List<ColonyRegion> regions = new List<ColonyRegion>();
        if (top.hungryDoor == null) return regions;

        foreach (KeyValuePair<string, Dictionary<int, List<List<int>>>> byColor in top.hungryDoor)
        {
            if (byColor.Value == null) continue;

            foreach (KeyValuePair<int, List<List<int>>> byCapacity in byColor.Value)
            {
                if (byCapacity.Value == null) continue;

                foreach (List<int> cells in byCapacity.Value)
                {
                    ColonyRegion region = ColonyRegion.FromIndices(cells, top.gridX);
                    if (region == null) continue;

                    region.color = byColor.Key;
                    region.capacity = byCapacity.Key;
                    regions.Add(region);
                }
            }
        }

        return regions;
    }

    public List<ColonyRegion> TopCookieRegions()
    {
        List<ColonyRegion> regions = new List<ColonyRegion>();
        if (top.cookie == null) return regions;

        foreach (KeyValuePair<int, List<List<int>>> byCapacity in top.cookie)
        {
            if (byCapacity.Value == null) continue;

            foreach (List<int> cells in byCapacity.Value)
            {
                ColonyRegion region = ColonyRegion.FromIndices(cells, top.gridX);
                if (region == null) continue;

                region.capacity = byCapacity.Key;
                regions.Add(region);
            }
        }

        return regions;
    }

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

    static Dictionary<string, Dictionary<int, List<List<int>>>> DoorGroups(List<ColonyRegion> regions, int gridX)
    {
        Dictionary<string, Dictionary<int, List<List<int>>>> groups =
            new Dictionary<string, Dictionary<int, List<List<int>>>>();
        if (regions == null) return groups;

        foreach (ColonyRegion region in Sorted(regions))
        {
            if (region == null || string.IsNullOrEmpty(region.color)) continue;

            if (!groups.TryGetValue(region.color, out Dictionary<int, List<List<int>>> byCapacity))
            {
                byCapacity = new Dictionary<int, List<List<int>>>();
                groups[region.color] = byCapacity;
            }

            if (!byCapacity.TryGetValue(region.capacity, out List<List<int>> list))
            {
                list = new List<List<int>>();
                byCapacity[region.capacity] = list;
            }

            list.Add(region.Indices(gridX));
        }

        return groups;
    }

    static Dictionary<int, List<List<int>>> CookieGroups(List<ColonyRegion> regions, int gridX)
    {
        Dictionary<int, List<List<int>>> groups = new Dictionary<int, List<List<int>>>();
        if (regions == null) return groups;

        foreach (ColonyRegion region in Sorted(regions))
        {
            if (region == null) continue;

            if (!groups.TryGetValue(region.capacity, out List<List<int>> list))
            {
                list = new List<List<int>>();
                groups[region.capacity] = list;
            }

            list.Add(region.Indices(gridX));
        }

        return groups;
    }

    static List<ColonyRegion> Sorted(List<ColonyRegion> regions)
    {
        List<ColonyRegion> copy = new List<ColonyRegion>(regions);
        copy.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;
            return a.capacity != b.capacity ? a.capacity.CompareTo(b.capacity) : a.y0 != b.y0 ? a.y0.CompareTo(b.y0) : a.x0.CompareTo(b.x0);
        });
        return copy;
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
