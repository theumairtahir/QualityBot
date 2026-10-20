# Seed data

SQL applied to the fresh test database at the start of every QA cycle (`scripts/db.sh seed`), after
the API has run its migrations. Regression tests may assume these rows exist.

Rules:

- Files run in name order: `NNN-<topic>.sql`. Add a new file rather than editing an old one unless
  you are fixing a mistake.
- Every statement must be **idempotent** (`INSERT ... ON CONFLICT DO NOTHING`, `WHERE NOT EXISTS`),
  because the agent may re-seed during a cycle.
- Seeds only add baseline data; they never create tables or alter schema. That belongs to the app's
  migrations in `development/`.
- Keep rows recognisable (`qa-` prefixes, `@qa.example` emails) so test data is never mistaken for
  real data.
- Start each file with a comment naming the feature and the tests that rely on it.
