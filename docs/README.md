# EnglishBench Documentation

These documents describe the current main-branch C# / WPF reader. This branch has no exercise display or answer storage. Exercise companions produced by the authoring skills can remain beside passages and are ignored by the reader.

| Document | Purpose |
| --- | --- |
| [Library Layout](Library-Layout.md) | Required and optional files, names, placement, discovery, and audio lookup |
| [Application Overview](Application-Overview.md) | Design goals, framework, module responsibilities, lifecycle, and limitations |
| [Getting Started](Getting-Started.md) | Launch, controls, build, publishing, and troubleshooting |
| [Testing](Testing.md) | Functional categories, fixture ownership, and manual acceptance |
| [Vocabulary Preparation](Vocabulary-Preparation.md) | Structured input, plain word lists, and reader compatibility checks |
| [Image to Passage Skill](skills/skills-python/image_to_passage/SKILL.md) | Convert page images into passage JSON and optional exercise companions |
| [Passage Segmentation Skill](skills/skills-python/passage_segment/SKILL.md) | Organize complete or blank passages into paragraphs and stable sentence IDs |
| [Vocabulary Enrichment Skill](skills/skills-python/vocabulary_enrichment/SKILL.md) | Enrich structured vocabulary with contextual meanings and audio declarations |

The three supplied skills are retained without changes. Some historical StudyBench wording and exercise instructions remain intentionally. They prepare content externally; they do not install OCR, dictionaries, speech generation, or an exercise interface in this reader. Vocabulary Preparation provides the plain-list input step without expanding or replacing the supplied enrichment skill.

## Content preparation sequence

1. Use Image to Passage for images and Passage Segmentation for text organization. Keep exercises out of the passage body.
2. Prepare `<title>.vocabulary.json` from structured JSON or a plain list, using the enrichment skill and compatibility guide.
3. Place the passage and its matching vocabulary inside a marked book directory. Add physical MP3 files if available.
4. Open the parent library directory, expand the tree, and select the passage. See Library Layout for exact filenames and audio paths.

Declared audio paths do not generate MP3 files. Documentation is written in English; the vocabulary workflow can produce glosses in the requested language.
