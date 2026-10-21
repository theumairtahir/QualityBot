# Testing notes

Environment facts and lessons that save time in the next cycle.

## Environment (every cycle)

- Fresh containers each run; the database starts empty and `seed/*.sql` is applied after the API's
  migrations. Nothing from a previous cycle survives except what is in `seed/` and `knowledge/`.
- Web `http://localhost:8080`, API `http://localhost:8081`, DB via `./scripts/db.sh` only.
- The regression suite runs **before** the agent; its JSON is in `.qa/regression-results.json`.
- Screenshots must be saved as `.qa/screenshots/<case>-<slug>.png`; `publish-screenshots.sh` pushes
  them to the `qa-artifacts` branch and returns URLs that render inline in PR comments.
- Pushes made by the bot carry `[skip ci]` and are skipped by the workflow's guard job; the next
  human push starts a new cycle.

## Lessons

- 2026-10-20 (PR #15): "Page not found" appears three times on the 404 page — use
  `getByRole('heading', { name: /not found/i })`, not `getByText`.
- 2026-10-20 (PR #15): the add-student form has no `id` on inputs, so `getByLabel` fails; the
  regression spec uses `data-testid` and unique names (`QA<timestamp>`) per test.
- 2026-10-20 (PR #15): a 500 from the API is presented by the UI as "API couldn't be reached";
  check `browser_network_requests` to tell a server error from a network error.
- One shell command per `Bash` call; compound commands are denied by the allow list.

## Not yet verified / open questions

- Two-tab concurrent edit behaviour (skipped in PR #15 for time).
- Whether the date-of-birth picker allows future dates in the UI (skipped in PR #15).
