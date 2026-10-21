# Students — add a new student

**Source of truth:** issue #2 "Add a new student" (AC1–AC3) · delivered in PR #15 · last tested
2026-10-20 (PR #15 cycle: 16 cases, 12 pass, 2 fail, 2 skipped; verdict PASS).

## Acceptance criteria (from issue #2)

1. `/students/new` offers first name, last name, date of birth, email and class; first name, last
   name and class are required.
2. Email format is validated (client and server).
3. Saving shows a success toast and redirects to `/students`, where the new student is listed.

## Critical flows

| Flow | Covered by |
| --- | --- |
| Required-field validation messages | `tests/regression/students.spec.ts` |
| Create a student via the UI and see it in the list | `tests/regression/students.spec.ts` |
| Server rejects an invalid email with a field error | `tests/regression/students.spec.ts` |
| Over-long email is rejected with a field error | `tests/regression/students.spec.ts` (`test.fail` until `email-too-long-500` is fixed) |

## Data

- No seed rows needed; tests generate unique names `QA<timestamp><rand>`.
- Table `students`; `email varchar(254)`.

## Bug history

| Cycle | Found | Status |
| --- | --- | --- |
| PR #15 (2026-10-20) | `email-too-long-500` (Medium), `form-labels-not-associated` (Low) | open |

## Verified safe

- XSS payload in a name renders as text (PR #15).
- First/last name are trimmed before save (PR #15); `class_name` is not.

## Still to test

- Two tabs creating students concurrently; future date of birth via the UI.
