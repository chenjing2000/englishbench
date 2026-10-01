namespace EnglishBench.Services;
using System.IO;

public sealed class PlaybackController
{
    private readonly IAudioPlayer player;
    private AudioItem[] playlist = Array.Empty<AudioItem>();
    private int index;
    private long request;
    public PlaybackController(IAudioPlayer player)
    {
        this.player = player;
        player.Ended += OnEnded;
        player.Failed += OnFailed;
    }
    public PlaybackState State { get; private set; }
    public string Owner { get; private set; } = "";
    public string? CurrentSid { get; private set; }
    public event Action? Changed;
    public event Action<string>? Message;
    public void Toggle(IReadOnlyList<AudioItem> items, string owner)
    {
        if (Owner == owner && State == PlaybackState.Playing) Pause();
        else if (Owner == owner && State == PlaybackState.Paused) Resume();
        else Play(items, owner);
    }

    public void Play(IReadOnlyList<AudioItem> items, string owner)
    {
        Stop();
        if (items.Count == 0) { Message?.Invoke("没有可播放的音频文件。"); return; }
        foreach (var item in items)
        {
            try
            {
                if (!Path.IsPathFullyQualified(item.Path) || !string.Equals(Path.GetExtension(item.Path), ".mp3", StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(item.Path) || new FileInfo(item.Path).Length == 0)
                {
                    Message?.Invoke($"{(items.Count > 1 ? "无法完整朗读，缺少或无效" : "缺少或无效")} MP3：{item.Path}");
                    return;
                }
            }
            catch (IOException error) { Message?.Invoke(error.Message); return; }
            catch (UnauthorizedAccessException error) { Message?.Invoke(error.Message); return; }
        }
        playlist = items.ToArray();
        index = 0;
        Owner = owner;
        PlayCurrent();
    }

    private void PlayCurrent()
    {
        var item = playlist[index];
        CurrentSid = item.Sid;
        State = PlaybackState.Playing;
        long token = ++request;
        try { player.Play(item.Path, token); Changed?.Invoke(); }
        catch (Exception error) when (error is IOException or InvalidOperationException or UnauthorizedAccessException)
        { OnFailed(token, error.Message); }
    }

    private void OnEnded(long token)
    {
        if (token != request || State == PlaybackState.Stopped) return;
        index++;
        if (index >= playlist.Length) Stop();
        else PlayCurrent();
    }

    private void OnFailed(long token, string error)
    {
        if (token != request || State == PlaybackState.Stopped) return;
        Stop();
        Message?.Invoke($"音频文件无法播放：{error}");
    }

    public void Pause()
    {
        if (State != PlaybackState.Playing) return;
        player.Pause();
        State = PlaybackState.Paused;
        Changed?.Invoke();
    }

    public void Resume()
    {
        if (State != PlaybackState.Paused) return;
        player.Resume();
        State = PlaybackState.Playing;
        Changed?.Invoke();
    }

    public void Stop()
    {
        request++;
        player.Stop();
        playlist = Array.Empty<AudioItem>();
        CurrentSid = null;
        Owner = "";
        State = PlaybackState.Stopped;
        Changed?.Invoke();
    }
}
