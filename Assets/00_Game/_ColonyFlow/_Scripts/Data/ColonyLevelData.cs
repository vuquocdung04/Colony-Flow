using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
}

public class BottomGridData
{
    [JsonProperty("gridX")] public int gridX = 4;
    [JsonProperty("gridY")] public int gridY = 2;
    [JsonProperty("colors")] public Dictionary<string, Dictionary<int, int>> colors = new Dictionary<string, Dictionary<int, int>>();
    [JsonProperty("blinds")] public JArray blinds = new JArray();
    [JsonProperty("ices")] public JObject ices = new JObject();
    [JsonProperty("tunnels")] public JObject tunnels = new JObject();
    [JsonProperty("links")] public JArray links = new JArray();
}

public class ColonyLevelData
{
    [JsonProperty("top")] public TopGridData top = new TopGridData();
    [JsonProperty("bottom")] public BottomGridData bottom = new BottomGridData();

    public static ColonyLevelData FromCells(string[] topCells, int topX, int topY,
                                            string[] bottomCells, int[] bottomCapacity, int bottomX, int bottomY)
    {
        ColonyLevelData data = new ColonyLevelData();

        data.top.gridX = topX;
        data.top.gridY = topY;
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

        return data;
    }

    public string[] TopToCells()
    {
        string[] cells = new string[AtLeastOne(top.gridX) * AtLeastOne(top.gridY)];
        if (top.colors == null) return cells;

        foreach (KeyValuePair<string, List<int>> pair in top.colors)
        {
            if (pair.Value == null) continue;
            foreach (int index in pair.Value)
                if (index >= 0 && index < cells.Length) cells[index] = pair.Key;
        }
        return cells;
    }

    public void BottomToCells(out string[] cells, out int[] capacity)
    {
        int count = AtLeastOne(bottom.gridX) * AtLeastOne(bottom.gridY);
        cells = new string[count];
        capacity = new int[count];
        if (bottom.colors == null) return;

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

    static int AtLeastOne(int value) => value > 0 ? value : 1;
}
