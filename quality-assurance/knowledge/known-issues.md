# Known issues

Findings QualityBot has reported, and what happened to them. Status is one of **open**,
**accepted** (a human said won't-fix / later; do not re-report, do list under *Known issues*),
**fixed** (verified in a later cycle). Slug = the marker used in the PR comment.

| Slug | Severity | Where | Summary | Found | Status | Decision / fixed in |
| --- | --- | --- | --- | --- | --- | --- |
| `future-dob-silently-dropped` | Medium | `/students/new` | Date picker shows "Choose a date on or before …" / "Enter a date as M/d/yyyy." for a future or non-date value, but Save still submits `dateOfBirth: null` and the student is saved without the date | PR #15, 2026-10-21 | open | `students.spec.ts` "UI does not save a student while the date of birth is invalid" is `test.fail` until fixed |
| `email-format-loopholes` | Low | `POST /api/students` | `a.b@localhost`, `ada@example.com.`, `ada@[127.0.0.1]`, `"john doe"@example.com` accepted (dot check on whole string, `MailAddress` leniency) | PR #15, 2026-10-21 | open | `students.spec.ts` "API rejects an email with a trailing dot…" is `test.fail` until fixed |
| `form-labels-not-associated` | Low | `/students/new` | `Id` is applied to wrapper div and control, so labels resolve to the wrapper; inputs have no accessible name (date picker is fine) | PR #15, 2026-10-20 | open (re-verified 2026-10-21) | `students.spec.ts` "form inputs are associated with their labels" is `test.fail` until fixed |
| `name-allows-digits-special-chars` | Medium | `/students/new`, `POST /api/students` | Names accepted digits/special characters | PR #15, 2026-10-21 | fixed | c446da6, re-verified 2026-10-21 (UI + API, Unicode names OK, trim silent) |
| `email-too-long-500` | Medium | `POST /api/students`, `/students/new` | Email > 254 chars → HTTP 500 | PR #15, 2026-10-20 | fixed | 01e6086, re-verified 2026-10-21 (400 + "Email must be at most 254 characters.") |

## Lower findings not raised as bugs (from reviews)

- "The API couldn't be reached" is shown for any non-2xx except 400, and for parse failures after a
  201 (the `try` in `AddStudent.razor` also wraps toast + navigation) — a retry would create a
  duplicate; no duplicate detection at any layer (PR #15, still open 2026-10-21).
- DOB accepts `0001-01-01`; no server-side lower bound. Client "today" is the UTC date (PR #15).
- A 400 without an `errors` map (malformed JSON / date) leaves the form silent (PR #15).
- `Location` header of `POST /api/students` points at a `GET` that returns 404 (PR #15).
- Name rule accepts separator runs (`Ann-`, `O''Brien`, `Mary  Ann`) and rejects periods (`J. R.`)
  and ZWJ/ZWNJ joiners (PR #15, 2026-10-21, observation).
- List ordering has no tie-breaker; `d MMM yyyy` follows browser culture; `ClassName` max 50 is a
  magic number; no unit tests for the API handlers or `StudentsClient` (PR #15 review, 2026-10-21).
- Required fields have no visual marker (PR #15).
