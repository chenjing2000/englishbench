# EnglishBench Documentation

| Document | Purpose |
| --- | --- |
| [Library Layout](Library-Layout.md) | Required/optional files, filenames, placement, and audio lookup |
| [Application Overview](Application-Overview.md) | Design goals, modules, ownership, and data flows |
| [Exercise Module](Exercise-Module.md) | Passage families, five exercise types, manual answers, and host integration |
| [Getting Started](Getting-Started.md) | Launch, controls, publishing, and troubleshooting |
| [Testing](Testing.md) | Functional categories, fixture ownership, and manual acceptance checks |
| [Vocabulary Preparation](Vocabulary-Preparation.md) | Plain-list conversion and reader-specific validation before enrichment |

## Supplied content skills

The following three supplied skills are the primary preparation instructions. Image to Passage follows the current question-only exercise format; the other two retain their original content:

1. [Image to Passage](skills/skills-python/image_to_passage/SKILL.md): extract passage and exercise JSON from page images.
2. [Passage Segment](skills/skills-python/passage_segment/SKILL.md): organize paragraphs, stable SIDs, blanks, and segment audio declarations.
3. [Vocabulary Enrichment](skills/skills-python/vocabulary_enrichment/SKILL.md): enrich structured vocabulary using optional passage context.

Their StudyBench name refers to the Python origin; their content formats are read by EnglishBench. Current WPF runtime behavior and the new answer format are documented separately. These skills run externally and do not provide built-in OCR, dictionary lookup, or audio generation.

For an unstructured word list, first create a compatible structured file using Vocabulary Preparation, then apply the supplied enrichment skill. The source passage must remain unchanged. Audio paths are declarations; physical MP3 files must be supplied separately.
