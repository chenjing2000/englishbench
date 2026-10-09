using System.Windows;
namespace ReadArticles;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow();
        window.Show();
        window.RestoreContent(Argument("--library"));
        string? Argument(string name)
        {
            int index = Array.IndexOf(e.Args, name);
            return index >= 0 && index + 1 < e.Args.Length ? e.Args[index + 1] : null;
        }
    }
}
