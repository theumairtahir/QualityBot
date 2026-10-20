# QualityBot

An example project showing how to run a **Quality Assurance bot** on every pull request using GitHub Actions, a self-hosted runner, and Claude.

When a PR is opened or updated, the bot checks out the branch, reviews the code, runs static analysis, tests the running app in a real browser, and reports back on the PR.

> **Status:** work in progress. The layout and plugins are in place; the runner setup and workflow are described below as the target design.

## What the bot does

For each pull request, the workflow on the self-hosted runner:

1. **Checks out the PR branch** and builds/starts the app.
2. **Reviews the code** with Claude, using the code-review and related skills from [superpowers](https://github.com/obra/superpowers) (`requesting-code-review`, `receiving-code-review`, `systematic-debugging`, `verification-before-completion`, etc.).
3. **Runs SonarQube analysis** against the branch, using SonarQube in Docker, and reports the issues it finds.
4. **Tests the application** with Claude, driving a real browser through the Playwright MCP server and applying the QA skills in [quality-assurance/](quality-assurance/) (test design, boundary analysis, exploratory testing, accessibility, API testing, ...).
5. **Reports findings on the PR**: bugs are posted as review comments with screenshots attached.
6. **Grows the regression suite**: critical flows the agent exercises are saved as Playwright scripts in a test project, so later PRs can replay them as regression checks.

```text
 Pull request
      │
      ▼
 GitHub Actions ──► Self-hosted runner
                      ├─ Claude Code + superpowers skills ──► code review
                      ├─ SonarQube (Docker)               ──► static analysis
                      └─ Claude + Playwright MCP + QA skills
                             ├─► bugs + screenshots ──► PR review comments
                             └─► critical flows     ──► regression test project
```

## Repository layout

| Path | Purpose |
| --- | --- |
| [quality-assurance/](quality-assurance/) | Claude sub-project for the QA bot: 8 QA plugins (63 skills), the Playwright MCP plugin, and its own `.claude/settings.json`. Start Claude Code from this folder to load them. |
| [development/](development/) | The sample app under test: a Student Information System (.NET 10 API, Blazor WebAssembly, PostgreSQL, Docker Compose). |
| [docs/](docs/) | Design specs and implementation plans for the sample application under test. |

### The `quality-assurance` sub-project

It is a local plugin marketplace named `quality-assurance` (see [marketplace.json](quality-assurance/.claude-plugin/marketplace.json)), enabled through [quality-assurance/.claude/settings.json](quality-assurance/.claude/settings.json).

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
| playwright-mcp | Playwright MCP server (`npx @playwright/mcp`) for browser automation |

The QA plugins come from the `qa-engineer` set of the [claude-skills](https://github.com/sethdford/claude-skills) collection. Screenshots and other Playwright output are git-ignored inside this folder.

## Runner prerequisites

The self-hosted runner needs the following installed:

- GitHub Actions runner, registered to this repository
- [Claude Code](https://docs.claude.com/en/docs/claude-code) CLI, authenticated for non-interactive use
- GitHub CLI (`gh`), authenticated to post review comments
- Node.js (for `npx @playwright/mcp`) and Playwright browsers (`npx playwright install`)
- Docker Engine, to run SonarQube
- The [superpowers](https://github.com/obra/superpowers) skills, available to Claude on the runner
- Whatever toolchain the application under test needs (the sample SIS app is .NET)

## Usage

1. Provision the runner and install the prerequisites above.
2. Add the repository secrets the workflow needs (Claude credentials, SonarQube token, GitHub token).
3. Add the GitHub Actions workflow that triggers on `pull_request` and runs on the self-hosted runner.
4. Open a PR and review the bot's comments.

To try the QA sub-project locally:

```bash
cd quality-assurance
npm ci                                  # Playwright, Playwright MCP, TypeScript
npx playwright install --with-deps chromium
claude
```

Then run `/plugin` to confirm the plugins and the Playwright MCP server are loaded.

The regression suite lives in `quality-assurance/tests/regression` and runs against `BASE_URL` (default `http://localhost:8080`, the `web` container):

```bash
BASE_URL=http://localhost:8080 npm run test:regression
npm run report                          # open the HTML report
```

## Security notes

A self-hosted runner executes PR code on your own machine, and the agent can run browsers and shell commands. Only run it on trusted branches or require approval for PRs from forks, keep the runner isolated, and scope the tokens it holds as narrowly as possible.
