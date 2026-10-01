using System.Text.Json.Serialization;
namespace EnglishBench.Models;

public sealed record AudioPaths(
    [property: JsonPropertyName("uk")] string Uk,
    [property: JsonPropertyName("us")] string Us)
{
    [JsonExtensionData] public Dictionary<string, System.Text.Json.JsonElement>? Extra { get; init; }
    public string For(string accent)
    {
        if (accent == "uk") return Uk;
        if (accent == "us") return Us;
        throw new ArgumentException("Accent must be uk or us.");
    }
}
public sealed record Segment(
    [property: JsonPropertyName("sid")] string Sid,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("audio")] AudioPaths? Audio = null);
public sealed record VocabularyMatch(int Start, int Length, string Key);
// This offset refers to rendered text; blank placeholders have a different display length.
public sealed record PassageSelection(string Sid, string Text, int RenderedOffset, int Length);
