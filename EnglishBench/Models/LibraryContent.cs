using System.ComponentModel;

namespace EnglishBench.Models;

public sealed record NavigationNode(string Name, string BookRoot, string? PassagePath, IReadOnlyList<NavigationNode> Children) : INotifyPropertyChanged
{
    public bool IsPassage => PassagePath is not null;
    private bool isExpanded;
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded == value) return;
            isExpanded = value;
            if (!value) foreach (var child in Children) child.CollapseBranch();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void CollapseBranch()
    {
        isExpanded = false;
        foreach (var child in Children) child.CollapseBranch();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
    }
}
public sealed record LibrarySnapshot(string Root, IReadOnlyList<NavigationNode> Books, IReadOnlyList<string> Warnings);
