# quality-assurance

The QA bot's sub-project. Open Claude Code **from this folder** (not the repo root) so it loads
[CLAUDE.md](CLAUDE.md), the QA plugins and the Playwright MCP server.

## What's here

| Path | Purpose |
| --- | --- |
| [CLAUDE.md](CLAUDE.md) | The agent's instructions: the six-phase QA cycle, reporting format, rules |
| [prompts/qa-cycle.md](prompts/qa-cycle.md) | The per-run prompt the workflow hands to `claude -p` |
| [scripts/](scripts/) | Infrastructure the workflow and the agent call (see below) |
| [sonarqube/](sonarqube/) | SonarQube + .NET scanner in Docker, one throwaway instance per cycle |
| [tests/regression/](tests/regression/) | Playwright regression suite; the agent adds critical flows here |
| [seed/](seed/) | Idempotent SQL applied to the fresh test database before each cycle |
| [.mcp.json](.mcp.json) | Playwright MCP server (headless Chromium, isolated profile) |
| [.claude/settings.json](.claude/settings.json) | Enabled plugins, tool allow/deny lists |
| `*-testing/`, `qa-toolkit/`, ... | 8 QA skill plugins (63 skills) served by the local marketplace in `.claude-plugin/` |
| `.qa/` | Per-run output (gitignored, uploaded as a workflow artifact) |

## Scripts

| Script | Who calls it | Does |
| --- | --- | --- |
| `app-up.sh` / `app-down.sh` | workflow | Build, start, health-check and seed an isolated app stack (`$QA_PROJECT`); destroy it and SonarQube |
| `db.sh seed\|query\|file\|psql` | workflow, agent | psql inside the test `db` container |
| `collect-context.sh` | workflow | PR metadata, linked issue, comments, diff → `.qa/` |
| `sonar.sh up\|scan\|issues\|down\|all` | workflow | SonarQube in Docker → `.qa/sonar-issues.json` + summary |
| `run-claude.sh` | workflow | The headless QA cycle |
| `publish-screenshots.sh` | agent | Push `.qa/screenshots` to the `qa-artifacts` branch, return URLs |
| `post-comment.sh <marker> <md>` | agent | Upsert a PR comment (summary, one per bug) |
| `post-review.sh <json>` | agent | PR review with inline comments (COMMENT / REQUEST_CHANGES) |
| `commit-tests.sh [msg]` | agent | Commit `tests/` + `seed/` and push to the PR branch |
| `install-plugins.sh` | workflow, once per runner | Install QA plugins + superpowers at user scope |
| `provision-runner.sh` | you, once per VM | Node, Playwright deps, Claude Code, sysctl for SonarQube |

## QA skill plugins

| Plugin | Focus |
| --- | --- |
| test-strategy | Test planning, risk-based testing, coverage analysis |
| functional-testing | Equivalence partitioning, boundary analysis, state transitions |
| automation | Frameworks, page object model, CI integration, flaky tests |
| performance-testing | Load testing, stress testing, capacity planning |
| api-testing | Contract testing, integration, mocks, schema validation |
| quality-metrics | Defect metrics, dashboards, release readiness |
| accessibility-testing | WCAG compliance, screen readers, keyboard navigation |
| qa-toolkit | Bug reports, exploratory testing, regression strategy |

From the `qa-engineer` set of [claude-skills](https://github.com/sethdford/claude-skills).
Standards referenced: ISTQB, ISO/IEC 25010, ISO/IEC/IEEE 29119, WCAG 2.2, IEEE 829.

## Local use

```bash
npm ci && npx playwright install chromium
npm run test:regression            # against BASE_URL (default http://localhost:8080)
npm run test:critical              # only @critical flows
claude                             # interactive, with plugins + Playwright MCP
```

Running the full cycle by hand is described at the end of [CLAUDE.md](CLAUDE.md).
