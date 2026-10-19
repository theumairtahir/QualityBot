# Student Information System — architecture & app shell (step 1)

**Date:** 2026-10-19
**Status:** Approved
**Location:** `development/`

## Goal

Stand up a demo Student Information System (SIS) that will be built feature by feature. This step
delivers only the **architecture** (solution layout, API skeleton, persistence wiring, Docker, CI) and
the **app shell** (sidebar menu + header). No business entities, no auth.

## Stack

- .NET 10 (SDK 10.0.x), `Sis.slnx` solution, central package management
- Blazor WebAssembly (standalone) using the in-house **`TailwindUi.Blazor`** package
- ASP.NET Core minimal API, EF Core 10 + Npgsql, PostgreSQL 18
- Docker Compose: `db` + `api` + `web` (nginx)
- GitHub Actions: build + unit tests
- Tests: xUnit, bUnit, AwesomeAssertions (matches the TailwindUi.Blazor repo)

## Decisions

| Topic | Decision |
|---|---|
| Backend organisation | **Vertical slice** in a single `Sis.Api` project — `Features/<Feature>/` files hold endpoint + handler + data access. No MediatR. |
| Client hosting | **Separate containers** — WASM served by nginx, API in its own container, CORS between them. |
| Shell | **`SidebarLayout` + `Header` slot** from TailwindUi.Blazor. |
| Naming | `Sis.*` projects. |
| Styling | Library's precompiled `tailwindui.css` only. An app-owned Tailwind v4 build is added when page work needs utilities the bundle lacks. |

## Solution layout

```
development/
  Sis.slnx
  Directory.Build.props        net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors
  Directory.Packages.props     central package versions
  nuget.config                 nuget.org + NimbleLoop GitHub Packages feed, package-source mapping
  docker-compose.yml
  .dockerignore
  README.md
  src/
    Sis.Contracts/             DTOs shared by Api and Web; no dependencies
    Sis.Api/
      Program.cs
      Common/IEndpoint.cs      slices implement it; registered by assembly scan
      Features/System/GetSystemInfo.cs
      Persistence/SisDbContext.cs
      Dockerfile
    Sis.Web/
      Program.cs, App.razor, _Imports.razor
      Layout/MainLayout.razor, Layout/AppMenu.cs
      Pages/Dashboard.razor, Pages/ModulePlaceholder.razor, Pages/NotFound.razor
      Services/SystemInfoClient.cs
      wwwroot/index.html, wwwroot/appsettings*.json
      Dockerfile, nginx.conf, docker-entrypoint.d/
  tests/
    Sis.Api.Tests/
    Sis.Web.Tests/
.github/workflows/development-ci.yml   (repo root — required by GitHub)
```

## Backend (`Sis.Api`)

- **Slice pattern:** each feature file contains a class implementing `IEndpoint`
  (`void MapEndpoint(IEndpointRouteBuilder app)`) plus its handler. `Program.cs` calls
  `AddEndpoints()` / `MapEndpoints()`, which scan the assembly — adding a feature never touches
  `Program.cs`. Handlers are plain classes registered in DI and injected into the endpoint delegate.
- **First slice — `GET /api/system/info`** → `SystemInfoResponse(ApplicationName, Version,
  Environment, ServerTimeUtc)` from `Sis.Contracts`. Time comes from `TimeProvider` so the handler
  is unit-testable. Purpose: prove WASM → nginx → CORS → API end to end, and serve as the template
  for future slices.
- **Persistence:** `SisDbContext` (no entity sets yet), Npgsql provider, snake_case naming
  convention, connection string `ConnectionStrings:Sis`. When
  `Database:ApplyMigrationsOnStartup` is `true` (Development + Docker), `MigrateAsync()` runs at
  startup. The first migration arrives with the first entity in a later step.
- **Health:** `/health` with a DbContext check against Postgres; used by Docker.
- **Errors:** `AddProblemDetails()` + exception handler → RFC 7807 responses.
- **CORS:** allowed origins from `Cors:AllowedOrigins` (array).
- **OpenAPI:** built-in `MapOpenApi()` in Development only.

## Frontend (`Sis.Web`)

- **Host page:** Inter font, `_content/TailwindUi.Blazor/css/tailwindui.css`,
  `@tailwindplus/elements` script, and the no-flash theme script from the library's getting-started.
