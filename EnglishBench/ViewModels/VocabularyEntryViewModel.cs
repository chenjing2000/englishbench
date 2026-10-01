using System.Windows.Input;
using EnglishBench.Infrastructure;
using EnglishBench.Models;

namespace EnglishBench.ViewModels;

public sealed class VocabularyEntryViewModel
{
    public VocabularyEntry Entry { get; }
    public string Word => Entry.Word;
    public string PhoneticUk => string.IsNullOrWhiteSpace(Entry.PhoneticUk) ? "—" : Entry.PhoneticUk;
    public string PhoneticUs => string.IsNullOrWhiteSpace(Entry.PhoneticUs) ? "—" : Entry.PhoneticUs;
    public IReadOnlyList<Meaning> Meanings => Entry.Meanings;
    public bool CanPlayUk { get; }
    public bool CanPlayUs { get; }
    public ICommand PlayUk { get; }
    public ICommand PlayUs { get; }
    public ICommand MoveUp { get; }
    public ICommand MoveDown { get; }
    public ICommand Delete { get; }

    public VocabularyEntryViewModel(VocabularyEntry entry, MainViewModel owner, bool uk, bool us)
    {
        Entry = entry;
        CanPlayUk = uk;
        CanPlayUs = us;
        PlayUk = new RelayCommand(() => owner.PlayWord(entry, "uk"), () => uk);
        PlayUs = new RelayCommand(() => owner.PlayWord(entry, "us"), () => us);
        MoveUp = new RelayCommand(() => owner.MoveWord(entry, -1), () => owner.CanMoveWord(entry, -1));
        MoveDown = new RelayCommand(() => owner.MoveWord(entry, 1), () => owner.CanMoveWord(entry, 1));
        Delete = new RelayCommand(() => owner.DeleteWord(entry), () => owner.CanWriteVocabulary);
    }
}

