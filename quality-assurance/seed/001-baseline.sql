-- 001-baseline: shared baseline for all regression tests.
-- The app shell (step 1) has no business entities yet, so there is nothing to insert. This file
-- exists so `db.sh seed` has something to run and so the first feature PR has a template to follow:
--
--   INSERT INTO students (id, first_name, last_name, email)
--   VALUES ('00000000-0000-0000-0000-0000000000a1', 'QA', 'Student', 'qa-student@qa.example')
--   ON CONFLICT (id) DO NOTHING;
--
-- Replace this file's body (or add 002-<feature>.sql) once the schema exists.
SELECT 1;
