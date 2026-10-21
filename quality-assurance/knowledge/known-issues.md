# Known issues

Findings QualityBot has reported, and what happened to them. Status is one of **open**,
**accepted** (a human said won't-fix / later; do not re-report, do list under *Known issues*),
**fixed** (verified in a later cycle). Slug = the marker used in the PR comment.

| Slug | Severity | Where | Summary | Found | Status | Decision / fixed in |
| --- | --- | --- | --- | --- | --- | --- |
| `edit-removed-student-wrong-error` | Medium | `/students/{id}/edit` | Saving after the student was removed elsewhere: `PUT → 404` is shown as "The API couldn't be reached. Check that it is running and try again." instead of the page's "Student not found" state (`StudentsClient.ReadSaveResultAsync` only special-cases 400) | PR #16, 2026-10-21 | open | `students-edit-remove.spec.ts` "saving a student that was removed meanwhile…" is `test.fail` until fixed |
| `row-actions-no-accessible-context` | Low | `/students` | Row "Edit"/"Remove" buttons have identical accessible names, no student context; grouped: `Table AriaLabel="Students"` not rendered; sidebar "Students" not `aria-current` on edit/new pages | PR #16, 2026-10-21 | open | — |
| `future-dob-silently-dropped` | Medium | `/students/new`, `/students/{id}/edit` | Date picker shows "Choose a date on or before …" / "Enter a date as M/d/yyyy." for a future or non-date value, but Save still submits. On add the student is saved without the date; on edit (PR #16) the stored DOB is kept and the typed value is silently ignored, with a "Student updated" toast | PR #15, 2026-10-21 (edit case re-checked PR #16) | open | `students.spec.ts` "UI does not save a student while the date of birth is invalid" is `test.fail` until fixed |
| `email-format-loopholes` | Low | `POST /api/students` | `a.b@localhost`, `ada@example.com.`, `ada@[127.0.0.1]`, `"john doe"@example.com` accepted (dot check on whole string, `MailAddress` leniency) | PR #15, 2026-10-21 | open | `students.spec.ts` "API rejects an email with a trailing dot…" is `test.fail` until fixed |
| `form-labels-not-associated` | Low | `/students/new`, `/students/{id}/edit`, `/students` search | `Id` is applied to wrapper div and control, so labels resolve to the wrapper; inputs have no accessible name (date picker is fine). The PR #16 search box (`#student-search`) has the same duplicate id but does carry `aria-label` | PR #15, 2026-10-20 | open (re-verified 2026-10-21, PR #16) | `students.spec.ts` "form inputs are associated with their labels" is `test.fail` until fixed |
| `name-allows-digits-special-chars` | Medium | `/students/new`, `POST /api/students` | Names accepted digits/special characters | PR #15, 2026-10-21 | fixed | c446da6, re-verified 2026-10-21 (UI + API, Unicode names OK, trim silent) |
| `email-too-long-500` | Medium | `POST /api/students`, `/students/new` | Email > 254 chars → HTTP 500 | PR #15, 2026-10-20 | fixed | 01e6086, re-verified 2026-10-21 (400 + "Email must be at most 254 characters.") |

## Lower findings not raised as bugs (from reviews)

- PR #16 (2026-10-21): search debounce cancels the delay but not the in-flight request, so a slow
  earlier response can overwrite a newer one (not reproducible on localhost); duplicate `GET` on
  blur because `ValueChanged` and `OnInput` both trigger the search; `UpdateStudent` load-then-save
  can 500 if the row is deleted in between; `required` dropped from `Student`; `defaultValue: 0` is
  scaffolder noise on the identity migration; no app-side tests for `EscapeLike` or the new handlers.
- "The API couldn't be reached" is shown for any non-2xx except 400, and for parse failures after a
  201 (the `try` in the shared `StudentForm` also wraps the `OnSaved` callback) — a retry would create a
  duplicate; no duplicate detection at any layer (PR #15, still open 2026-10-21).
- DOB accepts `0001-01-01`; no server-side lower bound. Client "today" is the UTC date (PR #15).
- A 400 without an `errors` map (malformed JSON / date) leaves the form silent (PR #15).
- `POST /api/students` `Location` header now resolves (`GET /api/students/{id}` added in PR #16).
- Name rule accepts separator runs (`Ann-`, `O''Brien`, `Mary  Ann`) and rejects periods (`J. R.`)
  and ZWJ/ZWNJ joiners (PR #15, 2026-10-21, observation).
- List ordering has no tie-breaker; `d MMM yyyy` follows browser culture; `ClassName` max 50 is a
  magic number (PR #15 review, 2026-10-21).
- Required fields have no visual marker (PR #15).
