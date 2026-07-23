using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CompactIntArrayConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        objectType == typeof(List<int>) || objectType == typeof(int[]);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        Formatting previous = writer.Formatting;
        writer.Formatting = Formatting.None;
        writer.WriteStartArray();

        if (value is List<int> list)
            foreach (int item in list) writer.WriteValue(item);
        else if (value is int[] array)
            foreach (int item in array) writer.WriteValue(item);

        writer.WriteEndArray();
        writer.Formatting = previous;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JArray token = JArray.Load(reader);
        List<int> list = new List<int>(token.Count);
        foreach (JToken item in token) list.Add(item.Value<int>());

        return objectType == typeof(int[]) ? (object)list.ToArray() : list;
    }
}

public class CompactIntMapConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(Dictionary<int, int>);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is not Dictionary<int, int> map)
        {
            writer.WriteNull();
            return;
        }

        List<int> keys = new List<int>(map.Keys);
        keys.Sort();

        Formatting previous = writer.Formatting;
        writer.Formatting = Formatting.None;
        writer.WriteStartObject();

        foreach (int key in keys)
        {
            writer.WritePropertyName(key.ToString());
            writer.WriteValue(map[key]);
        }

        writer.WriteEndObject();
        writer.Formatting = previous;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject token = JObject.Load(reader);
        Dictionary<int, int> map = new Dictionary<int, int>();
        foreach (KeyValuePair<string, JToken> pair in token)
            if (int.TryParse(pair.Key, out int key)) map[key] = pair.Value.Value<int>();

        return map;
    }
}

public static class ColonyLevelIO
{
    public const string FilePrefix = "level_";
    public const string FileExtension = ".json";

    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        Converters = new List<JsonConverter>
        {
            new CompactIntArrayConverter(),
            new CompactIntMapConverter(),
        },
    };

    public static string FileName(int level) => FilePrefix + level + FileExtension;

    public static string ToJson(ColonyLevelData data) => JsonConvert.SerializeObject(data, Settings);

    public static ColonyLevelData FromJson(string json) =>
        string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<ColonyLevelData>(json, Settings);
}
