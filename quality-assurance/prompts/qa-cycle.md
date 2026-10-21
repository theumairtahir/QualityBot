Run the full QA cycle for pull request #${PR_NUMBER} (${PR_URL}) exactly as described in CLAUDE.md.

Context already collected for you in `.qa/`: `pr.json`, `issue.json`, `pr-comments.json`,
`diff.patch`, `changed-files.txt`, `sonar-issues.json` (if SonarQube succeeded) and
`regression-results.json`. The application is running: web at ${QA_WEB_URL}, API at ${QA_API_URL},
database seeded from `seed/`. This run: ${QA_RUN_URL}.

Work through the six phases in order and write each phase's output file before starting the next.
Finish by posting the bug comments, the review, the summary comment, and writing `.qa/verdict.json`.
If you are running out of budget, skip remaining test cases rather than the report: a partial
cycle with an honest summary is better than a complete cycle nobody hears about.
