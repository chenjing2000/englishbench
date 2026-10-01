using System.IO;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EnglishBench.Infrastructure;

public static class JsonFiles
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static string Fingerprint(string path) => Fingerprint(File.ReadAllBytes(path));
    public static string Fingerprint(byte[] content) => Convert.ToHexString(SHA256.HashData(content));
    public static string WriteAtomic(string path, object data, Action? beforeCommit = null)
    {
        string absolute = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        string temporary = absolute + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, Options);
                stream.Flush(true);
            }
            string fingerprint = Fingerprint(temporary);
            beforeCommit?.Invoke();
            File.Move(temporary, absolute, true);
            return fingerprint;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static string Text(JsonElement item, string field, bool required = true)
    {
        if (item.ValueKind != JsonValueKind.Object) throw new InvalidDataException("JSON 项必须是对象。");
        if (!item.TryGetProperty(field, out var value))
        {
            if (!required) return "";
            throw new InvalidDataException($"缺少 {field}。");
        }
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"{field} 必须是字符串。");
        return value.GetString()!;
    }
    public static JsonElement Array(JsonElement item, string field, bool nonEmpty = false)
    {
        if (item.ValueKind != JsonValueKind.Object) throw new InvalidDataException("JSON 项必须是对象。");
        if (!item.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.Array || nonEmpty && value.GetArrayLength() == 0)
            throw new InvalidDataException($"{field} 必须是{(nonEmpty ? "非空" : "")}数组。");
        return value;
    }
}
