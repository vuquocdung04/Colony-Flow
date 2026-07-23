#if UNITY_EDITOR // Bọc toàn bộ lớp này lại
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Text.RegularExpressions; // THÊM 1: Cần dùng cho Regex
using System.Collections.Generic;    // THÊM 2: Cần dùng cho List<T>

public class LocalizationImporter
{
    private static string soPath = "Assets/00_BaseGame/_DataScriptable/LocalizationData.asset";
    
    private static string localPath = "Assets/00_BaseGame/_DataScriptable/CSV/localizationCSV.csv";
    
    private static string googleSheetUrl = "https://docs.google.com/spreadsheets/d/1QdVYWCZSUDpdcWH1JoGkIA7Svf1iWrXe4Rl06xYT-0g/export?format=csv&gid=633283787";
    
    private static readonly Regex CsvSplitRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    [MenuItem("Tools/Localization/Import Google Sheet (CSV)")]
    public static async void ImportFromGoogleSheets()
    {
        if (string.IsNullOrEmpty(googleSheetUrl))
        {
            Debug.LogError("Chưa có URL Google Sheets!");
            return;
        }
        Debug.Log("Đang tải từ Google Sheets...");
        UnityWebRequest www = UnityWebRequest.Get(googleSheetUrl);
        var op = www.SendWebRequest();
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        op.completed += _ => tcs.TrySetResult(true);
        await tcs.Task;
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Lỗi: {www.error}");
            return;
        }

        // Tải về csv về local 
        string csvContent = www.downloadHandler.text;
        
        try
        {
            string directory = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            File.WriteAllText(localPath, csvContent);
            Debug.Log($"Đã lưu file local tại: {localPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi khi lưu file local: {e.Message}");
        }
        
        ImportCsv(csvContent);
    }
    
    [MenuItem("Tools/Localization/Import CSV local")]
    public static void ImportFromLocalFile()
    {
        if (!File.Exists(localPath))
        {
            Debug.LogError("Không tìm thấy file!");
            return;
        }
        ImportCsv(File.ReadAllText(localPath));
    }
    
    private static void ImportCsv(string csvContent)
    {
        LocalizationDataBase dataBase = AssetDatabase.LoadAssetAtPath<LocalizationDataBase>(soPath);
        if (dataBase == null)
        {
            dataBase = ScriptableObject.CreateInstance<LocalizationDataBase>();
            AssetDatabase.CreateAsset(dataBase, soPath);
        }

        // Dùng List để thêm dữ liệu, hiệu quả hơn
        List<TranslationEntry> newEntries = new List<TranslationEntry>();
        
        // Tách dòng an toàn hơn, xử lý cả \n và \r\n
        string[] lines = csvContent.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        
        int importedCount = 0;
        for (int i = 2; i < lines.Length; i++) // Bỏ qua 2 dòng đầu
        {
            string line = lines[i]; // Không cần Trim() ở đây
            if (string.IsNullOrEmpty(line)) continue;

            // SỬA 1: Dùng Regex để tách, thay vì line.Split(',')
            string[] values = CsvSplitRegex.Split(line);
            
            if (values.Length < 3)
            {
                Debug.LogWarning($"Dòng {i+1} có định dạng CSV không hợp lệ, bỏ qua: {line}");
                continue;
            }

            // SỬA 2: Dùng hàm CleanValue để gỡ bỏ dấu "
            TranslationEntry entry = new TranslationEntry
            {
                key = CleanValue(values[0]),
                EN = CleanValue(values[1]),
                VI = CleanValue(values[2])
            };

            newEntries.Add(entry);
            importedCount++;
        }
        
        dataBase.entries.Clear();
        dataBase.entries.AddRange(newEntries);
        
        EditorUtility.SetDirty(dataBase);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ Import thành công {importedCount} mục!");
    }
    private static string CleanValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        string result = value.Trim();
        
        if (result.Length > 1 && result.StartsWith("\"") && result.EndsWith("\""))
        {
            result = result.Substring(1, result.Length - 2);
        }
        result = result.Replace("\"\"", "\"");

        return result;
    }
}
#endif