
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class JsonSaveSystem
{
    public static void Save<T>(T data, string fileName)
    {
        string path = GetPath(fileName);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, json);
        Debug.Log($"Da luu: {path}");
    }

    // C# thuong
    public static T Load<T>(string fileName) where T : new()
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
            {
                return new T();
            }
            T data = JsonConvert.DeserializeObject<T>(json);
            if (data == null)
            {
                return new T();
            }
            return data;
        }
        return new T();
    }
    
    public static void Delete(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"da xoa: {fileName}");
        }
    }
    
    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + ".json");
    }
}