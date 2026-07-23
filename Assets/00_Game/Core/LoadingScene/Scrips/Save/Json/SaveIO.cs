using System.IO;
using System.Threading;
using UnityEngine;

public static class SaveIO
{
    private const string Extension = ".sav";
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";

#if UNITY_EDITOR
    private const string FilenameSuffix = "_editor";
#else
    private const string FilenameSuffix = "";
#endif

    private static string root;
    private static string Root => root ??= Application.persistentDataPath;

    private static string GetFullPath(string filename) => Path.Combine(Root, filename + FilenameSuffix + Extension);

    public static async Awaitable WriteAsync(string filename, string content, CancellationToken token = default)
    {
        string path = GetFullPath(filename);
        string tempPath = path + TempSuffix;
        string backupPath = path + BackupSuffix;

        await File.WriteAllTextAsync(tempPath, Encrypt(content), token);

        if (File.Exists(path))
        {
            File.Copy(path, backupPath, true);
            try
            {
                File.Delete(path);
                File.Move(tempPath, path);
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backupPath))
                    File.Move(backupPath, path);
                throw;
            }
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    public static async Awaitable<string> ReadAsync(string filename, CancellationToken token = default)
    {
        string path = GetFullPath(filename);
        string backupPath = path + BackupSuffix;

        if (!File.Exists(path) && File.Exists(backupPath))
            File.Move(backupPath, path);

        if (!File.Exists(path))
            return null;

        string content = await File.ReadAllTextAsync(path, token);
        return string.IsNullOrEmpty(content) ? null : Decrypt(content);
    }

    public static void Delete(string filename)
    {
        string path = GetFullPath(filename);
        DeleteIfExists(path);
        DeleteIfExists(path + TempSuffix);
        DeleteIfExists(path + BackupSuffix);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    // TODO: Sau này cần mã hóa (XOR/AES) thì xử lý tại 2 method này
    private static string Encrypt(string plain) => plain;
    private static string Decrypt(string raw) => raw;
}
