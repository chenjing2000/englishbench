using EnglishBench.Services;
using System.IO;
using System.Diagnostics;
using EnglishBench.ViewModels;
using EnglishBench.Infrastructure;
using EnglishBench.Models;

internal static class AudioChecks
{
    internal static void Run()
    {
        Program.Run("resource paths reject traversal and absolute references", () =>
        {
            string resourceRoot = Directory.GetCurrentDirectory();
            Program.Check(ResourcePaths.Resolve(resourceRoot, "audio_segments/s001_uk.mp3").StartsWith(resourceRoot));
            Reject("../escape.mp3");
            Reject("C:/outside.mp3");
            Reject("audio_segments/file.mp3:stream");
            void Reject(string relative)
            {
                try { ResourcePaths.Resolve(resourceRoot, relative); }
                catch (ArgumentException) { return; }
                throw new Exception("Unsafe path accepted: " + relative);
            }
        });
        var root = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "test-media", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var first = Path.Combine(root, "a.mp3");
        var second = Path.Combine(root, "b.mp3");
        File.WriteAllBytes(first, [1]);
        File.WriteAllBytes(second, [1]);
        try
        {
            Program.Run("P18 missing MP3 rejects whole playlist gracefully", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                string? warning = null;
                controller.Message += message => warning = message;
                controller.Play([new(first, "s001"), new(Path.Combine(root, "missing.mp3"), "s002")], "passage");
                Program.Check(player.Played.Count == 0 && controller.State == PlaybackState.Stopped && warning is not null);
            });
            Program.Run("paragraph sequence, pause/resume and completion", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                AudioItem[] items = [new(first, "s001"), new(second, "s002")];
                controller.Play(items, "paragraph:0");
                Program.Check(controller.CurrentSid == "s001");
                controller.Toggle(items, "paragraph:0");
                Program.Check(controller.State == PlaybackState.Paused && player.Pauses == 1);
                controller.Toggle(items, "paragraph:0");
                Program.Check(controller.State == PlaybackState.Playing && player.Resumes == 1);
                player.Complete(player.Played[^1].Request);
                Program.Check(controller.CurrentSid == "s002" && player.Played.Count == 2);
                player.Complete(player.Played[^1].Request);
                Program.Check(controller.State == PlaybackState.Stopped && controller.Owner == "");
            });
            Program.Run("P19 switching targets ignores old media callbacks", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                controller.Play([new(first, "s001"), new(second, "s002")], "passage");
                long stale = player.Played[^1].Request;
                controller.Play([new(second)], "word:uk:life");
                player.Complete(stale);
                player.Fail(stale);
                Program.Check(controller.Owner == "word:uk:life" && player.Played.Count == 2 && controller.CurrentSid is null);
                controller.Stop();
                player.Complete(player.Played[^1].Request);
                Program.Check(player.Played.Count == 2 && controller.State == PlaybackState.Stopped);
            });
            Program.Run("media failure clears playing state and reports warning", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                string? warning = null;
                controller.Message += message => warning = message;
                controller.Play([new(first, "s001")], "segment:s001");
                player.Fail(player.Played[^1].Request);
                Program.Check(controller.State == PlaybackState.Stopped && controller.CurrentSid is null && warning is not null);
            });
            Program.Run("reader rejected vocabulary audio path cancels previous playback target", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                var model = new MainViewModel(controller);
                Program.Check(model.OpenArticle(TestData.ArticlePath));
                controller.Play([new(first, "s001")], "segment:s001");
                Program.Check(controller.State == PlaybackState.Playing);
                long stale = player.Played[^1].Request;
                var invalid = new VocabularyEntry
                {
                    Word = "unsafe",
                    Audio = new AudioPaths("../escape.mp3", "../escape.mp3")
                };
                model.PlayWord(invalid, "uk");
                Program.Check(controller.State == PlaybackState.Stopped && controller.Owner == "" && controller.CurrentSid is null);
                Program.Check(player.Played.Count == 1 && model.Status.Contains("escapes"));
                player.Complete(stale);
                player.Fail(stale);
                Program.Check(controller.State == PlaybackState.Stopped && player.Played.Count == 1);
            });
            Program.Run("prototype rejected resource path cancels previous playback target", () =>
            {
                string target = Path.Combine(root, "target");
                string link = Path.Combine(root, "audio_segments");
                Directory.CreateDirectory(target);
                var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true };
                start.Arguments = $"/c mklink /J \"{link}\" \"{target}\"";
                using var process = Process.Start(start)!;
                process.WaitForExit();
                try
                {
                    if (process.ExitCode != 0) throw new Exception(process.StandardError.ReadToEnd());
                    var player = new FakePlayer();
                    var controller = new PlaybackController(player);
                    var model = new PrototypeViewModel(controller) { AudioRoot = root };
                    model.PlayExistingFile(first);
                    Program.Check(controller.State == PlaybackState.Playing);
                    model.PlayAll();
                    Program.Check(controller.State == PlaybackState.Stopped && controller.Owner == "" && controller.CurrentSid is null);
                }
                finally { if (Directory.Exists(link)) Directory.Delete(link); Directory.Delete(target); }
            });
            Program.Run("missing audio_segments never falls back to old audio directory", () =>
            {
                string directory = Path.Combine(root, "no-fallback");
                string old = Path.Combine(directory, "audio");
                Directory.CreateDirectory(old);
                File.WriteAllBytes(Path.Combine(old, "s001_uk.mp3"), [1]);
                var resources = new AudioResources();
                var segment = new Segment("s001", "test", TestData.Paths("s001"));
                string asset = resources.Segment(directory, segment, "uk");
                Program.Check(!AudioResources.IsAvailable(asset));
                Directory.CreateDirectory(Path.Combine(directory, "audio_segments"));
                File.WriteAllBytes(asset, [1]);
                Program.Check(AudioResources.IsAvailable(resources.Segment(directory, segment, "uk")));
            });
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class FakePlayer : IAudioPlayer
    {
        public event Action<long>? Ended;
        public event Action<long, string>? Failed;
        public List<(string Path, long Request)> Played { get; } = [];
        public int Pauses { get; private set; }
        public int Resumes { get; private set; }
        public double Volume { get; set; }
        public void Play(string absolutePath, long request) => Played.Add((absolutePath, request));
        public void Pause() => Pauses++;
        public void Resume() => Resumes++;
        public void Stop() { }
        public void Dispose() { }
        public void Complete(long request) => Ended?.Invoke(request);
        public void Fail(long request) => Failed?.Invoke(request, "decode failed");
    }
}
