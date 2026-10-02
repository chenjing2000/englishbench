# EnglishBench

EnglishBench is a local C# / WPF application for English reading, vocabulary editing, and playback of existing MP3 files. It uses a directory tree, passage pane, and vocabulary pane backed by ordinary JSON files.

## Run

On Windows with the .NET 10 Windows Desktop Runtime (10.0.11 or a later compatible patch), launch `artifacts/app/EnglishBench.exe`. Use the folder-selection button to choose a library, or pass `--library` on the command line.

Startup loads only a collapsed library tree. It does not restore a passage or vocabulary. Routine successful actions are quiet; warnings and errors appear for six seconds, and the empty status row takes no space.

The bottom play/pause, stop, and progress controls use only the alphabetically first MP3 directly beside the passage JSON. Sentence audio uses `audio_segments/`; word audio uses `audio_vocabulary/`. These pronunciation actions share a separate player. Vocabulary editing writes the active companion JSON, so use working copies for experiments.

## Documentation

Start with the [documentation index](docs/README.md):

- [Library layout and file requirements](docs/Library-Layout.md)
- [Design goals, framework, and modules](docs/Application-Overview.md)
- [Supplied image-to-passage skill](docs/skills/skills-python/image_to_passage/SKILL.md)
- [Supplied passage-segment skill](docs/skills/skills-python/passage_segment/SKILL.md)
- [Supplied vocabulary-enrichment skill](docs/skills/skills-python/vocabulary_enrichment/SKILL.md)
- [Plain-list vocabulary preparation](docs/Vocabulary-Preparation.md)
- [Running, testing, publishing, and troubleshooting](docs/Getting-Started.md)
- [Functional regression and manual acceptance checks](docs/Testing.md)

All maintained documentation is in English. The conversion skills prepare content externally; they are not built-in OCR, enrichment, or speech-generation features.

## Repository structure

| Path | Purpose |
| --- | --- |
| `EnglishBench/` | Published application's source and embedded UI resources |
| `EnglishBench.Tests/` | Categorized regression checks and fixed fixtures |
| `artifacts/app/` | Runtime files and user settings; keep these together |
| `docs/` | Current documentation and conversion skills |

## Build and test

Run from the repository root with an SDK compatible with `global.json`:

```powershell
dotnet build EnglishBench -c Release
dotnet run --project EnglishBench.Tests -c Release
```

See the testing guide for independent categories, the optional machine-specific source checks, and release replacement that preserves settings. The WPF program supports five exercise types with manual saving for the fixed xiaoxin account; account registration/login is not implemented. See [Exercise Module](docs/Exercise-Module.md).
