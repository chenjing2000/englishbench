# Getting Started and Testing

## Requirements and launch

Run EnglishBench on Windows with the .NET 10 Windows Desktop Runtime, version `10.0.11` or a later compatible patch. Building also requires a .NET SDK compatible with `global.json`, currently SDK `10.0.400` with `latestPatch` roll-forward. Both the application and its tests target `net10.0-windows`.

The published application lives in `artifacts/app/`. Keep `EnglishBench.exe`, `EnglishBench.dll`, `EnglishBench.deps.json`, and `EnglishBench.runtimeconfig.json` together. The optional `reader-settings.json` is created beside them on normal exit.

- Launch `artifacts/app/EnglishBench.exe` to restore the last selected library, if one has been saved.
- `Start-EnglishBench.cmd` currently launches with the machine-specific library `C:\MyDocs\magazines`.
- For another library, use the folder-selection button or pass a library explicitly:

```powershell
.\artifacts\app\EnglishBench.exe --library "C:\Libraries\English"
```

Select the parent directory of marked book directories. Startup and folder selection show only a collapsed tree, not a previous passage or vocabulary. The interface currently mixes Chinese and English labels; this guide names controls by their function in English.

## Basic use

1. Open a library and expand its tree one level at a time.
2. Select a passage leaf to load its text and matching vocabulary.
3. Select passage text within one sentence to reveal the small circle-plus action. Click it to save that selection to the word list. Cross-sentence selections are not addable.
4. Select a vocabulary entry to show move-up, move-down, and delete. These actions write the active companion JSON immediately.
5. Use the highlight toggle to display or hide literal vocabulary matches. Highlighting begins off.
6. Use the import button beside the highlight toggle to replace the active vocabulary with a selected structured JSON file after confirmation.
7. Open the bottom-left settings circle for the shared body/vocabulary font size and sentence-playback accent.

The bottom play/pause and stop buttons control only the first MP3 directly beside the passage. Stop is disabled during sentence or word playback and does not interrupt either. Right-click text for sentence playback; use the word's pronunciation buttons for word playback. The bottom progress bar appears only while the sibling MP3 is playing or paused and seeks only in that file. It is hidden during sentence or word playback. With the progress bar focused, Left and Right seek backward and forward by 0.5 seconds per step; the keyboard focus outline is hidden.

Successful ordinary actions are quiet. Warning/error status appears for six seconds and then disappears with its layout row. A new warning restarts that interval. Missing physical audio does not prevent passage reading.

Use a separate working copy of a library for interactive editing experiments. Do not alter `EnglishBench.Tests/Fixtures/Library` directly; deterministic tests depend on that fixed data.

## Build and regression checks

Run commands from the repository root:

```powershell
dotnet build EnglishBench -c Release
dotnet run --project EnglishBench.Tests -c Release
```

The tests are a small STA console runner using real WPF controls, temporary data copies, and both a fake player and real MP3 decoding. No additional test framework is required. Exit code 0 means all selected checks passed; code 1 means a test failed; an unknown category returns code 2.

| Category | Checks |
| --- | --- |
| `--rendering-only` | Vocabulary boundaries, SID mapping, selections, and document styles |
| `--content-only` | JSON contracts, navigation discovery, persistence, metadata, and quiet normal operations |
| `--audio-only` | Playback state, path validation, stale callbacks, and real fixture MP3 decoding |
| `--window-only` | Explicitly named prototype contracts, final-reader interactions, scrolling, and timed status layout |
| `--tree-only` | One-level expansion, recursive collapse, and navigation/display checks |
| `--controls-only` | Folder-only loading, settings, progress, and sibling-MP3 playback behavior |
| `--icons-only` | Native add action, SVG controls, startup size, footer alignment, and highlight state |
| `--editing-only` | Vocabulary reorder/delete UI and persisted JSON order on temporary copies |
| `--source-only` | Explicit local integration with the original magazine directory; current JSON consistency and before/after hashes |

For example:

```powershell
dotnet run --project EnglishBench.Tests -c Release -- --content-only
dotnet run --project EnglishBench.Tests -c Release -- --icons-only
```

Default tests use fixed content and real MP3 copies in `Fixtures/Library`; they do not require the editable original magazine directory. `--source-only` explicitly uses the local path in `SourceLibraryChecks.cs`, currently the article under `C:\MyDocs\magazines\The Economist\2026-09-26\When America walks away`. It is not portable without adjusting that test path.

The original source-hash file is a historical baseline, not an assertion that user-edited vocabulary never changes. Source checks report historical differences and verify that the current reading operation changes no source files. Write tests use temporary copies and clean them up. Window tests disable normal preference persistence; a settings-restoration test saves and restores settings in its own test-output directory.

Tests generate build outputs and diagnostic screenshots under `artifacts/`. Those screenshots are verification artifacts, not runtime dependencies. Do not remove `artifacts/app/` as part of cleaning test outputs.

## Publish

Close the running application before replacing its files. Publish into a staging directory:

```powershell
dotnet publish EnglishBench -c Release -p:DebugType=None -p:DebugSymbols=false -o artifacts/app-update
```

After successful verification, copy only the four runtime files named above from `app-update` to `app`. Preserve `app/reader-settings.json`. Do not copy test data or diagnostic screenshots into the release folder. This framework-dependent release needs the installed Windows Desktop Runtime.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Library has no books | Select the parent library; confirm `book.json` exists in each immediate child book directory |
| A leaf fails to open | Discovery checks filenames only; confirm that JSON is a valid passage, not unrelated metadata or standalone vocabulary |
| Words do not load automatically | Confirm the exact sibling `<title>.vocabulary.json` name, JSON structure, and audio-path declarations |
| Vocabulary is unreadable and cannot be overwritten | Repair the companion, then reopen the article; the reader protects damaged vocabulary from replacement |
| Saving reports an external edit | Reopen the article to load the current fingerprint before editing again |
| Bottom play reports no MP3 | Place a real whole-article MP3 directly beside the passage JSON; sentence audio is not a fallback |
| A different whole-article file plays | The first case-insensitively sorted sibling MP3 wins; keep one intended file or rename deliberately |
| A sentence or word cannot play | Check the exact declared file, nonzero size, valid MP3 encoding, and safe relative path |
| A canonical word is not highlighted | Matching is literal, not inflection-aware; see the vocabulary skill's surface-form discussion |
| Blank text has no right-click audio | Blank passages intentionally omit sentence audio; exercise answering is not implemented |
| Settings do not restore a passage or old window size | This is intentional: library-only startup and 70% centered window size are the current behavior |

For content authoring, use [Image to Passage](skills/image-to-passage/SKILL.md) and [Vocabulary Enrichment](skills/vocabulary-enrichment/SKILL.md), then follow [Library Layout](Library-Layout.md).
