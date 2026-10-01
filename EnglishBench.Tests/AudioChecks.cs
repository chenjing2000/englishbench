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
            Program.Run("reader playback exposes progress and seeking only for article ownership", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                var playback = new ReaderPlayback(controller);
                controller.Play(new[] { new AudioItem(first) }, "passage");
                Program.Check(playback.IsArticlePlaying && playback.CanSeekArticle && playback.ArticleDuration == 10);
                playback.SeekArticle(20);
                Program.Check(playback.ArticlePosition == 10);
                playback.SeekArticle(-1);
                Program.Check(playback.ArticlePosition == 0);
                controller.Pause();
                playback.SeekArticle(4);
                Program.Check(playback.IsArticlePaused && playback.ArticlePosition == 4);
                foreach (string owner in new[] { "word:uk:life", "segment:s001" })
                {
                    controller.Play(new[] { new AudioItem(second) }, owner);
                    player.Position = TimeSpan.FromSeconds(2);
                    playback.SeekArticle(8);
                    playback.StopArticle();
                    Program.Check(!playback.IsArticleActive && !playback.CanSeekArticle && playback.ArticlePosition == 0 && playback.ArticleDuration == 0);
                    Program.Check(controller.State == PlaybackState.Playing && player.Position.TotalSeconds == 2);
                }
                controller.Stop();
                playback.SeekArticle(5);
                Program.Check(!playback.CanSeekArticle && player.Position == TimeSpan.Zero);
            });
            Program.Run("stale media-open callbacks do not refresh a newer target", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                int changes = 0;
                controller.Changed += () => changes++;
                controller.Play(new[] { new AudioItem(first) }, "passage");
                long stale = player.Played.Last().Request;
                controller.Play(new[] { new AudioItem(second) }, "word:uk:life");
                int before = changes;
                player.Open(stale);
                Program.Check(changes == before && controller.Owner == "word:uk:life");
                player.Open(player.Played.Last().Request);
                Program.Check(changes == before + 1);
                controller.Stop();
                before = changes;
                player.Open(player.Played.Last().Request);
                Program.Check(changes == before);
            });
            Program.Run("deleting a word cancels only that word's pronunciation", () =>
            {
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                var playback = new ReaderPlayback(controller);
                var entry = VocabularyRepository.NewEntry("life");
                foreach (string owner in new[] { "passage", "segment:s001", "word:uk:afterlife" })
                {
                    controller.Play(new[] { new AudioItem(first) }, owner);
                    playback.StopWord(entry);
                    Program.Check(controller.Owner == owner && controller.State == PlaybackState.Playing);
                }
                foreach (string accent in new[] { "uk", "us" })
                {
                    controller.Play(new[] { new AudioItem(first) }, "word:" + accent + ":life");
                    playback.StopWord(entry);
                    Program.Check(controller.State == PlaybackState.Stopped);
                }
            });
            Program.Run("article controls leave pronunciation alone and keep pause/resume ownership", () =>
            {
                string passage = Path.Combine(root, "Reading.json");
                File.Copy(TestData.ArticlePath, passage);
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                var model = new MainViewModel(controller);
                Program.Check(model.OpenArticle(passage));
                controller.Play(new[] { new AudioItem(second) }, "word:uk:life");
                model.StopArticleAudio();
                Program.Check(controller.Owner == "word:uk:life" && controller.State == PlaybackState.Playing);
                model.ToggleArticleAudio();
                Program.Check(controller.Owner == "passage" && player.Played.Last().Path == first);
                model.ToggleArticleAudio();
                Program.Check(controller.State == PlaybackState.Paused && player.Pauses == 1);
                model.Accent = "us";
                Program.Check(controller.State == PlaybackState.Paused);
                model.ToggleArticleAudio();
                Program.Check(controller.State == PlaybackState.Playing && player.Resumes == 1);
                model.StopArticleAudio();
                Program.Check(controller.State == PlaybackState.Stopped);
                controller.Play(new[] { new AudioItem(second, "s001") }, "segment:s001");
                model.StopArticleAudio();
                Program.Check(controller.State == PlaybackState.Playing);
                model.Accent = "uk";
                Program.Check(controller.State == PlaybackState.Stopped);
            });
            Program.Run("missing sibling MP3 preserves pronunciation but missing segment cancels it", () =>
            {
                string directory = Path.Combine(root, "no-sibling");
                Directory.CreateDirectory(directory);
                string passage = Path.Combine(directory, "Reading.json");
                File.Copy(TestData.ArticlePath, passage);
                var player = new FakePlayer();
                var controller = new PlaybackController(player);
                var model = new MainViewModel(controller);
                Program.Check(model.OpenArticle(passage));
                controller.Play(new[] { new AudioItem(first) }, "word:uk:life");
                model.ToggleArticleAudio();
                Program.Check(controller.Owner == "word:uk:life" && controller.State == PlaybackState.Playing && model.Status.Contains("MP3"));
                model.PlaySegment("s001");
                Program.Check(controller.State == PlaybackState.Stopped && model.Status.Contains("MP3"));
            });
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
        public event Action<long>? Opened;
        public event Action<long>? Ended;
        public event Action<long, string>? Failed;
        public List<(string Path, long Request)> Played { get; } = [];
        public int Pauses { get; private set; }
        public int Resumes { get; private set; }
        public double Volume { get; set; }
        public bool IsOpen { get; private set; }
        public TimeSpan Position { get; set; }
        public TimeSpan Duration { get; } = TimeSpan.FromSeconds(10);
        public void Play(string absolutePath, long request)
        {
            Played.Add((absolutePath, request));
            IsOpen = true;
            Opened?.Invoke(request);
        }
        public void Pause() => Pauses++;
        public void Resume() => Resumes++;
        public void Stop() { IsOpen = false; Position = TimeSpan.Zero; }
        public void Dispose() { }
        public void Complete(long request) => Ended?.Invoke(request);
        public void Open(long request) => Opened?.Invoke(request);
        public void Fail(long request) => Failed?.Invoke(request, "decode failed");
    }
}
