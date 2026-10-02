using System.IO;
using System.Text.Json;
namespace EnglishBench.Infrastructure;

public sealed class ReaderSettings
{
    public double LeftWidth { get; set; } = 310;
    public double RightWidth { get; set; } = 390;
    public double FontSize { get; set; } = 11.0 * 96 / 72;
    public double Volume { get; set; } = 0.65;
    public string Accent { get; set; } = "uk";
    public double ForwardStep { get; set; } = 1.0;
    public double BackwardStep { get; set; } = 2.0;
    public string? Library { get; set; }
    public static string PathName => Path.Combine(AppContext.BaseDirectory, "reader-settings.json");
    public static ReaderSettings Load()
    {
        try
        {
            if (!File.Exists(PathName)) return new ReaderSettings();
            return JsonSerializer.Deserialize<ReaderSettings>(File.ReadAllText(PathName)) ?? new ReaderSettings();
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return new ReaderSettings();
        }
    }
    public void Save() => JsonFiles.WriteAtomic(PathName, this);
}
