# EnglishBench Application Overview

## Design goals

EnglishBench is a local Windows reading and vocabulary application. It displays a directory tree, a rendered English passage, and an editable vocabulary list in three separate panes. Content remains in ordinary JSON and MP3 files so it can be prepared outside the application and inspected without a database.

The design favors explicit responsibilities, concrete services, and small functions. It avoids a dependency-injection container, generic repository framework, event bus, and plugin architecture. A single audio-player interface is retained because it separates native media playback from deterministic playback tests.

## Framework and execution

The application uses C# and WPF, targeting `net8.0-windows`. WPF XAML defines the window, templates, and bindings. Native `RichTextBox`, `FlowDocument`, and `TextPointer` APIs implement passage rendering and selection. A WPF `Adorner` hosts the selection-add button in the same visual tree as the passage. WPF media playback supplies MP3 decoding and progress.

JSON handling uses `System.Text.Json`; files remain the storage layer. The Python application is an archived migration reference, not a runtime dependency. The WPF reader does not use an embedded browser to render the passage.

## Module responsibilities

| Module | Responsibility |
| --- | --- |
| `App.xaml` / `App.xaml.cs` | Shared brushes and button styles; startup, command-line library selection, and main-window creation |
| `MainWindow.xaml` | Three-pane layout, controls, templates, bindings, and conditional status-row visibility |
| `MainWindow.xaml.cs` | Construct the state model and media player; connect window, state, timer, and playback events |
| `MainWindow.Library.cs` | Folder selection, tree selection, library restoration, and passage-control assembly |
| `MainWindow.Vocabulary.cs` | Import confirmation, list selection, highlighting, and move/delete UI actions |
| `MainWindow.Playback.cs` | Play/pause/stop controls, enabled states, progress updates, and seeking |
| `MainWindow.Settings.cs` | Startup size, column widths, font/accent menus, six-second status expiry, persistence, and cleanup |
| `ViewModels/MainViewModel.cs` | Active library/article state; vocabulary operations; command coordination; warning/error status |
| `ViewModels/VocabularyEntryViewModel.cs` | A word's display fields, available pronunciation actions, and move/delete commands |
| `Models/` | Passage segments, SIDs, selection ranges, vocabulary entries, navigation nodes, and loaded snapshots |
| `Services/LibraryRepository.cs` | Discover marked book directories and recursively build the directory tree without parsing content |
| `Services/ArticleRepository.cs` | Parse and validate passages; load the matching vocabulary; report missing sentence audio |
| `Services/VocabularyRepository.cs` | Validate vocabulary, derive audio stems, and save with external-change detection |
| `Services/UserRepository.cs` | Read the limited account-display data from the book's existing answer sheet |
| `Services/ReaderWorkspace.cs` | Commit prepared library/article state and replace vocabulary after successful persistence |
| `Services/VocabularyMatcher.cs` | Find non-overlapping literal vocabulary matches with explicit English-word boundaries |
| `Services/AudioResources.cs` | Resolve sentence/word audio and select the first sibling MP3 for whole-article playback |
| `Services/PlaybackController.cs` / `IAudioPlayer.cs` | Playback state, target ownership, pause/resume, completion, and stale-event rejection; player boundary |
| `Rendering/ParagraphReader.cs` | Build paragraph documents; maintain SID-to-text ranges, selections, styles, and playback requests |
| `Rendering/SelectionAddAdorner.cs` | Position and show/hide the native circle-plus selection action |
| `Infrastructure/JsonFiles.cs` | JSON helpers, SHA256 fingerprints, and temporary-file replacement |
| `Infrastructure/ResourcePaths.cs` | Relative-path and reparse-point checks |
| `Infrastructure/ReaderSettings.cs` | Load and save application preferences beside the executable |
| `Infrastructure/UiIcons.cs` | Load embedded icons and create vector drawings, including disabled variants |
| `Infrastructure/WpfAudioPlayer.cs` / `RelayCommand.cs` | Native media adapter and small WPF command implementation |

The window's partial files form one class; they separate readable responsibilities without creating additional forwarding layers. Services do not reference the main window.

## Important data flows

### Library and article loading

```text
Folder selection -> LibraryRepository -> navigation nodes -> collapsed tree
Passage selection -> ArticleRepository -> prepared article and words
                  -> commit workspace -> render paragraphs and vocabulary
```

Library selection clears the active passage and words. Startup restores only a library directory, when available. Selecting an article loads its matching vocabulary. Preparation occurs before committing the article, so a failed switch retains the previous readable article.

### Vocabulary editing

```text
Selection/import/move/delete -> MainViewModel -> ReaderWorkspace
                            -> VocabularyRepository -> checked file replacement
                            -> refresh words -> update passage styles
```

All successful vocabulary commits use one refresh/notification path. Import keeps incoming metadata; later edits retain the current vocabulary metadata. Recognized extension metadata at the vocabulary root, entry, meaning, and audio levels survives serialization.

The JSON `words` array determines list order. Moving swaps neighboring positions; the first and last items disable impossible moves. Delete removes the selected entry. The selection-add button creates a word with declared pronunciation paths, but does not generate definitions or MP3 files.

Paragraph documents are rebuilt when the case-insensitive vocabulary set changes. A reorder, capitalization-only change, or duplicate-only set change updates styles without rebuilding the document. Valid text selections and SID mappings are preserved through document updates.

### Playback

```text
UI request -> audio resource lookup -> PlaybackController -> WpfAudioPlayer
Media event -> request-token check -> state update -> UI/progress update
```

The bottom play/pause action owns only the selected sibling MP3; stop is global. Sentence and word requests have separate owners. Only one target plays at a time. Old completion/failure callbacks cannot advance or stop a newer target. Progress is shown for active playback and can seek within the current file.

### Status and lifecycle

Normal startup, loading, and successful vocabulary edits produce no routine status message. Missing resources, invalid content, duplicate additions, and failed saves remain visible for six seconds. Empty status collapses the entire status row, returning its height to all three panes. A new message restarts expiry.

On close, timers stop, playback stops, and the media player is disposed. Normal application windows persist settings; test windows can disable persistence.

## Display and settings

- Startup width and height are 70% of the primary screen's working area, centered; previous window dimensions are not restored.
- The tree and title use 11 pt and 12 pt respectively. The shared body/vocabulary setting offers 11, 12, or 13 pt. Settings menus use 10 pt.
- Each paragraph has a two-em first-line indent. All panes scroll independently and column splitters allow resizing.
- The three bottom toolbars share their height and alignment. Playback controls remain present when disabled.
- Highlighting starts off. The selection-add action is a 14 by 14 DIP native vector circle-plus; it follows selection scrolling and layout and hides outside the viewport or when selection is cleared.
- Font size, accent, library, column widths, and player volume are retained as applicable. Saved passage/highlight fields do not cause article restoration or initial highlighting.

## Current boundaries

The application reads passages and vocabulary and plays existing audio. OCR, vocabulary enrichment, dictionary lookup, and audio generation are external preparation workflows. There is no paragraph-play button, vocabulary-export UI, account registration/login workflow, or exercise-answering interface.

Matching is literal and case-insensitive, not morphological: `convict` does not automatically match `convicted`, and straight/curly apostrophes are not normalized. ASCII letters, apostrophes, and hyphens participate in word boundaries; digits deliberately do not. Among matches starting at the same position, the longest wins. These details matter when authoring canonical vocabulary entries.

`EnglishBench.Tests/Prototype/` contains old rendering contract fixtures only and is not included in the published application. See [Getting Started and Testing](Getting-Started.md) for current test categories.
