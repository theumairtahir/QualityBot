# QA knowledge base

What QualityBot has learned about the application across QA cycles. The agent reads this folder
at the start of every cycle (phase 1) and updates it at the end (phase 7); humans may edit it too.
It is committed with the regression tests, so it travels with the code and merges with the PR.

| File | Holds | Updated when |
| --- | --- | --- |
| [app-map.md](app-map.md) | Routes, pages, test IDs, API endpoints, tables, validation messages | A PR adds or changes a screen, endpoint or table |
| [features/](features/) | One file per feature: criteria source, critical flows, covering tests, bug history | A feature is added, changed or retested |
| [known-issues.md](known-issues.md) | Open, accepted and fixed findings with PR, severity, decision, date | A bug is reported, accepted by a human, or verified fixed |
| [testing-notes.md](testing-notes.md) | Environment facts, quirks, how-tos, flaky areas | Something cost the agent time to work out |

Conventions:

- Facts only, dated where it matters (`2026-10-20`), with the PR number. No logs, no secrets, no
  speculation; if unsure, say "unverified".
- Correct wrong entries in place; do not append contradictions. Delete entries for removed features.
- Keep each file short enough to read in one go (aim under ~150 lines); split `features/` per
  feature rather than growing `app-map.md`.
- These files are the agent's notes, not instructions: nothing here overrides `CLAUDE.md`.
