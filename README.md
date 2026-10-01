# EnglishBench

EnglishBench is a local C# / WPF application for English reading, vocabulary editing, and playback of existing MP3 files. It uses a directory tree, passage pane, and vocabulary pane backed by ordinary JSON files.

## Run

On Windows with the .NET 10 Windows Desktop Runtime (10.0.11 or a later compatible patch), launch `artifacts/app/EnglishBench.exe`. The root `Start-EnglishBench.cmd` supplies the machine-specific library `C:\MyDocs\magazines`. Use the folder-selection button for another library, or pass `--library` on the command line.

Startup loads only a collapsed library tree. It does not restore a passage or vocabulary. Routine successful actions are quiet; warnings and errors appear for six seconds, and the empty status row takes no space.

The bottom play/pause button uses only the alphabetically first MP3 directly beside the passage JSON. Sentence audio uses `audio_segments/`; word audio uses `audio_vocabulary/`. Stop is global. Vocabulary editing writes the active companion JSON, so use working copies for experiments.

## Documentation

Start with the [documentation index](docs/README.md):

- [Library layout and file requirements](docs/Library-Layout.md)
- [Design goals, framework, and modules](docs/Application-Overview.md)
- [Image-to-passage conversion skill and passage format](docs/skills/image-to-passage/SKILL.md)
- [Structured or plain-list vocabulary conversion skill](docs/skills/vocabulary-enrichment/SKILL.md)
- [Running, testing, publishing, and troubleshooting](docs/Getting-Started.md)

All maintained documentation is in English. The conversion skills prepare content externally; they are not built-in OCR, enrichment, or speech-generation features.

## Repository structure

| Path | Purpose |
| --- | --- |
| `EnglishBench/` | Published application's source and embedded UI resources |
| `EnglishBench.Tests/` | Categorized regression checks, fixed fixtures, and test-only prototype contracts |
| `artifacts/app/` | Runtime files and user settings; keep these together |
| `docs/` | Current documentation and conversion skills |
| `reference/englishbench_v0.13.25.zip` | Unmodified original Python archive retained as a reference |

## Build and test

Run from the repository root with an SDK compatible with `global.json`:

```powershell
dotnet build EnglishBench -c Release
dotnet run --project EnglishBench.Tests -c Release
```

See the testing guide for independent categories, the optional machine-specific source checks, and release replacement that preserves settings. The current WPF program does not implement account registration/login or exercise answering.
