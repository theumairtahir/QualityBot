# Student Information System (demo)

A demo SIS built step by step. This step contains the architecture and the app shell only.

| Project | What it is |
| --- | --- |
| `src/Sis.Api` | ASP.NET Core minimal API, vertical slices under `Features/`, EF Core + Postgres |
| `src/Sis.Web` | Blazor WebAssembly client using `TailwindUi.Blazor` |
| `src/Sis.Contracts` | DTOs shared by the API and the client |
| `tests/*` | xUnit unit tests (bUnit for components) |

## Prerequisites

- .NET 10 SDK, Docker
- Access to the `TailwindUi.Blazor` package on the NimbleLoop-Technologies GitHub Packages feed:
  create a classic PAT with `read:packages` (SSO-authorise it if the org enforces SSO), then set
  `GH_PACKAGES_USER` (your GitHub username) and `GH_PACKAGES_TOKEN` (the PAT) as environment variables.

## Run with Docker

```bash
docker compose up --build
```

| URL | Service |
| --- | --- |
| <http://localhost:8080> | web (nginx) |
| <http://localhost:8081/api/system/info> | API |
| <http://localhost:8081/health> | API + database health |
| `localhost:5433` | Postgres (`sis` / `sis`, demo only) |

The web image reads `API_BASE_URL` at container start and writes it into `appsettings.json`,
so the same image can point at any API.

## Run locally

```bash
docker compose up -d db
dotnet run --project src/Sis.Api   # http://localhost:5081
dotnet run --project src/Sis.Web   # http://localhost:5080
```

### In VS Code

Open the repository root and install the recommended extensions when prompted.

- **Run and Debug → `SIS: DB + API + Web`** (F5) starts Postgres, builds the solution, then
  debugs the API and the Blazor client together (the client opens in Edge). Stopping one stops both.
- `Sis.Api` or `Sis.Web` debugs one app on its own.
- **Terminal → Run Task** offers `sis: watch all` (hot reload, no debugger), `sis: test`,
  `sis: db up` / `sis: db down` and `sis: docker up` (the full Compose stack).

## Test

```bash
dotnet test Sis.slnx
```

## Adding a feature

- **API:** add `src/Sis.Api/Features/<Feature>/<UseCase>.cs` with a class implementing
  `IEndpoint`. It is discovered automatically and mapped under `/api`. Put request and response
  DTOs in `Sis.Contracts`.
- **Web:** build the page and delete its route from `Pages/ModulePlaceholder.razor`. Add or rename
  menu entries in `Layout/AppMenu.cs`; the routing test fails if a menu link has no page.
- **Styling:** the app uses the library's precompiled `tailwindui.css`. A utility class that
  isn't in that bundle silently does nothing. Add an app-owned Tailwind v4 build when page
  work needs one.

### About `wwwroot/lib/tailwindui/`

`TailwindUi.Blazor` ships its CSS bundle as a NuGet content file rather than a static web asset,
so it is not served from `_content/TailwindUi.Blazor/`. `Sis.Web.csproj` copies it from the
restored package into `wwwroot/lib/tailwindui/` on every build. That folder is git-ignored:
the bundle is Tailwind Plus licensed and this repository is public, so never commit it.

## CI

`.github/workflows/development-ci.yml` restores, builds (warnings as errors) and runs the unit
tests on pushes to `main` and on pull requests that touch `development/`. It needs the
repository secret **`GH_PACKAGES_TOKEN`**: a PAT with `read:packages` for NimbleLoop-Technologies.
