-- 002-students-list-edit: fixed rows for the student list search tests (PR #16, issues #3/#4).
-- Used by tests/regression/students-list.spec.ts (search, roll number, empty state). Edit/remove
-- tests create their own rows via the API, so nothing here is ever modified or deleted.
-- roll_number is an identity column and is assigned by the database.
INSERT INTO students (id, first_name, last_name, date_of_birth, email, class_name, created_at_utc)
VALUES
  ('00000000-0000-0000-0000-0000000000a1', 'Qa', 'Searchable', '2012-05-01', 'qa-searchable@qa.example', 'Grade 3', now()),
  ('00000000-0000-0000-0000-0000000000a2', 'Zoë', 'Qaumlaut', NULL, NULL, 'Grade 1', now()),
  ('00000000-0000-0000-0000-0000000000a3', 'Percent', 'Qawildcard', NULL, 'qa-percent@qa.example', 'Grade 6', now())
ON CONFLICT (id) DO NOTHING;
