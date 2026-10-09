using System.IO;
using System.Text.Json;
using ReadArticles.Infrastructure;

namespace ReadArticles.Services;

public sealed class UserRepository
{
    public string ReadUser(string bookRoot)
    {
        try
        {
            string sheet = ResourcePaths.Resolve(bookRoot, "userdata/xiaoxin/answer_sheet.json");
            if (!File.Exists(sheet)) return "User: —";
            using var json = JsonDocument.Parse(File.ReadAllText(sheet));
            return "User: " + JsonFiles.Text(json.RootElement, "username");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException or UnauthorizedAccessException or InvalidOperationException)
        {
            return "User: —（账户数据无法读取）";
        }
    }
}
