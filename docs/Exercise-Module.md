# Exercise Module

## Scope and integration

`EnglishBench/Exercises/` is a concrete, reusable module. It has no dependency on the main window, vocabulary, audio players, accounts, or the main view model. It uses the existing JSON and atomic-file helpers. No new packages or service interfaces are required.

| File | Responsibility |
| --- | --- |
| `ExerciseContent.cs` | Exercise, question, and option data |
| `ExerciseRepository.cs` | Read and validate one exercise against a validated Article or ArticleBlank |
| `ExerciseAnswers.cs` | Derive the mirrored answer path and read/write the new answer format |
| `ExerciseSession.cs` | Current answers, saved baseline, reset, and explicit save |
| `ExerciseView.cs` | WPF question controls and two action buttons |
| `UnsavedAnswersDialog.xaml` / `.xaml.cs` | Rounded modal confirmation with explicit save, discard, and cancel actions |
| `MainWindow.Exercises.cs` | Application integration: companion lookup, book context, restoration, warnings, and leave confirmation |

The shared passage models live in their own files: `Models/Article.cs` validates complete text with declared segment audio; `Models/ArticleBlank.cs` extends the common structure and validates blanks without segment audio. Choice/free-response exercises require Article; the three cloze types require ArticleBlank. Exercise data composes with these passage models rather than duplicating their validation or creating five mostly empty subclasses.

To reuse the module, load an `ExerciseContent` with `ExerciseRepository.Load(exercisePath, passage)`, provide a destination path and optional restored answers to `ExerciseSession`, and call `ExerciseView.ShowExercise(session)`. The view inherits font settings, exposes its current `Session`, and reports failures through its `Message` event. It can be placed in another WPF container. `ShowExercise(null)` clears and hides it.

EnglishBench places the exercise in its own container below the passage inside the central scrolling area. The exercise font follows the body/vocabulary setting. Missing exercises leave the container hidden. Invalid exercises produce a warning while the passage remains readable.

## Exercise files

The companion is `<title>.exercise.json`, beside `<title>.json`. It has `filetype: "exercise"` and exactly one supported `type`:

| Type | Payload and input |
| --- | --- |
| `article_choice` | `questions`, each with `prompt` and keyed `options`; one radio choice per question |
| `article_answer` | `questions`, each with `prompt`; a multiline answer box |
| `article_cloze` | `items`, each with keyed `options`; one radio choice per blank |
| `article_cloze_words` | `items`, each with string `cue`; a single-line answer box |
| `article_cloze_sentences` | A shared top-level keyed `options` pool and `items`; one uppercase letter per blank |

Every question/item has a unique positive integer `number`. Options require at least two entries with unique, nonempty keys and nonempty text; sentence-pool keys are single uppercase A-Z letters. Cloze item numbers must match passage `[[n]]` placeholders exactly. Complete-passage question types require a passage without blanks. Question JSON contains only question data, with no user responses, answer keys, solutions, or grading information. The `cue` in word cloze exercises is part of the printed question and is retained.

The preparation skills under `skills/skills-python/` describe these five source formats. The image-to-passage skill follows the current question-only schema. The saved response format below is specific to this WPF release.

## Actions

Buttons appear after all questions in this order: Save Answers, Reset Answers. The UI labels are localized in the application.

- **Save Answers:** explicitly writes all current answers. There is no automatic saving. Each question has one current answer; a later save replaces the previous file without history.
- **Reset Answers:** clears every current response. This changes the interface only; click Save Answers to replace the previously saved responses with empty answers.

Switching article/library or closing with changed responses opens a rounded confirmation centered on the reader window. Save and Continue writes responses before leaving; Discard Changes leaves without saving; Cancel, Escape, and the close icon return to the current exercise. Enter activates the default save action. A failed save retains the responses and cancels leaving. The module records responses without evaluating their correctness. Unused extra fields in existing source exercise files are ignored; loading never rewrites the source file.

Space and Left/Right keys inside exercise text inputs retain their normal editing role. Outside those inputs, the existing article-audio shortcuts remain in effect. Exercise input never controls either audio player.

## Answer storage

The sole account is `xiaoxin`. There is no account creation or Python-answer migration. Answers live under the selected library root:

```text
Library/
  Book Name/
    book.json
    Week/Day/Reading.json
    Week/Day/Reading.exercise.json
  userdata/
    xiaoxin/
      Book Name/
        Week/Day/Reading.exercise.json
```

The book name is the name of the directory containing `book.json`. Paths inside that book and the exercise filename are mirrored exactly. An exercise directly in a book is saved directly under `userdata/xiaoxin/Book Name/`. The source exercise is never overwritten. The library scanner excludes `userdata` from navigation.

The saved file uses a distinct format:

```json
{
  "filetype": "exercise_answers",
  "version": 1,
  "type": "article_choice",
  "answers": [
    { "number": 1, "answer": "B" },
    { "number": 2, "answer": "" }
  ]
}
```

Empty strings represent unanswered questions. Saved question numbers must belong to the current exercise and must not repeat. Choice values must match valid option keys. Old formats, malformed JSON, and mismatched types are reported and ignored rather than partially restored. A deliberate save writes the current new-format responses atomically.

## Verification

Run `dotnet run --project EnglishBench.Tests -c Release -- --exercise-only`. Cases cover the five question-only schemas, manual save/reset/restore, mirrored paths and rejection of invalid data, independent WPF controls with exactly two action buttons, the actual confirmation's save/discard/cancel/close and save-failure branches, the Week 5 Monday sample, and keyboard separation.

The test runner uses temporary library copies for saving, verifies source hashes, and never writes answers into the original sample library. Default checks always use the checked-in fixture, so edits to the real textbook folder cannot affect the regression suite.
