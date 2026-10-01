namespace EnglishBench.Services;

public interface IAudioPlayer : IDisposable
{
    event Action<long>? Ended;
    event Action<long, string>? Failed;
    double Volume { get; set; }
    void Play(string absolutePath, long request);
    void Pause();
    void Resume();
    void Stop();
}
public enum PlaybackState { Stopped, Playing, Paused }
public sealed record AudioItem(string Path, string? Sid = null);
