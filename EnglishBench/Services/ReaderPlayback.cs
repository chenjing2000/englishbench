using System.IO;
using EnglishBench.Models;

namespace EnglishBench.Services;

public sealed class ReaderPlayback
{
    private const string ArticleOwner = "passage";
    private readonly PlaybackController controller;
    private readonly AudioResources resources = new AudioResources();

    public ReaderPlayback(PlaybackController controller)
    {
        this.controller = controller;
        controller.Changed += () => Changed?.Invoke();
        controller.Message += message => Message?.Invoke(message);
    }

    public event Action? Changed;
    public event Action<string>? Message;
    public PlaybackState State => controller.State;
    public string Owner => controller.Owner;
    public string? CurrentSid => controller.CurrentSid;
    public bool IsArticleActive => Owner == ArticleOwner && State != PlaybackState.Stopped;
    public bool IsArticlePlaying => IsArticleActive && State == PlaybackState.Playing;
    public bool IsArticlePaused => IsArticleActive && State == PlaybackState.Paused;
    public bool CanSeekArticle => IsArticleActive && controller.IsOpen && controller.Duration > TimeSpan.Zero;
    public double ArticleDuration => CanSeekArticle ? controller.Duration.TotalSeconds : 0;
    public double ArticlePosition => CanSeekArticle ? controller.Position.TotalSeconds : 0;

    public void ToggleArticle(LoadedArticle? article)
    {
        Handle(() =>
        {
            if (controller.ToggleCurrent(ArticleOwner)) return;
            string path = resources.ArticleFile(RequireArticle(article).Directory);
            controller.Play(new[] { new AudioItem(path) }, ArticleOwner);
        }, false);
    }

    public void StopArticle()
    {
        if (IsArticleActive) controller.Stop();
    }

    public void SeekArticle(double seconds)
    {
        if (CanSeekArticle) controller.Seek(seconds);
    }

    public void PlaySegment(LoadedArticle? article, string sid, string accent)
    {
        Handle(() =>
        {
            var current = RequireArticle(article);
            var segment = current.Segments.Single(s => s.Sid == sid);
            string path = resources.Segment(current.Directory, segment, accent);
            controller.Play(new[] { new AudioItem(path, sid) }, "segment:" + sid);
        });
    }

    public bool CanPlaySegment(LoadedArticle? article, string sid, string accent)
    {
        var segment = article?.Segments.FirstOrDefault(s => s.Sid == sid);
        return segment?.Audio != null && AudioResources.IsAvailable(resources.Segment(article!.Directory, segment, accent));
    }

    public void PlayWord(LoadedArticle? article, VocabularyEntry entry, string accent)
    {
        Handle(() =>
        {
            string path = resources.Vocabulary(RequireArticle(article).Directory, entry, accent);
            controller.Play(new[] { new AudioItem(path) }, "word:" + accent + ":" + entry.Word);
        });
    }

    public bool CanPlayWord(LoadedArticle article, VocabularyEntry entry, string accent)
    {
        try
        {
            return AudioResources.IsAvailable(resources.Vocabulary(article.Directory, entry, accent));
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void StopWord(VocabularyEntry entry)
    {
        if (Owner == "word:uk:" + entry.Word || Owner == "word:us:" + entry.Word) controller.Stop();
    }

    public void StopPronunciation()
    {
        if (!IsArticleActive) controller.Stop();
    }

    public void Stop() => controller.Stop();

    private static LoadedArticle RequireArticle(LoadedArticle? article) =>
        article ?? throw new InvalidOperationException("请先打开文章。");

    private void Handle(Action action, bool stopOnFailure = true)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
        {
            // A missing sibling MP3 leaves pronunciation playing; failed pronunciation cancels its previous target.
            if (stopOnFailure) controller.Stop();
            Message?.Invoke(error.Message);
        }
    }
}
