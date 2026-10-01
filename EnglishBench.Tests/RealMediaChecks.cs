using EnglishBench.Infrastructure;
using EnglishBench.Services;

internal static class RealMediaChecks
{
    internal static void Run()
    {
        foreach (string accent in new[] { "uk", "us" })
        {
            Program.Run($"real MP3 segment {accent}: decode, position, pause/resume and end", () =>
            {
                var article = new ArticleRepository().Load(TestData.ArticlePath);
                string path = new AudioResources().Segment(article.Directory, article.Segments.First(), accent);
                Verify(path, true);
            });
            Program.Run($"real MP3 vocabulary {accent}: decode, position and end", () =>
            {
                var article = new ArticleRepository().Load(TestData.ArticlePath);
                string path = new AudioResources().Vocabulary(article.Directory, article.Vocabulary.Data.Words[0], accent);
                Verify(path, false);
            });
        }
        Program.Run("real MP3 playlist advances SID and stops after final audio", () =>
        {
            var article = new ArticleRepository().Load(TestData.ArticlePath);
            var resources = new AudioResources();
            using var player = new WpfAudioPlayer { Volume = 0 };
            var controller = new PlaybackController(player);
            string? error = null;
            int opened = 0;
            player.Opened += _ => opened++;
            controller.Message += message => error = message;
            var segments = article.Segments.Take(2).ToArray();
            controller.Play(segments.Select(s => new AudioItem(resources.Segment(article.Directory, s, "uk"), s.Sid)).ToArray(), "paragraph:0");
            WpfTestHelpers.Wait(() => opened == 1 || error is not null);
            Program.Check(error is null && controller.CurrentSid == "s001");
            player.Position = player.Duration - TimeSpan.FromMilliseconds(100);
            WpfTestHelpers.Wait(() => opened == 2 || error is not null);
            Program.Check(error is null && controller.CurrentSid == "s002");
            player.Position = player.Duration - TimeSpan.FromMilliseconds(100);
            WpfTestHelpers.Wait(() => controller.State == PlaybackState.Stopped || error is not null);
            Program.Check(error is null && controller.CurrentSid is null);
        });
    }
    private static void Verify(string path, bool pause)
    {
        using var player = new WpfAudioPlayer { Volume = 0 };
        bool opened = false;
        bool ended = false;
        string? error = null;
        player.Opened += token => opened = token == 42;
        player.Ended += token => ended = token == 42;
        player.Failed += (_, message) => error = message;
        player.Play(path, 42);
        WpfTestHelpers.Wait(() => opened || error is not null);
        Program.Check(error is null && player.Duration > TimeSpan.Zero);
        WpfTestHelpers.Wait(() => player.Position > TimeSpan.FromMilliseconds(120) || error is not null);
        Program.Check(error is null);
        if (pause)
        {
            player.Pause();
            WpfTestHelpers.Pump(120);
            var before = player.Position;
            WpfTestHelpers.Pump(250);
            Program.Check(Math.Abs((player.Position - before).TotalMilliseconds) < 60);
            player.Resume();
            WpfTestHelpers.Wait(() => player.Position > before + TimeSpan.FromMilliseconds(100));
        }
        player.Position = player.Duration - TimeSpan.FromMilliseconds(100);
        WpfTestHelpers.Wait(() => ended || error is not null);
        Program.Check(ended && error is null);
    }
}
