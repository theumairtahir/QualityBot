# Application map

Student Information System: Blazor WebAssembly client (`web`, port 8080) → minimal API (`api`,
port 8081) → PostgreSQL 18 (`db`, host port 5433, database/user/password `sis`).
Seeded from the PR #15 cycle on 2026-10-20; the agent keeps it current.

## Pages (client routes)

| Route | Page | Notes |
| --- | --- | --- |
| `/` | Dashboard | System-info card shows `Connected · v<version> · <environment>` (`Badge` text "Connected") |
| `/students` | Student list | Lists students from `GET /api/students` |
| `/students/new` | Add student | Form: first name, last name, date of birth, email, class. Save button `data-testid="save-student"`. Success → toast + redirect to `/students` |
| `/staff`, `/fees`, `/reports`, `/settings` | Module placeholders | Administration group in the sidebar; no behaviour yet |
| any other | Not found | `<h1>` "Page not found"; the title also appears in breadcrumb and header (`data-testid="page-title"`), so locate by heading role |

Shell: `SidebarLayout` with a `<nav>` sidebar and header (TailwindUi.Blazor). Dark mode classes present.

## API endpoints

| Method + path | Purpose | Notes |
| --- | --- | --- |
| `GET /health` | Health check incl. DB | Returns plain `Healthy` |
| `GET /api/system/info` | Version / environment for the dashboard | |
| `GET /api/students` | List students | |
| `POST /api/students` | Create student | 400 with an `errors` map on validation failure; `Location: /api/students/{id}` but no `GET /api/students/{id}` exists (PR #15) |

CORS allows origin `http://localhost:8080` only.

## Database

| Table | Columns of note |
| --- | --- |
| `students` | `email varchar(254)`; names trimmed on save; `class_name` not trimmed (PR #15 review) |

Migrations run automatically when the API starts (`Database__ApplyMigrationsOnStartup=true`).

## Validation messages (exact text, for locators)

- "First name is required." · "Last name is required." · "Class is required."
- Email format is validated client- and server-side; **length is not** (see known-issues).
- Generic failure banner: "The API couldn't be reached. Check that it is running and try again." —
  shown for *any* non-2xx, not only network errors (PR #15 review).

## Test IDs and selectors that work

- `getByTestId('save-student')`, `getByTestId('page-title')`
- Form inputs have `name` attributes (`first-name`, …) but **no `id`**, so `getByLabel` does not
  work yet (known issue `form-labels-not-associated`); use `getByRole('textbox', { name })` once
  fixed, placeholders/names until then.
