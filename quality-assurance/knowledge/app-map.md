# Application map

Student Information System: Blazor WebAssembly client (`web`, port 8080) → minimal API (`api`,
port 8081) → PostgreSQL 18 (`db`, host port 5433, database/user/password `sis`).
Seeded from the PR #15 cycles (2026-10-20 / 2026-10-21); the agent keeps it current.

## Pages (client routes)

| Route | Page | Notes |
| --- | --- | --- |
| `/` | Dashboard | System-info card shows `Connected · v<version> · <environment>` (`Badge` text "Connected") |
| `/students` | Student list | `GET /api/students`; table (aria-label "Students") with Name, Class, Email, Date of birth (`d MMM yyyy`, "—" when empty); empty state heading "No students yet"; "Add student" button `data-testid="add-student"`; error banner "Couldn't load students" |
| `/students/new` | Add student | Form `data-testid="student-form"`: first name, last name, date of birth, email, class. Save button `data-testid="save-student"`, Cancel link → `/students`. Success → toast "Student added" / "<First> <Last> is now on record." + redirect to `/students` |
| `/staff`, `/fees`, `/reports`, `/settings`, `/admissions`, `/classes`, … | Module placeholders | No behaviour yet |
| any other | Not found | `<h1>` "Page not found"; the title also appears in breadcrumb and header (`data-testid="page-title"`), so locate by heading role |

Shell: `SidebarLayout` with a `<nav>` "Main" sidebar and header (TailwindUi.Blazor). Dark mode classes present.

## API endpoints

| Method + path | Purpose | Notes |
| --- | --- | --- |
| `GET /health` | Health check incl. DB | Returns plain `Healthy` |
| `GET /api/system/info` | Version / environment for the dashboard | |
| `GET /api/students` | List students | Ordered by last name, then first name; no paging |
| `POST /api/students` | Create student | Body `firstName, lastName, dateOfBirth (yyyy-MM-dd), email, className`. 201 + JSON body; 400 ProblemDetails with `errors` map on validation failure (keys `FirstName`, `LastName`, `Email`, `DateOfBirth`, `ClassName`); malformed JSON/date → 400 **without** `errors`; no body content type → 415. `Location: /api/students/{id}` but no `GET /api/students/{id}` exists (404) |

CORS allows origin `http://localhost:8080` only (foreign `Origin` gets no CORS headers; the request is still processed).

## Database

| Table | Columns of note |
| --- | --- |
| `students` | `id uuid`, `first_name`/`last_name varchar(100)` (trimmed on save), `date_of_birth date null`, `email varchar(254) null` (trimmed; empty → NULL), `class_name varchar(50)`, `created_at_utc timestamptz` |

Migrations run automatically when the API starts (`Database__ApplyMigrationsOnStartup=true`).

## Validation rules and messages (exact text, for locators)

Shared by client and API (`CreateStudentRequest.Validate`); the form validates client-side first and
only POSTs when the model is valid.

- "First name is required." · "Last name is required." · "Class is required."
- "First name must be at most 100 characters." (same for Last name) — 100 accepted, 101 rejected.
- "First name can only contain letters, spaces, hyphens and apostrophes." (same for Last name) —
  regex `^\p{L}[\p{L}\p{M} '’-]*$` on the trimmed value: Unicode letters/marks OK, digits/period/
  underscore/HTML rejected, leading hyphen/apostrophe rejected, trailing hyphen accepted.
- "Enter a valid email address." — `MailAddress`-based; see known-issues `email-format-loopholes`.
- "Email must be at most 254 characters." — 254 accepted, 255 rejected.
- "Date of birth can't be in the future." (server; client picker shows its own "Choose a date on or
  before M/d/yyyy." and "Enter a date as M/d/yyyy." — see known-issues `future-dob-silently-dropped`).
- "Pick a class from the list." — classes are exactly `Grade 1` … `Grade 10` (case- and space-sensitive).
- Generic failure banner: "Couldn't save the student" / "The API couldn't be reached. Check that it is
  running and try again." — shown for *any* non-2xx other than 400, not only network errors.

## Test IDs and selectors that work

- `getByTestId('save-student')`, `getByTestId('add-student')`, `getByTestId('student-form')`,
  `getByTestId('student-list')`, `getByTestId('page-title')`.
- Inputs: `input[name="first-name"]`, `input[name="last-name"]`, `input[name="email"]`,
  `select[name="class-name"]` (hidden disabled placeholder option "Select a class"). `getByLabel`
  does **not** work for these (duplicate ids, known issue `form-labels-not-associated`).
- Date of birth: custom picker, `#date-of-birth` (`role=combobox`, accessible name "Date of birth",
  placeholder `M/d/yyyy`); `fill('5/1/2012')` or ISO `2012-05-01` then Tab both work; buttons
  "Open the calendar for Date of birth" and "Clear Date of birth".
- Toast text exists twice (visible paragraph + live region): use `getByText(..., { exact: true })`
  or `getByText('Student added', { exact: true })`.
- List rows: `getByRole('row', { name: /First Last/ })`.
