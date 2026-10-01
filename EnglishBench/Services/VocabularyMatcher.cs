using EnglishBench.Models;
namespace EnglishBench.Services;
public sealed class VocabularyMatcher
{
    public IReadOnlyList<VocabularyMatch> Match(string text, IEnumerable<string> words)
    {
        var candidates = words.Select(w => w.Trim()).Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var matches = new List<VocabularyMatch>();
        int cursor = 0;
        while (cursor < text.Length)
        {
            int bestStart = -1;
            string? bestWord = null;
            foreach (string word in candidates)
            {
                int start = FindWholeWord(text, word, cursor);
                if (start >= 0 && (bestStart < 0 || start < bestStart ||
                    start == bestStart && word.Length > bestWord!.Length))
                {
                    bestStart = start;
                    bestWord = word;
                }
            }
            if (bestWord is null) break;
            matches.Add(new VocabularyMatch(bestStart, bestWord.Length, bestWord));
            cursor = bestStart + bestWord.Length;
        }
        return matches;
    }

    private static int FindWholeWord(string text, string word, int cursor)
    {
        while (cursor <= text.Length - word.Length)
        {
            int start = text.IndexOf(word, cursor, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return -1;
            int end = start + word.Length;
            if ((start == 0 || !IsEnglishWordCharacter(text[start - 1])) &&
                (end == text.Length || !IsEnglishWordCharacter(text[end]))) return start;
            cursor = start + 1;
        }
        return -1;
    }

    // Reproduce runtime.js exactly: digits are not English boundary characters.
    private static bool IsEnglishWordCharacter(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '\'' || c == '’' || c == '-';
}
