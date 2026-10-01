using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.IO;
using EnglishBench.Models;
using EnglishBench.Services;
using EnglishBench.Infrastructure;

namespace EnglishBench.ViewModels;

public sealed class PrototypeViewModel : INotifyPropertyChanged
{
    private readonly PlaybackController playback;
    private string status = "中央阅读区原型：左键选词，右键播放 Segment。词表更改只保存在本次会话。";
    private string accent = "uk";
    private bool highlightsVisible = true;
    private string audioRoot = Directory.GetCurrentDirectory();
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? VocabularyChanged;
    public event Action? PlaybackChanged;
    public ObservableCollection<string> Words { get; } = ["character", "public", "public life", "responsibility", "take responsibility"];
    public IReadOnlyList<Segment[]> Paragraphs { get; } =
    [
        [new("s001", "Character matters in public life.", Paths("s001")),
         new("s002", "A politician's words and actions may shape public trust.", Paths("s002"))],
        [new("s003", "People sometimes take responsibility for their mistakes,", Paths("s003")),
         new("s004", "but public life also requires judgment and restraint.", Paths("s004"))],
        [new("s005", "Public life requires trust.", Paths("s005")),
         new("s006", "Public life requires trust.", Paths("s006"))]
    ];
    public Segment[] BlankParagraph { get; } = [new("s007", "Good character [[1]] an important role in public life.")];

    public PrototypeViewModel(PlaybackController playback)
    {
        this.playback = playback;
        playback.Message += message => Status = message;
        playback.Changed += () =>
        {
            Changed(nameof(PlaybackDescription));
            PlaybackChanged?.Invoke();
        };
    }
    public string Status { get => status; set { status = value; Changed(); } }
    public string Accent
    {
        get => accent;
        set
        {
            if (accent == value) return;
            if (value is not ("uk" or "us")) throw new ArgumentException("Invalid accent.");
            playback.Stop();
            accent = value;
            Changed();
            Status = value == "uk" ? "British / UK" : "American / US";
        }
    }
    public bool HighlightsVisible
    {
        get => highlightsVisible;
        set { highlightsVisible = value; Changed(); VocabularyChanged?.Invoke(); }
    }
    public string AudioRoot
    {
        get => audioRoot;
        set { playback.Stop(); audioRoot = Path.GetFullPath(value); Changed(); Status = $"音频根目录：{audioRoot}"; }
    }
    public string? PlayingSid => playback.CurrentSid;
    public string PlaybackDescription => $"{playback.State}  {playback.Owner}  {playback.CurrentSid}";

    public void AddSelection(PassageSelection selection)
    {
        string text = Regex.Replace(selection.Text, @"\s+", " ").Trim();
        if (Words.Contains(text, StringComparer.OrdinalIgnoreCase)) { Status = $"词表中已存在：{text}"; return; }
        Words.Add(text);
        VocabularyChanged?.Invoke();
        Status = $"已加入本次会话词表：{text}（{selection.Sid}）";
    }
    public void PlaySegment(string sid)
    {
        var segment = Paragraphs.SelectMany(p => p).Single(s => s.Sid == sid);
        RunAudio(() => playback.Play([Item(segment)], $"segment:{sid}"));
    }
    public void PlayParagraph(int index) => RunAudio(() => playback.Toggle(Paragraphs[index].Select(Item).ToArray(), $"paragraph:{index}"));
    public void PlayAll() => RunAudio(() => playback.Toggle(Paragraphs.SelectMany(p => p).Select(Item).ToArray(), "passage"));
    public void PlayExistingFile(string path) => RunAudio(() => playback.Play([new(path)], "local-file"));
    public void Pause() => playback.Pause();
    public void Resume() => playback.Resume();
    public void Stop() => playback.Stop();
    private AudioItem Item(Segment segment) => new(ResourcePaths.Resolve(AudioRoot, segment.Audio!.For(Accent)), segment.Sid);
    private void RunAudio(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        { playback.Stop(); Status = error.Message; }
    }
    private static AudioPaths Paths(string sid) => new($"audio_segments/{sid}_uk.mp3", $"audio_segments/{sid}_us.mp3");
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
