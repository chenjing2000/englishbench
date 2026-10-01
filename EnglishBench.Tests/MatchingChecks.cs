using EnglishBench.Services;

internal static class MatchingChecks
{
    internal static void Run()
    {
        Program.Run("P11 earliest match and same-start longest phrase", () =>
        {
            var hits = new VocabularyMatcher().Match("PUBLIC life, take responsibility. republican public.", ["public", "public life", "responsibility", "take responsibility"]);
            Program.Check(hits.Count == 3 && hits[0].Key == "public life" && hits[1].Key == "take responsibility" && hits[2].Key == "public");
        });
        Program.Run("P12/P13 apostrophe, curly apostrophe and hyphen boundaries", () =>
        {
            var hits = new VocabularyMatcher().Match("politician's politician’s politician public-life", ["politician", "public", "life"]);
            Program.Check(hits.Count == 1 && hits[0].Start == 26);
        });
        Program.Run("Python boundaries deliberately exclude digits", () => Program.Check(new VocabularyMatcher().Match("public2 2public", ["public"]).Count == 2));
        Program.Run("matching does not normalize curly apostrophes into straight", () => Program.Check(new VocabularyMatcher().Match("politician’s politician's", ["politician's"]).Count == 1));
    }
}
