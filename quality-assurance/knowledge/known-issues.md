# Known issues

Findings QualityBot has reported, and what happened to them. Status is one of **open**,
**accepted** (a human said won't-fix / later; do not re-report, do list under *Known issues*),
**fixed** (verified in a later cycle). Slug = the marker used in the PR comment.

| Slug | Severity | Where | Summary | Found | Status | Decision / fixed in |
| --- | --- | --- | --- | --- | --- | --- |
| `email-too-long-500` | Medium | `POST /api/students`, `/students/new` | Email > 254 chars → HTTP 500; UI shows "API couldn't be reached" | PR #15, 2026-10-20 | open | Regression test `students.spec.ts` is `test.fail` until fixed |
| `form-labels-not-associated` | Low | `/students/new` | Inputs have `name` but no `id`; labels' `for` do not match; no accessible names | PR #15, 2026-10-20 | open | |

## Lower findings not raised as bugs (from reviews)

- DOB accepts `0001-01-01`; no server-side lower bound (PR #15).
- A 400 without an `errors` map (e.g. malformed date) leaves the form silent (PR #15).
- Required fields have no visual marker (PR #15).
- `ClassName`/`Email` length limits are duplicated between entity configuration and validation (PR #15).
- `Location` header of `POST /api/students` points at a `GET` that does not exist (PR #15).
