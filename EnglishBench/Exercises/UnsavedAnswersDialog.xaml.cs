using System.Windows;

namespace EnglishBench.Exercises;

public partial class UnsavedAnswersDialog : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public UnsavedAnswersDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        DialogResult = true;
    }

    private void DiscardClicked(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        DialogResult = true;
    }

    private void CancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;
}
