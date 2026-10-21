# Settings — school profile (name and academic year)

**Source of truth:** issue #14 "[Settings] Set the school profile" (AC1–AC3) + PR #17 body (inferred
rules) · delivered in PR #17 · last tested 2026-10-21 (cycle 1: 35 cases, 28 pass, 5 fail, 2 skipped;
verdict PASS).

## Acceptance criteria

1. I can edit the school name and the academic year (e.g. 2026-2027). (issue #14)
2. The school name is required. (issue #14)
3. The saved school name shows in the sidebar header. (issue #14)
4. Inferred (PR body/code): year optional, two consecutive years; name ≤ 100 (trimmed); default
   "Demo campus" until saved; header updates without reload; year shown in the subtitle.

## Critical flows

| Flow | Covered by (`tests/regression/settings.spec.ts`) |
| --- | --- |
| Sidebar link → form | "sidebar Settings link opens the school profile form" |
| Save name + year → toast, header updates without reload, persists, trimmed | "saving name and year updates the sidebar header…" |
| Blank year → NULL, subtitle without year | "clearing the academic year removes it…" |
| Client validation (blank name, bad year), no PUT | "a blank school name and a malformed year are rejected…" |
| API name required / 100–101 boundary / trim | "API requires a name and limits it to 100 characters" |
| API year partitions | "API rejects academic year …" (7 cases) |
| HTML in name rendered as text | "API renders an HTML name as text in the header" |
| Open bugs (expected-fail) | Unicode-digit year (API + UI), label association |

## Data

- Table `school_profile`, single row id=1; empty in a fresh DB. Tests save their own state; the spec
  runs in `mode: 'default'` (one worker, in order) because all tests share the one row.
- No seed needed.

## Bug history

| Cycle | Found | Status |
| --- | --- | --- |
| PR #17 cycle 1 (2026-10-21) | `academic-year-unicode-digits-crash` (Medium), `sidebar-school-header` (Low); `form-labels-not-associated` extended to `/settings` | open |

## Verified safe (2026-10-21)

- HTML/script in the name is rendered as text. CORS preflight from a foreign origin gets no allow headers.
- Malformed JSON / wrong types → 400, missing content type → 415 (no 500).
- 16 concurrent first-time PUTs → all 200, one row (race in the code not reproduced).
- Keyboard: Tab name → year; Enter submits. Double-click → 2 idempotent PUTs, no error.

## Still to test

- Back button after save; concurrent PUTs on an existing row; mobile drawer header; dark mode.
- UI on a PUT 500 / 400-without-errors (needs fault injection).
