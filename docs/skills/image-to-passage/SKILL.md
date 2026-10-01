---
name: image-to-passage
description: Convert English reading-page images into new EnglishBench passage JSON with faithful text, paragraphs, sentence SIDs, blank placeholders, and compatible audio declarations.
---

# Image to Passage

## Purpose and scope

Create `<title>.json` from one or more reading images. Preserve the source passage; do not translate, summarize, rewrite, or mix separate exercises into its body. Use the current EnglishBench format below.

This is a content-preparation skill, not an application feature. It declares audio paths but does not create audio, vocabulary, exercises, account state, or caches. The original Python `image_to_passage` and `passage_segment` skills informed this workflow; exercise generation is omitted because the current WPF reader does not implement it.

## Inputs and deliverable

Accept images in reading order, an optional requested title, and an optional destination. If page order or essential text cannot be determined, request the missing source information rather than inventing it.

Choose the title from the user's explicit title, then the genuine article title, then a concise English title derived from the body. Do not mistake a running header, unit label, question heading, or caption for the article title.

The filename stem is the displayed title. Use a Windows-safe filename: avoid reserved device names, invalid filename characters, and trailing spaces or periods. Do not add a JSON `title` field to compensate for naming.

Write one complete UTF-8 JSON file. The file itself must contain raw JSON, without Markdown fences, comments, commentary, or analysis fields. If returning file content in chat, return the complete JSON rather than a patch. Do not silently overwrite an existing passage or renumber its SIDs; replacement is a separate user-directed update because SIDs identify sentence audio.

## Extract the body

1. Read columns, pages, and paragraphs in their actual reading order. Remove duplicate text where images overlap.
2. Exclude page numbers, running headers/footers, unit labels, QR text, separate questions/options, and unrelated captions or printed translation glosses.
3. Preserve spelling, capitalization, punctuation, numbers, quotations, and word order. Correct a transcription error against the visible source; do not silently correct the author's grammar.
4. Restore natural paragraphs. Replace formatting-only line wraps within a paragraph with one ASCII space. Rejoin a layout-hyphenated word only when the intended word is clear; preserve real hyphens.
5. If genuine blanks occur in the body, replace them in reading order with `[[1]]`, `[[2]]`, and so on. Each number occurs once; numbering is continuous from 1. Do not create blanks from separate exercise questions or fill source blanks with guesses.

## Passage organization

| Field | Type and rule |
| --- | --- |
| `filetype` | String, exactly `"passage"` |
| `next_sid` | Integer, 1 through 1000, greater than every used SID number; generated files use the largest SID number plus one |
| `paragraphs` | Non-empty array of natural paragraph objects |
| `paragraph` | Non-empty array of sentence segment objects within each paragraph |
| `sid` | Lowercase `s` plus exactly three digits; `s001` through `s999`; unique across the whole passage |
| `text` | Non-empty string without leading or trailing whitespace |
| `audio` | Required UK/US declarations for every complete-article segment; entirely absent from every blank-passage segment |

For new files, assign sequential SIDs in reading order, starting at `s001` and continuing across paragraph boundaries. The loader permits some nonsequential SID sets, but this authoring workflow emits sequential ones. Do not reset numbering in each paragraph. The format permits at most 999 generated segments; if the source exceeds this limit, propose separately titled parts without truncating or emitting four-digit SIDs.

A segment represents one complete sentence meaning. A paragraph boundary ends its final segment. Do not split mechanically at commas, semicolons, colons, abbreviations, initials, decimal points, URLs, or punctuation inside an unfinished quotation. Sentence-final `.`, `?`, `!`, or ellipsis can mark a boundary when the meaning actually ends.

Keep the title outside the JSON. Do not add `tts_enabled`, exercise fields, paragraph IDs, Markdown, or HTML.

## Complete article

If there are no genuine body blanks, every segment must declare these exact relative paths:

```text
audio_segments/<sid>_uk.mp3
audio_segments/<sid>_us.mp3
```

Example file `A Small Example.json`:

```json
{
  "filetype": "passage",
  "next_sid": 4,
  "paragraphs": [
    {
      "paragraph": [
        {
          "sid": "s001",
          "text": "Reading opens new perspectives.",
          "audio": {
            "uk": "audio_segments/s001_uk.mp3",
            "us": "audio_segments/s001_us.mp3"
          }
        },
        {
          "sid": "s002",
          "text": "Careful practice makes a difference.",
          "audio": {
            "uk": "audio_segments/s002_uk.mp3",
            "us": "audio_segments/s002_us.mp3"
          }
        }
      ]
    },
    {
      "paragraph": [
        {
          "sid": "s003",
          "text": "Keep exploring.",
          "audio": {
            "uk": "audio_segments/s003_uk.mp3",
            "us": "audio_segments/s003_us.mp3"
          }
        }
      ]
    }
  ]
}
```

Declare the paths even if no MP3 files have been supplied. Missing physical sentence audio yields a warning but does not prevent reading. Do not create dummy audio files or claim audio was generated.

## Blank passage

One or more valid `[[n]]` placeholders make the entire passage a blank passage. Omit `audio` from every segment, including sentences without blanks. Do not set it to `null` or an empty object.

Example file `A Blank Example.json`:

```json
{
  "filetype": "passage",
  "next_sid": 3,
  "paragraphs": [
    {
      "paragraph": [
        {"sid": "s001", "text": "Reading opens [[1]] perspectives."},
        {"sid": "s002", "text": "Careful [[2]] makes a difference."}
      ]
    }
  ]
}
```

Do not emit malformed or unmatched brackets, `[[0]]`, `[[x]]`, or spaced forms such as `[[ 1 ]]`. Do not place reference answers inside passage JSON. The current reader displays these placeholders; it does not provide a cloze-answering interface.

## Placement and playback

Place the file inside a book recognized by `book.json`; see [Library Layout](../../Library-Layout.md). Date and article subdirectories are optional.

Sentence audio belongs in `audio_segments/`. The central bottom play/pause button instead uses the alphabetically first MP3 directly beside the passage JSON. That file is optional, is not represented by the segment `audio` fields, and is not generated by this skill. Do not confuse the two playback paths.

## Final checks

- Compare the extracted body and reading order with the images, including overlap removal, genuine paragraphs, and ambiguous punctuation.
- Parse the complete output as JSON. Verify the exact field names, non-empty arrays, trimmed text, passage-wide unique sequential SIDs, three-digit limit, and `next_sid`.
- For a complete article, verify that no `[[` or `]]` remains and every segment has exact SID-derived UK/US paths under `audio_segments/`.
- For a blank passage, verify unique continuous placeholder numbers and complete absence of all segment `audio` fields.
- Verify that exercises, guesses, translation glosses, and user state have not entered the body or metadata.
- Verify the filename and destination. Keep source images unchanged.
