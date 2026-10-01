# EnglishBench Documentation

These documents describe the current C# / WPF application. The running code is the compatibility authority; the archived Python application supplies historical workflow guidance.

| Document | Purpose |
| --- | --- |
| [Library Layout](Library-Layout.md) | Library discovery, required and optional files, exact filenames, placement, and audio lookup |
| [Application Overview](Application-Overview.md) | Design goals, framework, modules, responsibilities, state transitions, and current limitations |
| [Image to Passage Skill](skills/image-to-passage/SKILL.md) | Convert page images into passage JSON; includes the complete passage authoring contract |
| [Vocabulary Enrichment Skill](skills/vocabulary-enrichment/SKILL.md) | Convert structured vocabulary JSON or an unstructured word list into compatible vocabulary JSON |
| [Getting Started and Testing](Getting-Started.md) | Run the application, use its controls, build, test, publish, and troubleshoot content |

The two `SKILL.md` files are reusable instructions stored with this project. They can be given to an assistant with the source images or vocabulary input. They are not installed globally and do not add an in-application OCR, translation, or speech-generation feature.

## Content preparation sequence

1. Convert the source images into `<title>.json` using the passage skill.
2. Build or enrich `<title>.vocabulary.json` using the vocabulary skill. A matching passage is optional context.
3. Place both files in a marked book directory, optionally adding the audio files described in the library guide.
4. Open the parent library directory in EnglishBench, then select the passage.

JSON examples demonstrate structure. Audio paths are declarations, not generated MP3 files. The vocabulary skill's default translation language is Simplified Chinese, following the original workflow; all documentation and examples here are written in English.
