using System.ComponentModel;
using System.Windows.Threading;

namespace ReadArticles;

public partial class MainWindow
{
    private readonly DispatcherTimer statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };

    private void InitializeStatus()
    {
        ViewModel.PropertyChanged += ViewModelPropertyChanged;
        statusTimer.Tick += StatusTimerTick;
        RestartStatusTimer();
    }

    private void CloseStatus()
    {
        statusTimer.Stop();
        statusTimer.Tick -= StatusTimerTick;
        ViewModel.PropertyChanged -= ViewModelPropertyChanged;
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Status)) RestartStatusTimer();
    }

    private void StatusTimerTick(object? sender, EventArgs e)
    {
        statusTimer.Stop();
        ViewModel.Status = "";
    }

    private void RestartStatusTimer()
    {
        statusTimer.Stop();
        if (!string.IsNullOrEmpty(ViewModel.Status)) statusTimer.Start();
    }
}
