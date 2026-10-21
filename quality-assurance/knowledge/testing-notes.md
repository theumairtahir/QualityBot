# Testing notes

Environment facts and lessons that save time in the next cycle.

## Environment (every cycle)

- Fresh containers each run; the database starts empty and `seed/*.sql` is applied after the API's
  migrations. Nothing from a previous cycle survives except what is in `seed/` and `knowledge/`.
- Web `http://localhost:8080`, API `http://localhost:8081`, DB via `./scripts/db.sh` only.
- The regression suite runs **before** the agent; its JSON is in `.qa/regression-results.json`.
  The suite's own API tests may create rows, so the DB is only guaranteed empty if they all failed
  validation; check `select count(*) from students` before relying on the empty state.
- Screenshots must be saved as `.qa/screenshots/<case>-<slug>.png`; `publish-screenshots.sh` pushes
  them to the `qa-artifacts` branch and returns URLs that render inline in PR comments.
- Pushes made by the bot carry `[skip ci]` and are skipped by the workflow's guard job; the next
  human push starts a new cycle.
- `curl` works for simple bodies, but `$(...)` inside a `Bash` call is denied ("nested command").
  For generated payloads (long strings, loops) use `browser_evaluate` with `fetch` from the app
  origin (CORS allows it); note the `Location` header is not exposed to JS — use `curl -i` for that.

## Lessons

- 2026-10-21 (PR #16): `getByRole('table', { name: 'Students' })` finds nothing — the `Table`
  component does not render its `AriaLabel`. Scope with `getByTestId('student-list').getByRole('table')`.
- 2026-10-21 (PR #16): the roll-number cell is the first cell, so row names are `"17 Qa Searchable
  Grade 3 …"`; use `getByRole('cell', { name, exact: true })` or an unanchored regex, never `^First`.
- 2026-10-21 (PR #16): `#student-search` matches two elements (wrapper div + input, same duplicate-id
  pattern as the form); use `input#student-search` or the searchbox role.
- 2026-10-21 (PR #16): to simulate "another user", `curl -X DELETE` / `PUT` against `$QA_API_URL`
  while the page is open works well; two browser tabs are not needed.
- 2026-10-21 (PR #16): `mv`/`rm` inside `.qa/` need approval in the headless run — name screenshots
  correctly the first time (`C7-future-dob-edit-wipes-dob.png` is misnamed: the DOB was *kept*).
- 2026-10-21 (PR #16): the browser logs `Failed to load resource … 404` for intentional 404 calls
  (edit of a removed student, DELETE after DELETE); that is not an application console error.
- 2026-10-21 (PR #15): generated names must be **letters only** (`Date.now()` digits are rejected by
  the name rule). `students.spec.ts` has a base-26 `unique()` helper.
- 2026-10-21 (PR #15): toast text appears twice (visible paragraph + live region) → `getByText`
  needs `{ exact: true }` or strict mode fails.
- 2026-10-21 (PR #15): Blazor binds on `change`; a JS `element.click()` from `browser_evaluate` does
  not blur the focused input, so the last typed value is not bound and validation fires. Use real
  Playwright clicks (`browser_click`, `doubleClick: true` for double-submit tests).
- 2026-10-21 (PR #15): the date picker is a text input (`#date-of-birth`, format `M/d/yyyy`, ISO
  also accepted); invalid values show the component's own message and are **not** bound — the form
  still submits (bug `future-dob-silently-dropped`).
- 2026-10-20 (PR #15): "Page not found" appears three times on the 404 page — use
  `getByRole('heading', { name: /not found/i })`, not `getByText`.
- 2026-10-20 (PR #15): `getByLabel` fails on the add-student form (duplicate ids); use
  `input[name=...]` / `select[name=...]`.
- 2026-10-20 (PR #15): a 500 from the API is presented by the UI as "API couldn't be reached";
  check `browser_network_requests` to tell a server error from a network error.
- One shell command per `Bash` call; compound commands are denied by the allow list.

## Not yet verified / open questions

- UI presentation of a 500 or of a 400 without `errors` (needs the API stopped or a crafted
  response; not reachable through the form).
- Whether the calendar popup itself can select a future date (only typing was tested).
- Search stale-response race (PR #16 review): needs network throttling to reproduce.
