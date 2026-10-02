# Testing EnglishBench

## Runner and data ownership

Run from the repository root on Windows:

```powershell
dotnet run --project EnglishBench.Tests -c Release
```

The STA console runner tests current production services and WPF controls. It uses a fake audio adapter for deterministic state checks and real MP3 files for decoding checks. Exit code 0 means all selected checks passed, 1 means a failure, and 2 means an unknown category.

Default checks always use checked-in fixtures under `EnglishBench.Tests/Fixtures/`. Exercise checks use the Week 5 Monday fixture, not a fallback to editable personal files. Tests that save vocabulary or responses use temporary copies and remove them afterward. Windows normally use `persistSettings: false`; settings-restoration tests back up and restore their test-output settings file.

No test-only prototype application, paragraph playlist, or historical source-hash baseline is required. Tests for removed behavior have been removed or replaced with checks of the current controls. Some overlap between service and UI tests is intentional: one verifies a rule, while the other verifies its wiring.

## Functional categories

| Category | Responsibility |
| --- | --- |
| `--content-only` | Article and ArticleBlank contracts, JSON parsing, discovery, fixed account, atomic vocabulary writes and external edits |
| `--rendering-only` | Literal matching, SID ownership, selections, blanks, and text styles |
| `--window-only` | Loaded-control hit-testing, selection action, three-pane rendering, independent scrolling, and timed status lifecycle |
| `--tree-only` | One-level expansion and recursive collapse |
| `--audio-only` | Single-file playback, two-player coordination, stale callbacks, invalid resources, and real MP3 decoding |
| `--controls-only` | Library-only startup, settings, keyboard shortcuts, progress/time, and sibling MP3 controls |
| `--icons-only` | Embedded icons, circle-plus movement, footer alignment, startup size, and highlighting |
| `--editing-only` | Vocabulary move/delete UI and matching persisted order |
| `--exercise-only` | Five question-only schemas, save/reset actions, manual persistence, container integration, and keyboard isolation |
| `--source-only` | Optional read-only integration against the local Economist article; excluded from default tests |

Example:

```powershell
dotnet run --project EnglishBench.Tests -c Release -- --exercise-only
```

Cases share immutable fixture definitions and small WPF event/layout helpers. Each write case owns its temporary data. Schema, session, persistence, view, and integration checks have distinct purposes, even where one case contains several related assertions.

`--source-only` uses the path in `SourceLibraryChecks.cs`. It compares source hashes before and after reading instead of comparing editable files with an old snapshot. It requires the local library and is not a portable regression category.

## Manual acceptance checks

Automated checks cover services, real controls, and event routing. They do not replace checking native dialogs and actual user input:

1. Open a copied library. Confirm startup is collapsed and empty; expand/select a passage and inspect all three panes.
2. Select text, scroll, and move the window. Confirm the circle-plus follows the selection and disappears when it clears.
3. Play the sibling MP3, then word/segment audio. Confirm the article pauses and preserves its position. Resume the article and confirm pronunciation stops. Stop/seek must affect only the article MP3.
4. Exercise the five question types. In text inputs, type spaces, edit with arrows, and enter a multiline answer where supported. Confirm playback is unaffected.
5. Save and reopen responses. Reset without saving and verify the previous save restores. Save the reset and verify empty responses restore.
6. Change an answer and switch articles or close. Test each Save/Discard/Cancel dialog branch. On save failure, responses must remain and leaving must be cancelled.

Use copies for write checks. Do not manually edit the checked-in fixtures. A screenshot is supporting evidence, not a passing test by itself.

## Generated files

Build outputs and diagnostics are ignored by Git. Screenshots under `artifacts/` are not runtime files and can be removed after review. Keep the formal `artifacts/app/` release and its settings. See [Getting Started](Getting-Started.md) for publishing.