- **`AppMenu`:** static model (sections → items with label, href, icon). Single source of truth for
  the sidebar, the mobile title, and the header page title. Initial menu:
  - **Overview:** Dashboard (`/`)
  - **Students:** Students (`/students`), Admissions (`/admissions`)
  - **Academics:** Classes (`/classes`), Courses (`/courses`), Attendance (`/attendance`), Grades (`/grades`)
  - **Administration:** Staff (`/staff`), Fees (`/fees`), Reports (`/reports`), Settings (`/settings`)
- **`MainLayout`:** `SidebarLayout` — `Sidebar` slot holds the brand block and a `NavList`
  rendered from `AppMenu`; `Header` slot holds the current page title, a `ThemeSwitcher`, and a
  user-menu dropdown (Profile / Settings / Sign out — placeholders until auth). `MobileTitle` is
  the current page title. `ToastHost` is mounted once; `@Body` is wrapped in an `ErrorBoundary`.
- **`ModulePlaceholder.razor`:** one `@page` directive per unbuilt module; renders a "coming soon"
  empty state titled from `AppMenu`. A module's route is removed from here when it is built.
- **`Dashboard.razor`:** placeholder heading + one status card using `SystemInfoClient`:
  "Connected · v{Version} · {Environment}" or "API unreachable" on failure.
- **Config:** `wwwroot/appsettings.json` → `Api:BaseUrl`. Locally points at the API's dev URL; in
  Docker the nginx entrypoint rewrites it from `API_BASE_URL` at container start.

## Package feed

`TailwindUi.Blazor` lives on the private NimbleLoop GitHub Packages feed; this repo is public and
outside that org, so:

- `nuget.config` declares the feed with credentials from `%GH_PACKAGES_USER%` /
  `%GH_PACKAGES_TOKEN%` and **package-source mapping** (`TailwindUi.*` → NimbleLoop, `*` →
  nuget.org). Projects that don't use the library restore without credentials.
- The package (or any Tailwind Plus source) is **never committed** — licence requirement.

## Docker

| Service | Image | Host port | Notes |
|---|---|---|---|
| `db` | `postgres:18` | 5432 | named volume, `pg_isready` healthcheck |
| `api` | multi-stage → `mcr.microsoft.com/dotnet/aspnet:10.0` | 8081 | `depends_on: db (healthy)`; env connection string, CORS origin `http://localhost:8080`, migrations on |
| `web` | multi-stage → `nginxinc/nginx-unprivileged:alpine` | 8080 | SPA fallback, `.wasm` MIME, gzip, no-cache for `index.html` / `appsettings.json`; entrypoint writes `appsettings.json` from `API_BASE_URL` |

- Build context is `development/` so `Directory.*.props`, `nuget.config` and `Sis.Contracts` are
  available to both images.
- The web image restores with BuildKit secrets
  (`RUN --mount=type=secret,id=...,env=...`); Compose sources them from the host's
  `GH_PACKAGES_USER` / `GH_PACKAGES_TOKEN`. Credentials never land in an image layer.

## CI — `.github/workflows/development-ci.yml`

- Triggers: push to `main`, pull requests; path filter `development/**` and the workflow file.
- `ubuntu-latest`, `actions/setup-dotnet` 10.0.x → restore → build `-c Release` → test; TRX results
  uploaded as an artifact.
- Feed credentials via env: `GH_PACKAGES_USER: ${{ github.repository_owner }}`,
  `GH_PACKAGES_TOKEN: ${{ secrets.GH_PACKAGES_TOKEN }}`.
- **Prerequisite (manual):** a classic PAT with `read:packages` (SSO-authorised for NimbleLoop if
  enforced) stored as repo secret `GH_PACKAGES_TOKEN`. Fork PRs lack the secret and will fail
  restore — accepted.

## Unit tests

- **`Sis.Api.Tests`:** `GetSystemInfo` handler returns configured name/version/environment and the
  `FakeTimeProvider` time.
- **`Sis.Web.Tests`** (bUnit, loose JS interop):
  - every `AppMenu` href has a matching `@page` route in `Sis.Web` (no dead links);
  - `AppMenu` hrefs are unique;
  - `MainLayout` renders every menu label, and the header shows the title for the current path;
  - `Dashboard` status card renders both the connected and the unreachable state.

## Out of scope

Authentication/authorisation, domain entities and migrations, app-owned Tailwind build, Docker
image builds in CI, deployment.
