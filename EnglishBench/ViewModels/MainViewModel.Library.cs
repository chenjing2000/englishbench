using EnglishBench.Models;
using EnglishBench.Services;

namespace EnglishBench.ViewModels;

public sealed partial class MainViewModel
{
    public bool OpenLibrary(string root)
    {
        if (CanLeaveArticle?.Invoke() == false) return false;
        return Handle(() =>
        {
            workspace.OpenLibrary(root);
            playback.Stop();
            Books.Clear();
            foreach (var book in workspace.Library!.Books) Books.Add(book);
            Words.Clear();
            Changed(nameof(ArticleTitle));
            Status = string.Join("  ", workspace.Library.Warnings);
            ArticleChanged?.Invoke();
        });
    }

    public bool OpenArticle(string path)
    {
        bool opened = false;
        bool loaded = Handle(() =>
        {
            var prepared = new ArticleRepository().Load(path);
            var words = PrepareWords(prepared);
            if (CanLeaveArticle?.Invoke() == false) return;
            workspace.CommitArticle(prepared);
            playback.Stop();
            Words.Clear();
            foreach (var word in words) Words.Add(word);
            Changed(nameof(ArticleTitle));
            Status = string.Join("  ", Article!.Warnings);
            ArticleChanged?.Invoke();
            opened = true;
        });
        return loaded && opened;
    }

    public static IEnumerable<NavigationNode> AllLeaves(IEnumerable<NavigationNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsPassage) yield return node;
            else
            {
                foreach (var leaf in AllLeaves(node.Children)) yield return leaf;
            }
        }
    }
}
