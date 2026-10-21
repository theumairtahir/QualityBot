# Students — add a new student

**Source of truth:** issue #2 "Add a new student" (AC1–AC3) + team request in the PR #15 thread
(names: no digits/special characters, trim surrounding spaces silently) · delivered in PR #15 ·
last tested 2026-10-21 (cycle 3: 32 cases, 28 pass, 3 fail, 1 skipped; verdict PASS).

## Acceptance criteria

1. `/students/new` offers first name, last name, date of birth, email and class; first name, last
   name and class are required. (issue #2)
2. Email format is validated (client and server). (issue #2)
3. Saving shows a success toast and redirects to `/students`, where the new student is listed. (issue #2)
4. First/last name reject digits and special characters; surrounding spaces are trimmed without a
   message. (PR #15 comment, 2026-10-20)
5. Inferred: email ≤ 254 chars, DOB not in the future, class from the fixed Grade 1–10 list.

## Critical flows

| Flow | Covered by (`tests/regression/students.spec.ts`) |
| --- | --- |
| Required-field validation messages | "required fields are validated" |
| Create a student via the UI and see it in the list | "saving a student shows a success message…" |
| Server rejects an invalid email with a field error | "an invalid email is rejected" |
| Email length 254/255 boundary (API) | "API rejects an email longer than 254 characters…" |
| Names: digits/special chars rejected in UI and API; real-world names accepted; 100/101 boundary | `students: name rules` describe |
| Trim via UI and API without a message | "UI trims surrounding spaces…", "API trims surrounding spaces…" |
| DOB tomorrow → 400, today → 201; class not in list → 400 (API) | "API rejects a date of birth in the future…", "API rejects a class that is not in the list" |
| Cancel returns to the list | "cancel returns to the list without saving" |
| Open bugs (expected-fail) | future DOB via UI, email loopholes, label association |

## Data

- No seed rows needed; tests generate unique letters-only surnames `Qa<base26>`.
- Table `students`; see `app-map.md`.

## Bug history

| Cycle | Found | Status |
| --- | --- | --- |
| PR #15 cycle 1 (2026-10-20) | `email-too-long-500` (Medium), `form-labels-not-associated` (Low) | fixed / open |
| PR #15 cycle 2 (2026-10-21) | `name-allows-digits-special-chars` (Medium) | fixed |
| PR #15 cycle 3 (2026-10-21) | `future-dob-silently-dropped` (Medium), `email-format-loopholes` (Low) | open |

## Verified safe (2026-10-21)

- XSS/HTML payloads in names are rejected by the character rule; earlier cycle: rendered as text.
- First/last name and email are trimmed before save; empty email stored as NULL; `class_name` not
  trimmed but validated against the list.
- Unicode names (`Zoë`, `José María`, `محمد`, `李`, decomposed marks) accepted.
- Double-click on Save → one POST; two tabs saving concurrently both succeed.
- Keyboard: logical tab order; Enter in a text field submits.
- Extra JSON fields (`id`, `createdAtUtc`) are ignored by the API.

## Changes since

- PR #16 (2026-10-21): the form moved into the shared `StudentForm` component (also used by
  `/students/{id}/edit`); the add flow was re-verified (manual + regression suite). The list now
  starts each row with the roll number — `students.spec.ts` trim test re-anchored on the name cell.
  See `students-list-edit.md`.

## Still to test

- UI behaviour on a 500 / 400-without-errors response (needs API fault injection).
- Calendar popup navigation (only typed dates tested).
