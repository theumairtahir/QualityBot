# SIS Architecture & App Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the demo Student Information System in `development/` — a vertical-slice ASP.NET Core API on Postgres, a Blazor WASM client with a TailwindUi.Blazor sidebar shell, Docker Compose, and a GitHub Actions build + unit-test workflow.

**Architecture:** `Sis.Api` is a single minimal-API project; each feature is one file under `Features/<Feature>/` implementing `IEndpoint`, discovered by assembly scan. `Sis.Web` is a standalone Blazor WASM app served by nginx, talking to the API over CORS through a runtime-configured base URL. `Sis.Contracts` holds DTOs shared by both. The shell is `SidebarLayout` driven by a single `AppMenu` model.

**Tech Stack:** .NET 10 (runtime 10.0.12), Blazor WebAssembly, TailwindUi.Blazor `2026.10.13.29` (private NimbleLoop GitHub Packages feed), EF Core 10 + Npgsql 10, PostgreSQL 18, nginx-unprivileged, Docker Compose, GitHub Actions, xUnit 2.9 + bUnit 2 + AwesomeAssertions.

**Spec:** [docs/superpowers/specs/2026-10-19-sis-app-shell-design.md](../specs/2026-10-19-sis-app-shell-design.md)

---

## Prerequisites (check once before Task 1)

- .NET SDK 10.0.x: `dotnet --list-sdks` shows a `10.0.` line.
- Env vars `GH_PACKAGES_USER` and `GH_PACKAGES_TOKEN` are set (PAT with `read:packages` for `NimbleLoop-Technologies`). Without them `Sis.Web` cannot restore `TailwindUi.Blazor`.
- Docker Desktop running (only needed for Task 10).
- Work on branch `feature/sis-app-shell`. All paths below are relative to the repo root; shell commands run from `development/` unless stated.

**Licence rule:** never commit the `TailwindUi.Blazor` package, its CSS, or any Tailwind Plus source into this public repo. Consume it from the feed only.

## File map

```text
development/
  .dockerignore                      Docker build-context exclusions
  .gitattributes                     forces LF on shell scripts
  Directory.Build.props              shared MSBuild settings (net10.0, nullable, warnings-as-errors, version)
  Directory.Packages.props           central package versions
  nuget.config                       nuget.org + NimbleLoop feed with source mapping
  Sis.slnx                           solution
  docker-compose.yml                 db + api + web
  README.md                          how to run / test / configure CI
  src/
    Sis.Contracts/
      Sis.Contracts.csproj
      SystemInfo/SystemInfoResponse.cs
    Sis.Api/
      Sis.Api.csproj
      Program.cs                     composition root
      appsettings.json, appsettings.Development.json
      Properties/launchSettings.json
      Common/IEndpoint.cs            slice contract
      Common/EndpointExtensions.cs   AddEndpoints / MapEndpoints (assembly scan)
      Features/SystemInfo/GetSystemInfo.cs
      Persistence/SisDbContext.cs
      Persistence/PersistenceExtensions.cs   AddPersistence / ApplyMigrationsAsync
      Dockerfile
    Sis.Web/
      Sis.Web.csproj
      Program.cs, App.razor, _Imports.razor
      Properties/launchSettings.json
      Layout/AppMenu.cs              menu model (single source of truth)
      Layout/MainLayout.razor        SidebarLayout shell
      Layout/UserMenu.razor          header avatar dropdown
      Pages/Dashboard.razor          "/" + API status card
      Pages/ModulePlaceholder.razor  routes for unbuilt modules
      Pages/NotFound.razor
      Services/SystemInfoClient.cs
      wwwroot/index.html, wwwroot/css/app.css, wwwroot/appsettings.json, wwwroot/icon-192.png
      nginx.conf
      docker-entrypoint.d/40-sis-config.sh
      Dockerfile
  tests/
    Sis.Api.Tests/
      Sis.Api.Tests.csproj
      Common/EndpointExtensionsTests.cs
      Features/SystemInfo/GetSystemInfoTests.cs
    Sis.Web.Tests/
      Sis.Web.Tests.csproj
      Support/StubHttp.cs
      Layout/AppMenuTests.cs
      Layout/MainLayoutTests.cs
      Pages/RoutingTests.cs
      Pages/DashboardTests.cs
      Pages/ModulePlaceholderTests.cs
      Services/SystemInfoClientTests.cs
.github/workflows/development-ci.yml
```

---

### Task 1: Solution scaffolding + contracts project

**Files:**

- Create: `development/Directory.Build.props`
- Create: `development/Directory.Packages.props`
- Create: `development/nuget.config`
- Create: `development/.gitattributes`
- Create: `development/.dockerignore`
- Create: `development/Sis.slnx` (via CLI)
- Create: `development/src/Sis.Contracts/Sis.Contracts.csproj`
- Create: `development/src/Sis.Contracts/SystemInfo/SystemInfoResponse.cs`

- [ ] **Step 1: Write `development/Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Vulnerability advisories stay visible as warnings instead of breaking every build. -->
    <WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Write `development/Directory.Packages.props`**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup Label="Web">
    <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.12" />
    <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.12" />
    <PackageVersion Include="TailwindUi.Blazor" Version="2026.10.13.29" />
  </ItemGroup>

  <ItemGroup Label="Api">
    <PackageVersion Include="EFCore.NamingConventions" Version="10.0.1" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.12" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.12" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>

  <ItemGroup Label="Tests">
    <PackageVersion Include="AwesomeAssertions" Version="9.6.0" />
    <PackageVersion Include="bunit" Version="2.12.5" />
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.11.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.11.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Write `development/nuget.config`**

Package-source mapping sends only `TailwindUi.*` to the private feed, so projects that don't use it (`Sis.Api`, `Sis.Contracts`) restore without credentials.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="nimbleloop" value="https://nuget.pkg.github.com/NimbleLoop-Technologies/index.json" />
  </packageSources>

  <!-- Read from the environment: never commit a token. Locally set GH_PACKAGES_USER /
       GH_PACKAGES_TOKEN; CI and the web Docker build pass them in. -->
  <packageSourceCredentials>
    <nimbleloop>
      <add key="Username" value="%GH_PACKAGES_USER%" />
      <add key="ClearTextPassword" value="%GH_PACKAGES_TOKEN%" />
    </nimbleloop>
  </packageSourceCredentials>

  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="nimbleloop">
      <package pattern="TailwindUi.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

- [ ] **Step 4: Write `development/.gitattributes` and `development/.dockerignore`**

`.gitattributes`:

```text
# Shell scripts run inside Linux containers; CRLF breaks their shebang.
*.sh text eol=lf
```

`.dockerignore`:

```text
**/bin/
**/obj/
**/TestResults/
**/.vs/
**/*.user
tests/
```

- [ ] **Step 5: Write the contracts project**

`development/src/Sis.Contracts/Sis.Contracts.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

`development/src/Sis.Contracts/SystemInfo/SystemInfoResponse.cs`:

```csharp
namespace Sis.Contracts.SystemInfo;

/// <summary>What <c>GET /api/system/info</c> returns: which API build is answering, and where.</summary>
public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
```

- [ ] **Step 6: Create the solution and add the project**

Run (from `development/`):

```bash
dotnet new sln -n Sis --format slnx
dotnet sln Sis.slnx add src/Sis.Contracts/Sis.Contracts.csproj --solution-folder src
```

Expected: `Sis.slnx` created; "Project ... added to the solution."

- [ ] **Step 7: Build**

Run: `dotnet build Sis.slnx`
Expected: `Build succeeded.` with 0 warnings, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add development
git commit -m "chore(sis): scaffold solution, central packages, feed config and contracts"
```

---

### Task 2: API skeleton with slice discovery

**Files:**

- Create: `development/src/Sis.Api/Sis.Api.csproj`
- Create: `development/src/Sis.Api/Common/IEndpoint.cs`
- Create: `development/src/Sis.Api/Common/EndpointExtensions.cs`
- Create: `development/src/Sis.Api/Program.cs`
- Create: `development/src/Sis.Api/appsettings.json`
- Create: `development/src/Sis.Api/appsettings.Development.json`
- Create: `development/src/Sis.Api/Properties/launchSettings.json`
- Create: `development/tests/Sis.Api.Tests/Sis.Api.Tests.csproj`
- Test: `development/tests/Sis.Api.Tests/Common/EndpointExtensionsTests.cs`

- [ ] **Step 1: Create the API project file**

`development/src/Sis.Api/Sis.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Sis.Contracts\Sis.Contracts.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the API test project**

`development/tests/Sis.Api.Tests/Sis.Api.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AwesomeAssertions" />
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Sis.Api\Sis.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="AwesomeAssertions" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write the failing test**

`development/tests/Sis.Api.Tests/Common/EndpointExtensionsTests.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sis.Api.Common;

namespace Sis.Api.Tests.Common;

public sealed class EndpointExtensionsTests
{
    [Fact]
    public void AddEndpoints_registers_every_concrete_endpoint_in_the_assembly()
    {
        var services = new ServiceCollection();

        services.AddEndpoints(typeof(EndpointExtensionsTests).Assembly);

        services.Where(descriptor => descriptor.ServiceType == typeof(IEndpoint))
            .Select(descriptor => descriptor.ImplementationType)
            .Should().ContainSingle()
            .Which.Should().Be<PingEndpoint>();
    }

    private sealed class PingEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/ping", () => "pong");
    }

    // Must be skipped by the scan: it cannot be constructed.
    private abstract class AbstractEndpoint : IEndpoint
    {
        public abstract void MapEndpoint(IEndpointRouteBuilder app);
    }
}
```

- [ ] **Step 4: Add both projects to the solution and run the test to verify it fails**

```bash
dotnet sln Sis.slnx add src/Sis.Api/Sis.Api.csproj --solution-folder src
dotnet sln Sis.slnx add tests/Sis.Api.Tests/Sis.Api.Tests.csproj --solution-folder tests
dotnet test tests/Sis.Api.Tests
```

Expected: build FAILS — `The type or namespace name 'Common' does not exist in the namespace 'Sis.Api'` (and/or missing `Program`).

- [ ] **Step 5: Write the slice contract and discovery**

`development/src/Sis.Api/Common/IEndpoint.cs`:

```csharp
namespace Sis.Api.Common;

/// <summary>
/// One feature slice's HTTP surface. Implementations are discovered by
/// <see cref="EndpointExtensions.AddEndpoints"/>, so adding a feature never touches Program.cs.
/// </summary>
public interface IEndpoint
{
    /// <summary>Maps the slice's routes. <paramref name="app"/> is already scoped to <c>/api</c>.</summary>
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

`development/src/Sis.Api/Common/EndpointExtensions.cs`:

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sis.Api.Common;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoint(api);
        }

        return app;
    }
}
```

- [ ] **Step 6: Write `Program.cs`, settings and launch profile**

`development/src/Sis.Api/Program.cs`:

```csharp
using Sis.Api.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpoints(typeof(Program).Assembly);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEndpoints();

await app.RunAsync();
```

`development/src/Sis.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": []
  }
}
```

`development/src/Sis.Api/appsettings.Development.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5080" ]
  }
}
```

`development/src/Sis.Api/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5081",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 7: Run the test to verify it passes**

Run: `dotnet test tests/Sis.Api.Tests`
Expected: `Passed!  - Failed: 0, Passed: 1`

- [ ] **Step 8: Commit**

```bash
git add development
git commit -m "feat(api): add minimal API host with IEndpoint slice discovery"
```

---

### Task 3: First slice — `GET /api/system/info`

**Files:**

- Create: `development/src/Sis.Api/Features/SystemInfo/GetSystemInfo.cs`
- Test: `development/tests/Sis.Api.Tests/Features/SystemInfo/GetSystemInfoTests.cs`

- [ ] **Step 1: Write the failing tests**

`development/tests/Sis.Api.Tests/Features/SystemInfo/GetSystemInfoTests.cs`:

```csharp
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Sis.Api.Features.SystemInfo;

namespace Sis.Api.Tests.Features.SystemInfo;

public sealed class GetSystemInfoTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Handle_reports_the_environment_and_server_time()
    {
        var response = GetSystemInfo.Handle(new FakeTimeProvider(Now), new StubHostEnvironment("Staging"));

        response.Environment.Should().Be("Staging");
        response.ServerTimeUtc.Should().Be(Now);
    }

    [Fact]
    public void Handle_reports_the_application_name_and_a_semantic_version()
    {
        var response = GetSystemInfo.Handle(new FakeTimeProvider(Now), new StubHostEnvironment("Development"));

        response.ApplicationName.Should().Be("Student Information System");
        response.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Sis.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Sis.Api.Tests`
Expected: build FAILS — `The type or namespace name 'SystemInfo' does not exist in the namespace 'Sis.Api.Features'`.

- [ ] **Step 3: Write the slice**

`development/src/Sis.Api/Features/SystemInfo/GetSystemInfo.cs`:

```csharp
using System.Reflection;
using Sis.Api.Common;
using Sis.Contracts.SystemInfo;

namespace Sis.Api.Features.SystemInfo;

/// <summary>Reports which API build is answering. Also the reference shape for new slices.</summary>
public sealed class GetSystemInfo : IEndpoint
{
    public const string ApplicationName = "Student Information System";

    // "1.0.0+<commit sha>" → "1.0.0": the source-link suffix is noise on a status card.
    private static readonly string Version =
        (typeof(GetSystemInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/system/info", (TimeProvider timeProvider, IHostEnvironment environment) =>
                Handle(timeProvider, environment))
            .WithName(nameof(GetSystemInfo))
            .WithTags("System");

    public static SystemInfoResponse Handle(TimeProvider timeProvider, IHostEnvironment environment) =>
        new(ApplicationName, Version, environment.EnvironmentName, timeProvider.GetUtcNow());
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Sis.Api.Tests`
Expected: `Passed!  - Failed: 0, Passed: 3`

- [ ] **Step 5: Smoke-test the endpoint**

Run (two terminals, from `development/`):

```bash
dotnet run --project src/Sis.Api
curl -s http://localhost:5081/api/system/info
```

Expected: JSON like `{"applicationName":"Student Information System","version":"1.0.0","environment":"Development","serverTimeUtc":"..."}`. Stop the API afterwards (Ctrl+C).

- [ ] **Step 6: Commit**

```bash
git add development
git commit -m "feat(api): add GET /api/system/info slice"
```

---

### Task 4: Persistence, health check and startup migrations

**Files:**

- Modify: `development/src/Sis.Api/Sis.Api.csproj`
- Create: `development/src/Sis.Api/Persistence/SisDbContext.cs`
- Create: `development/src/Sis.Api/Persistence/PersistenceExtensions.cs`
- Modify: `development/src/Sis.Api/Program.cs`
- Modify: `development/src/Sis.Api/appsettings.Development.json`

No unit test: this task is pure wiring with no logic of our own; it is verified by the build here and against a real Postgres in Task 10.

- [ ] **Step 1: Add the EF Core packages to `Sis.Api.csproj`**

Replace the package `ItemGroup` with:

```xml
  <ItemGroup>
    <PackageReference Include="EFCore.NamingConventions" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
```

- [ ] **Step 2: Write the DbContext**

`development/src/Sis.Api/Persistence/SisDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Sis.Api.Persistence;

public sealed class SisDbContext(DbContextOptions<SisDbContext> options) : DbContext(options)
{
    // Each feature adds its own IEntityTypeConfiguration<T>; nothing to register by hand.
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SisDbContext).Assembly);
}
```

- [ ] **Step 3: Write the persistence wiring**

`development/src/Sis.Api/Persistence/PersistenceExtensions.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Sis.Api.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Sis")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Sis' is not configured.");

        services.AddDbContext<SisDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<SisDbContext>("database");

        return services;
    }

    /// <summary>Applies pending migrations when <c>Database:ApplyMigrationsOnStartup</c> is true.</summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SisDbContext>();
        await db.Database.MigrateAsync();
    }
}
```

- [ ] **Step 4: Wire it into `Program.cs`**

Replace the whole file with:

```csharp
using Sis.Api.Common;
using Sis.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddEndpoints(typeof(Program).Assembly);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapEndpoints();

await app.ApplyMigrationsAsync();
await app.RunAsync();
```

- [ ] **Step 5: Add local-dev database settings**

Replace `development/src/Sis.Api/appsettings.Development.json` with (credentials match the Compose `db` service — demo only):

```json
{
  "ConnectionStrings": {
    "Sis": "Host=localhost;Port=5432;Database=sis;Username=sis;Password=sis"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5080" ]
  }
}
```

- [ ] **Step 6: Build and run all API tests**

Run: `dotnet build Sis.slnx && dotnet test tests/Sis.Api.Tests`
Expected: `Build succeeded.` 0 warnings; `Passed: 3`.

- [ ] **Step 7: Commit**

```bash
git add development
git commit -m "feat(api): add Postgres DbContext, health check and startup migrations"
```

---

### Task 5: Blazor WASM client scaffold with TailwindUi.Blazor

**Files:**

- Create (template, then overwrite): `development/src/Sis.Web/**`
- Overwrite: `Sis.Web.csproj`, `Program.cs`, `_Imports.razor`, `wwwroot/index.html`, `wwwroot/css/app.css`, `Properties/launchSettings.json`
- Create: `development/src/Sis.Web/wwwroot/appsettings.json`

- [ ] **Step 1: Generate the empty WASM template**

Run (from `development/`):

```bash
dotnet new blazorwasm --empty -n Sis.Web -o src/Sis.Web --no-restore
```

Expected: `The template "Blazor WebAssembly Standalone App" was created successfully.` It creates `App.razor`, `Layout/MainLayout.razor`, `Pages/Home.razor`, `Pages/NotFound.razor`, `wwwroot/icon-192.png` and the files overwritten below. Keep `App.razor`, `Layout/MainLayout.razor`, `Pages/Home.razor`, `Pages/NotFound.razor` and `icon-192.png` untouched for now; later tasks replace them.

- [ ] **Step 2: Overwrite `Sis.Web.csproj`** (central package versions — the template's `Version` attributes break CPM)

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
    <PackageReference Include="TailwindUi.Blazor" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Sis.Contracts\Sis.Contracts.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Overwrite `wwwroot/index.html`**

Host-page wiring follows the TailwindUi.Blazor getting-started guide (Inter, the library bundle, the Elements runtime, and the no-flash theme script).

```html
<!DOCTYPE html>
<html lang="en" class="h-full bg-white dark:bg-dark-950">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Student Information System</title>
    <base href="/" />
    <script>
        // Apply the persisted theme before Blazor boots so the page never flashes the wrong scheme.
        (function () {
            try {
                var stored = localStorage.getItem('tailwindui-blazor-theme');
                if (stored !== 'light' && stored !== 'dark' && stored !== 'system') stored = 'system';
                var dark = stored === 'dark' ||
                    (stored === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
                document.documentElement.classList.toggle('dark', dark);
            } catch (_) { /* ignore */ }
        })();
    </script>
    <link rel="preload" id="webassembly" />
    <link rel="icon" type="image/png" href="icon-192.png" />
    <link rel="stylesheet" href="https://rsms.me/inter/inter.css" />
    <link rel="stylesheet" href="_content/TailwindUi.Blazor/css/tailwindui.css" />
    <link rel="stylesheet" href="css/app.css" />
    <script src="https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1" type="module"></script>
    <script type="importmap"></script>
</head>

<body class="h-full font-sans antialiased text-dark-900 dark:text-dark-100">
    <div id="app">
        <div class="boot-loading">Loading…</div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="." class="reload">Reload</a>
        <span class="dismiss">🗙</span>
    </div>
    <script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
</body>

</html>
```

- [ ] **Step 4: Overwrite `wwwroot/css/app.css`**

```css
/* Only what the TailwindUi bundle cannot cover: Blazor's boot placeholder and error bar,
   which render before (or outside) any component. */
.boot-loading {
    display: flex;
    min-height: 100vh;
    align-items: center;
    justify-content: center;
    font: 500 0.875rem/1 InterVariable, Inter, system-ui, sans-serif;
    color: #64748b;
}

#blazor-error-ui {
    display: none;
    position: fixed;
    inset-inline: 0;
    bottom: 0;
    z-index: 1000;
    padding: 0.75rem 2.5rem 0.75rem 1.25rem;
    background: #fef3c7;
    color: #78350f;
    box-shadow: 0 -1px 2px rgb(0 0 0 / 0.1);
}

#blazor-error-ui .reload {
    font-weight: 600;
    text-decoration: underline;
}

#blazor-error-ui .dismiss {
    position: absolute;
    top: 0.625rem;
    right: 0.75rem;
    cursor: pointer;
}
```

- [ ] **Step 5: Add runtime config `wwwroot/appsettings.json`**

Local default; the Docker image rewrites this file at container start (Task 10).

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5081/"
  }
}
```

- [ ] **Step 6: Overwrite `Program.cs`**

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sis.Web;
using TailwindUi.Blazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Relative request paths ("api/...") only resolve under a base address ending in "/".
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
if (!apiBaseUrl.EndsWith('/'))
{
    apiBaseUrl += "/";
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddTailwindUi();

await builder.Build().RunAsync();
```

- [ ] **Step 7: Overwrite `_Imports.razor`**

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using Sis.Contracts.SystemInfo
@using Sis.Web
@using Sis.Web.Layout
@using TailwindUi.Blazor
@using TailwindUi.Blazor.Components
@using TailwindUi.Blazor.Theming
```

- [ ] **Step 8: Overwrite `Properties/launchSettings.json`**

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "http://localhost:5080",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 9: Add to the solution and build**

```bash
dotnet sln Sis.slnx add src/Sis.Web/Sis.Web.csproj --solution-folder src
dotnet build Sis.slnx
```

Expected: `Build succeeded.` 0 warnings. If restore fails with `NU1301`/401 on `nimbleloop`, the `GH_PACKAGES_*` env vars are missing or the PAT lacks `read:packages`.

- [ ] **Step 10: Commit**

```bash
git add development
git commit -m "feat(web): scaffold Blazor WASM client wired to TailwindUi.Blazor"
```

---

### Task 6: `AppMenu` — the menu model

**Files:**

- Create: `development/tests/Sis.Web.Tests/Sis.Web.Tests.csproj`
- Create: `development/src/Sis.Web/Layout/AppMenu.cs`
- Test: `development/tests/Sis.Web.Tests/Layout/AppMenuTests.cs`

- [ ] **Step 1: Create the web test project**

`development/tests/Sis.Web.Tests/Sis.Web.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AwesomeAssertions" />
    <PackageReference Include="bunit" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Sis.Web\Sis.Web.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="Bunit" />
    <Using Include="AwesomeAssertions" />
  </ItemGroup>

</Project>
```

Run: `dotnet sln Sis.slnx add tests/Sis.Web.Tests/Sis.Web.Tests.csproj --solution-folder tests`

- [ ] **Step 2: Write the failing tests**

`development/tests/Sis.Web.Tests/Layout/AppMenuTests.cs`:

```csharp
using Sis.Web.Layout;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Theming;

namespace Sis.Web.Tests.Layout;

public sealed class AppMenuTests
{
    [Fact]
    public void Menu_links_are_unique() =>
        AppMenu.Items.Select(item => item.Href).Should().OnlyHaveUniqueItems();

    [Fact]
    public void Every_menu_icon_exists_in_the_icon_set() =>
        AppMenu.Items.Should().OnlyContain(item => HeroIconData.Contains(IconVariant.Outline, item.IconName));

    [Theory]
    [InlineData("", "Dashboard")]
    [InlineData("students", "Students")]
    [InlineData("students/42", "Students")]
    [InlineData("Attendance?date=2026-10-19", "Attendance")]
    public void FindByPath_returns_the_menu_item_that_owns_the_path(string relativePath, string expectedLabel) =>
        AppMenu.FindByPath(relativePath)!.Label.Should().Be(expectedLabel);

    [Theory]
    [InlineData("not-found")]
    [InlineData("studentsx")]
    public void FindByPath_returns_null_outside_the_menu(string relativePath) =>
        AppMenu.FindByPath(relativePath).Should().BeNull();
}
```

> If `HeroIconData` or `IconVariant` resolve to a different namespace, check with
> `dotnet build` and fix the `using` — they ship in the `TailwindUi.Blazor` package.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: build FAILS — `The name 'AppMenu' does not exist in the current context`.

- [ ] **Step 4: Write `AppMenu`**

`development/src/Sis.Web/Layout/AppMenu.cs`:

```csharp
namespace Sis.Web.Layout;

public sealed record AppMenuItem(string Label, string Href, string IconName);

public sealed record AppMenuSection(string Title, IReadOnlyList<AppMenuItem> Items);

/// <summary>
/// The app's navigation, in one place. The sidebar, the mobile title and the header title all
/// read from here, so adding a module is a one-line change.
/// </summary>
public static class AppMenu
{
    public static IReadOnlyList<AppMenuSection> Sections { get; } =
    [
        new("Overview",
        [
            new("Dashboard", "/", "home"),
        ]),
        new("Students",
        [
            new("Students", "/students", "academic-cap"),
            new("Admissions", "/admissions", "clipboard-document-check"),
        ]),
        new("Academics",
        [
            new("Classes", "/classes", "user-group"),
            new("Courses", "/courses", "book-open"),
            new("Attendance", "/attendance", "calendar-days"),
            new("Grades", "/grades", "chart-bar"),
        ]),
        new("Administration",
        [
            new("Staff", "/staff", "briefcase"),
            new("Fees", "/fees", "banknotes"),
            new("Reports", "/reports", "document-chart-bar"),
            new("Settings", "/settings", "cog-6-tooth"),
        ]),
    ];

    public static IEnumerable<AppMenuItem> Items => Sections.SelectMany(section => section.Items);

    /// <summary>
    /// The item whose route owns <paramref name="relativePath"/> — the path as returned by
    /// <c>NavigationManager.ToBaseRelativePath</c>, e.g. <c>"students/42?tab=fees"</c> → Students.
    /// </summary>
    public static AppMenuItem? FindByPath(string relativePath)
    {
        var path = "/" + relativePath.Split('?', '#')[0].Trim('/');

        return Items.FirstOrDefault(item =>
            string.Equals(path, item.Href, StringComparison.OrdinalIgnoreCase) ||
            (item.Href != "/" && path.StartsWith(item.Href + "/", StringComparison.OrdinalIgnoreCase)));
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: `Passed!  - Failed: 0, Passed: 8`

- [ ] **Step 6: Commit**

```bash
git add development
git commit -m "feat(web): add AppMenu navigation model"
```

---

### Task 7: `SystemInfoClient`

**Files:**

- Create: `development/tests/Sis.Web.Tests/Support/StubHttp.cs`
- Create: `development/src/Sis.Web/Services/SystemInfoClient.cs`
- Modify: `development/src/Sis.Web/Program.cs`
- Modify: `development/src/Sis.Web/_Imports.razor`
- Test: `development/tests/Sis.Web.Tests/Services/SystemInfoClientTests.cs`

- [ ] **Step 1: Write the HTTP stub used by web tests**

`development/tests/Sis.Web.Tests/Support/StubHttp.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;

namespace Sis.Web.Tests.Support;

/// <summary>An HttpClient whose responses come from a delegate — no network.</summary>
internal static class StubHttp
{
    public static readonly Uri BaseAddress = new("http://api.test/");

    public static HttpClient Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new Handler(respond)) { BaseAddress = BaseAddress };

    public static HttpResponseMessage Json<T>(T body) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
```

- [ ] **Step 2: Write the failing test**

`development/tests/Sis.Web.Tests/Services/SystemInfoClientTests.cs`:

```csharp
using Sis.Contracts.SystemInfo;
using Sis.Web.Services;
using Sis.Web.Tests.Support;

namespace Sis.Web.Tests.Services;

public sealed class SystemInfoClientTests
{
    [Fact]
    public async Task GetAsync_reads_system_info_from_the_api()
    {
        var expected = new SystemInfoResponse("Student Information System", "1.0.0", "Testing", DateTimeOffset.UnixEpoch);
        HttpRequestMessage? sent = null;
        var http = StubHttp.Create(request =>
        {
            sent = request;
            return StubHttp.Json(expected);
        });

        var actual = await new SystemInfoClient(http).GetAsync();

        actual.Should().Be(expected);
        sent!.RequestUri.Should().Be(new Uri(StubHttp.BaseAddress, "api/system/info"));
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: build FAILS — `The type or namespace name 'Services' does not exist in the namespace 'Sis.Web'`.

- [ ] **Step 4: Write the client**

`development/src/Sis.Web/Services/SystemInfoClient.cs`:

```csharp
using System.Net.Http.Json;
using Sis.Contracts.SystemInfo;

namespace Sis.Web.Services;

public sealed class SystemInfoClient(HttpClient http)
{
    public async Task<SystemInfoResponse> GetAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<SystemInfoResponse>("api/system/info", cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty system info response.");
}
```

- [ ] **Step 5: Register it and import its namespace**

In `development/src/Sis.Web/Program.cs`, add `using Sis.Web.Services;` under the other usings, and register the client after the `HttpClient` line:

```csharp
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<SystemInfoClient>();
builder.Services.AddTailwindUi();
```

In `development/src/Sis.Web/_Imports.razor`, add after `@using Sis.Web.Layout`:

```razor
@using Sis.Web.Services
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: `Passed!  - Failed: 0, Passed: 9`

- [ ] **Step 7: Commit**

```bash
git add development
git commit -m "feat(web): add SystemInfoClient"
```

---

### Task 8: Pages — dashboard, module placeholders, not-found

**Files:**

- Delete: `development/src/Sis.Web/Pages/Home.razor`
- Create: `development/src/Sis.Web/Pages/Dashboard.razor`
- Create: `development/src/Sis.Web/Pages/ModulePlaceholder.razor`
- Overwrite: `development/src/Sis.Web/Pages/NotFound.razor`
- Overwrite: `development/src/Sis.Web/App.razor`
- Test: `development/tests/Sis.Web.Tests/Pages/RoutingTests.cs`
- Test: `development/tests/Sis.Web.Tests/Pages/ModulePlaceholderTests.cs`
- Test: `development/tests/Sis.Web.Tests/Pages/DashboardTests.cs`

- [ ] **Step 1: Write the failing tests**

`development/tests/Sis.Web.Tests/Pages/RoutingTests.cs`:

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Sis.Web.Layout;

namespace Sis.Web.Tests.Pages;

public sealed class RoutingTests
{
    private static readonly HashSet<string> RouteTemplates = typeof(App).Assembly.GetTypes()
        .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
        .Select(route => route.Template)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static TheoryData<string> MenuLinks => new(AppMenu.Items.Select(item => item.Href));

    [Theory]
    [MemberData(nameof(MenuLinks))]
    public void Every_menu_link_has_a_page(string href) =>
        RouteTemplates.Should().Contain(href);
}
```

`development/tests/Sis.Web.Tests/Pages/ModulePlaceholderTests.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sis.Web.Pages;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class ModulePlaceholderTests : BunitContext
{
    public ModulePlaceholderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Titles_itself_from_the_menu_item_for_the_current_path()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/fees");

        var cut = Render<ModulePlaceholder>();

        cut.Find("h1").TextContent.Trim().Should().Be("Fees");
        cut.Markup.Should().Contain("Fees is coming soon");
    }
}
```

`development/tests/Sis.Web.Tests/Pages/DashboardTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.SystemInfo;
using Sis.Web.Pages;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class DashboardTests : BunitContext
{
    public DashboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Shows_connected_status_with_version_and_environment()
    {
        var info = new SystemInfoResponse("Student Information System", "1.2.3", "Testing", DateTimeOffset.UnixEpoch);
        UseApi(_ => StubHttp.Json(info));

        var cut = Render<Dashboard>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=api-status]").TextContent
                .Should().Contain("Connected").And.Contain("v1.2.3").And.Contain("Testing"));
    }

    [Fact]
    public void Shows_unreachable_status_when_the_api_call_fails()
    {
        UseApi(_ => throw new HttpRequestException("connection refused"));

        var cut = Render<Dashboard>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=api-status]").TextContent.Should().Contain("API unreachable"));
    }

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        Services.AddSingleton(new SystemInfoClient(StubHttp.Create(respond)));
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: build FAILS — `The type or namespace name 'Dashboard' could not be found` / `'ModulePlaceholder' could not be found`.

- [ ] **Step 3: Delete the template home page**

```bash
git rm -q src/Sis.Web/Pages/Home.razor
```

(If `Home.razor` was never committed, use `rm src/Sis.Web/Pages/Home.razor`.)

- [ ] **Step 4: Write the dashboard**

`development/src/Sis.Web/Pages/Dashboard.razor`:

```razor
@page "/"
@using System.Text.Json
@inject SystemInfoClient SystemInfo

<PageTitle>Dashboard · SIS</PageTitle>

<PageHeading Title="Dashboard" Description="Your campus at a glance. Widgets arrive as modules are built." />

<div class="mt-8 max-w-md">
    <Card>
        <HeaderContent>
            <h2 class="text-dark-900 text-base font-semibold dark:text-white">System status</h2>
        </HeaderContent>
        <ChildContent>
            <div data-testid="api-status" class="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
                @if (_info is not null)
                {
                    <Badge Dot Color="ThemeColor.Success" Text="Connected" />
                    <span class="text-dark-500">v@(_info.Version) · @_info.Environment</span>
                }
                else if (_unreachable)
                {
                    <Badge Dot Color="ThemeColor.Danger" Text="API unreachable" />
                    <span class="text-dark-500">Check that the API is running and allows this origin.</span>
                }
                else
                {
                    <span class="text-dark-500">Checking API…</span>
                }
            </div>
        </ChildContent>
    </Card>
</div>

@code {
    private SystemInfoResponse? _info;
    private bool _unreachable;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _info = await SystemInfo.GetAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or NotSupportedException or InvalidOperationException)
        {
            _unreachable = true;
        }
    }
}
```

- [ ] **Step 5: Write the module placeholder**

`development/src/Sis.Web/Pages/ModulePlaceholder.razor` — one `@page` per unbuilt module. **When a module is built, delete its route here.**

```razor
@page "/students"
@page "/admissions"
@page "/classes"
@page "/courses"
@page "/attendance"
@page "/grades"
@page "/staff"
@page "/fees"
@page "/reports"
@page "/settings"
@inject NavigationManager Navigation

<PageTitle>@Title · SIS</PageTitle>

<PageHeading Title="@Title" />

<div class="mt-10">
    <EmptyState Title="@($"{Title} is coming soon")">
        This module hasn't been built yet. It will appear here in a later step.
    </EmptyState>
</div>

@code {
    private string Title => AppMenu.FindByPath(Navigation.ToBaseRelativePath(Navigation.Uri))?.Label ?? "Module";
}
```

- [ ] **Step 6: Overwrite the not-found page**

`development/src/Sis.Web/Pages/NotFound.razor`:

```razor
@page "/not-found"

<PageTitle>Not found · SIS</PageTitle>

<PageHeading Title="Page not found" />

<div class="mt-10">
    <EmptyState Title="We couldn't find that page">
        Check the address, or pick a module from the menu.
    </EmptyState>
</div>
```

- [ ] **Step 7: Overwrite `App.razor`**

```razor
<Router AppAssembly="@typeof(App).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
</Router>
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: `Passed!  - Failed: 0, Passed: 23` (9 earlier + 11 routing + 1 placeholder + 2 dashboard).

- [ ] **Step 9: Commit**

```bash
git add -A development
git commit -m "feat(web): add dashboard status card, module placeholders and not-found page"
```

---

### Task 9: App shell — `MainLayout` + `UserMenu`

**Files:**

- Overwrite: `development/src/Sis.Web/Layout/MainLayout.razor`
- Create: `development/src/Sis.Web/Layout/UserMenu.razor`
- Test: `development/tests/Sis.Web.Tests/Layout/MainLayoutTests.cs`

- [ ] **Step 1: Write the failing tests**

`development/tests/Sis.Web.Tests/Layout/MainLayoutTests.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sis.Web.Layout;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Sidebar_lists_every_menu_section_and_item()
    {
        var cut = Render<MainLayout>();

        foreach (var section in AppMenu.Sections)
        {
            cut.Markup.Should().Contain(section.Title);
            foreach (var item in section.Items)
            {
                cut.Markup.Should().Contain(item.Label);
            }
        }
    }

    [Fact]
    public void Header_names_the_current_module()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/attendance");

        var cut = Render<MainLayout>();

        cut.Find("[data-testid=page-title]").TextContent.Trim().Should().Be("Attendance");
    }

    [Fact]
    public void Header_falls_back_for_paths_outside_the_menu()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/not-found");

        var cut = Render<MainLayout>();

        cut.Find("[data-testid=page-title]").TextContent.Trim().Should().Be("Page not found");
    }

    [Fact]
    public void Header_has_a_user_menu()
    {
        var cut = Render<MainLayout>();

        cut.Markup.Should().Contain("Demo Admin").And.Contain("Sign out");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: FAIL — the template `MainLayout` has no menu, no `[data-testid=page-title]` element, and no user menu (e.g. `Expected cut.Markup ... to contain "Overview"` and `ElementNotFoundException`).

- [ ] **Step 3: Write the user menu**

`development/src/Sis.Web/Layout/UserMenu.razor`:

```razor
@inject IToastService Toasts

<Dropdown Anchor="DropdownAnchor.BottomEnd">
    <Trigger>
        <span class="inline-flex items-center gap-x-2">
            <Avatar Initials="Demo Admin" Size="Size.Small" InitialsBackground="ThemeColor.Primary" />
            <span class="text-dark-900 hidden text-sm font-semibold sm:inline dark:text-white">Demo Admin</span>
        </span>
    </Trigger>
    <Header>
        <p class="text-dark-500 text-sm">Signed in as</p>
        <p class="text-dark-900 truncate text-sm font-medium dark:text-white">admin@demo.school</p>
    </Header>
    <ChildContent>
        <DropdownSection>
            <DropdownItem Text="Your profile" OnClick="ShowNotYetAvailable" />
            <DropdownItem Text="Settings" Href="settings" />
        </DropdownSection>
        <DropdownSection>
            <DropdownItem Text="Sign out" OnClick="ShowNotYetAvailable" />
        </DropdownSection>
    </ChildContent>
</Dropdown>

@code {
    // Placeholder until authentication is built.
    private void ShowNotYetAvailable() =>
        Toasts.ShowInfo("Not available yet", "Accounts arrive with authentication in a later step.");
}
```

- [ ] **Step 4: Write the shell layout**

Overwrite `development/src/Sis.Web/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase
@inject NavigationManager Navigation

<SidebarLayout SidebarWidth="w-72" MobileTitle="@PageTitle" OffWhitePageBackground="true">
    <Sidebar>
        <div class="flex h-16 shrink-0 items-center gap-x-3">
            <div class="bg-primary-600 grid size-8 place-items-center rounded-md text-white">
                <Icon Name="academic-cap" Size="size-5" />
            </div>
            <div class="flex flex-col">
                <span class="text-dark-900 text-sm font-semibold dark:text-white">Student Information System</span>
                <span class="text-dark-500 text-xs">Demo campus</span>
            </div>
        </div>

        <NavList AriaLabel="Main">
            @foreach (var section in AppMenu.Sections)
            {
                <NavSection Title="@section.Title">
                    @foreach (var item in section.Items)
                    {
                        <NavItem Href="@item.Href" Label="@item.Label">
                            <Icon><Icon Name="@item.IconName" /></Icon>
                        </NavItem>
                    }
                </NavSection>
            }
        </NavList>
    </Sidebar>

    <Header>
        <span data-testid="page-title" class="text-dark-900 truncate text-sm font-semibold dark:text-white">@PageTitle</span>
        <div class="flex shrink-0 items-center gap-x-4">
            <ThemeSwitcher />
            <UserMenu />
        </div>
    </Header>

    <ChildContent>
        <ErrorBoundary @ref="_errorBoundary">
            <ChildContent>
                @Body
            </ChildContent>
            <ErrorContent>
                <Alert Color="ThemeColor.Danger" Title="Something went wrong">
                    This page failed to render. Pick another page from the menu, or reload.
                </Alert>
            </ErrorContent>
        </ErrorBoundary>
    </ChildContent>
</SidebarLayout>

<ToastHost Position="ToastPosition.TopRight" />

@code {
    private ErrorBoundary? _errorBoundary;

    private string PageTitle =>
        AppMenu.FindByPath(Navigation.ToBaseRelativePath(Navigation.Uri))?.Label ?? "Page not found";

    // A new page arrives as a new Body: clear any error the previous page left behind.
    protected override void OnParametersSet() => _errorBoundary?.Recover();
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/Sis.Web.Tests`
Expected: `Passed!  - Failed: 0, Passed: 27`

- [ ] **Step 6: Run the whole suite and build with warnings-as-errors**

Run: `dotnet build Sis.slnx -c Release && dotnet test Sis.slnx -c Release --no-build`
Expected: `Build succeeded.` 0 warnings; Api.Tests `Passed: 3`, Web.Tests `Passed: 27`.

- [ ] **Step 7: Eyeball the shell in a browser**

Run in two terminals: `docker run --rm -d --name sis-db-dev -e POSTGRES_DB=sis -e POSTGRES_USER=sis -e POSTGRES_PASSWORD=sis -p 5432:5432 postgres:18`, then `dotnet run --project src/Sis.Api` and `dotnet run --project src/Sis.Web`. Open <http://localhost:5080>.

Check: sidebar shows 4 sections / 11 items with icons; clicking "Fees" lights the item, the header reads "Fees" and the page shows "Fees is coming soon"; the dashboard card reads "Connected · v1.0.0 · Development"; the theme switcher toggles dark mode; the avatar menu opens and "Sign out" shows a toast; below `lg` width the hamburger opens the drawer. Stop everything and `docker rm -f sis-db-dev`.

- [ ] **Step 8: Commit**

```bash
git add development
git commit -m "feat(web): add sidebar app shell with header, user menu and error boundary"
```

---

### Task 10: Docker — API image, web image, Compose

**Files:**

- Create: `development/src/Sis.Api/Dockerfile`
- Create: `development/src/Sis.Web/Dockerfile`
- Create: `development/src/Sis.Web/nginx.conf`
- Create: `development/src/Sis.Web/docker-entrypoint.d/40-sis-config.sh`
- Create: `development/docker-compose.yml`

- [ ] **Step 1: Write the API Dockerfile**

`development/src/Sis.Api/Dockerfile` (build context is `development/`):

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Sis.Contracts/Sis.Contracts.csproj src/Sis.Contracts/
COPY src/Sis.Api/Sis.Api.csproj src/Sis.Api/
RUN dotnet restore src/Sis.Api/Sis.Api.csproj

COPY src/Sis.Contracts/ src/Sis.Contracts/
COPY src/Sis.Api/ src/Sis.Api/
RUN dotnet publish src/Sis.Api/Sis.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sis.Api.dll"]
```

- [ ] **Step 2: Write the nginx site config**

`development/src/Sis.Web/nginx.conf`:

```nginx
server {
    listen 8080;
    server_name _;
    root /usr/share/nginx/html;

    gzip on;
    gzip_min_length 1024;
    gzip_types text/css application/javascript application/json application/wasm image/svg+xml;

    # Client-side routes (/students, /fees, ...) all boot from index.html.
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Always revalidate the entry point and the runtime config; everything under
    # _framework is fingerprinted by the .NET build.
    location = /index.html {
        add_header Cache-Control "no-cache";
    }

    location = /appsettings.json {
        add_header Cache-Control "no-cache";
    }
}
```

- [ ] **Step 3: Write the runtime-config entrypoint script**

`development/src/Sis.Web/docker-entrypoint.d/40-sis-config.sh` (must be saved with LF line endings — `.gitattributes` from Task 1 enforces this on checkout):

```sh
#!/bin/sh
# Writes the Blazor client's runtime config from the container environment, so one image
# can point at any API without a rebuild. nginx's entrypoint runs every *.sh in
# /docker-entrypoint.d/ before starting the server.
set -eu

: "${API_BASE_URL:?API_BASE_URL must be set, e.g. http://localhost:8081/}"

cat > /usr/share/nginx/html/appsettings.json <<EOF
{
  "Api": {
    "BaseUrl": "${API_BASE_URL}"
  }
}
EOF
```

- [ ] **Step 4: Write the web Dockerfile**

`development/src/Sis.Web/Dockerfile`. The NuGet credentials arrive as BuildKit secrets exposed as env vars for the restore step only — they never land in an image layer.

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Sis.Contracts/Sis.Contracts.csproj src/Sis.Contracts/
COPY src/Sis.Web/Sis.Web.csproj src/Sis.Web/
RUN --mount=type=secret,id=gh_packages_user,env=GH_PACKAGES_USER \
    --mount=type=secret,id=gh_packages_token,env=GH_PACKAGES_TOKEN \
    dotnet restore src/Sis.Web/Sis.Web.csproj

COPY src/Sis.Contracts/ src/Sis.Contracts/
COPY src/Sis.Web/ src/Sis.Web/
RUN dotnet publish src/Sis.Web/Sis.Web.csproj -c Release --no-restore -o /app/publish

FROM nginxinc/nginx-unprivileged:alpine AS final
COPY src/Sis.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --chmod=0755 src/Sis.Web/docker-entrypoint.d/40-sis-config.sh /docker-entrypoint.d/40-sis-config.sh
# Owned by nginx's uid so the entrypoint script can rewrite appsettings.json.
COPY --from=build --chown=101:101 /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 8080
```

- [ ] **Step 5: Write `docker-compose.yml`**

`development/docker-compose.yml` (demo credentials only):

```yaml
name: sis

services:
  db:
    image: postgres:18
    environment:
      POSTGRES_DB: sis
      POSTGRES_USER: sis
      POSTGRES_PASSWORD: sis
    ports:
      - "5432:5432"
    volumes:
      # postgres 18+ keeps its data under a versioned subdirectory of /var/lib/postgresql.
      - sis-db:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U sis -d sis"]
      interval: 5s
      timeout: 5s
      retries: 10

  api:
    build:
      context: .
      dockerfile: src/Sis.Api/Dockerfile
    environment:
      ConnectionStrings__Sis: "Host=db;Port=5432;Database=sis;Username=sis;Password=sis"
      Database__ApplyMigrationsOnStartup: "true"
      Cors__AllowedOrigins__0: "http://localhost:8080"
    ports:
      - "8081:8080"
    depends_on:
      db:
        condition: service_healthy

  web:
    build:
      context: .
      dockerfile: src/Sis.Web/Dockerfile
      secrets:
        - gh_packages_user
        - gh_packages_token
    environment:
      # The browser calls the API directly, so this is the host-facing URL, not http://api:8080.
      API_BASE_URL: "http://localhost:8081/"
    ports:
      - "8080:8080"
    depends_on:
      - api

secrets:
  gh_packages_user:
    environment: GH_PACKAGES_USER
  gh_packages_token:
    environment: GH_PACKAGES_TOKEN

volumes:
  sis-db:
```

- [ ] **Step 6: Build and start the stack**

Run (from `development/`): `docker compose up --build -d`
Expected: three containers `sis-db-1`, `sis-api-1`, `sis-web-1` running; `docker compose ps` shows `db` as `healthy`.

- [ ] **Step 7: Verify each tier**

```bash
curl -s http://localhost:8081/health
curl -s http://localhost:8081/api/system/info
curl -s http://localhost:8080/appsettings.json
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/fees
curl -s -I -H "Origin: http://localhost:8080" http://localhost:8081/api/system/info | grep -i access-control-allow-origin
```

Expected, in order: `Healthy`; JSON with `"environment":"Production"`; `{"Api":{"BaseUrl":"http://localhost:8081/"}}` (pretty-printed); `200`; `Access-Control-Allow-Origin: http://localhost:8080`.

Then open <http://localhost:8080>: the dashboard card reads "Connected · v1.0.0 · Production". Check `docker compose logs api` shows no migration errors.

- [ ] **Step 8: Tear down and commit**

```bash
docker compose down
git add development
git commit -m "feat(sis): add Dockerfiles and Compose stack (postgres, api, nginx web)"
```

---

### Task 11: GitHub Actions — build + unit tests

**Files:**

- Create: `.github/workflows/development-ci.yml` (repo root)

- [ ] **Step 1: Write the workflow**

```yaml
name: Development CI

on:
  push:
    branches: [main]
    paths:
      - "development/**"
      - ".github/workflows/development-ci.yml"
  pull_request:
    paths:
      - "development/**"
      - ".github/workflows/development-ci.yml"
  workflow_dispatch:

permissions:
  contents: read

concurrency:
  group: development-ci-${{ github.ref }}
  cancel-in-progress: true

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  # TailwindUi.Blazor lives on the NimbleLoop-Technologies GitHub Packages feed. This repo is
  # outside that org, so GITHUB_TOKEN cannot read it: GH_PACKAGES_TOKEN is a PAT with read:packages.
  GH_PACKAGES_USER: ${{ github.repository_owner }}
  GH_PACKAGES_TOKEN: ${{ secrets.GH_PACKAGES_TOKEN }}

defaults:
  run:
    working-directory: development

jobs:
  build-and-test:
    name: Build & unit tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Check package feed secret
        if: env.GH_PACKAGES_TOKEN == ''
        run: |
          echo "::error::Repository secret GH_PACKAGES_TOKEN is not set. Add a classic PAT with read:packages for NimbleLoop-Technologies."
          exit 1

      - name: Restore
        run: dotnet restore Sis.slnx

      - name: Build
        run: dotnet build Sis.slnx -c Release --no-restore

      - name: Test
        run: dotnet test Sis.slnx -c Release --no-build --logger trx --results-directory TestResults

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: development/TestResults/*.trx
          if-no-files-found: warn
```

- [ ] **Step 2: Validate the YAML locally**

Run (from repo root): `python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/development-ci.yml')); print('ok')"`
Expected: `ok`. (If Python/PyYAML is unavailable, skip — GitHub validates on push.)

- [ ] **Step 3: Re-run the exact CI commands locally**

Run (from `development/`):

```bash
dotnet restore Sis.slnx
dotnet build Sis.slnx -c Release --no-restore
dotnet test Sis.slnx -c Release --no-build --logger trx --results-directory TestResults
```

Expected: build succeeds with 0 warnings; 30 tests pass; `.trx` files in `development/TestResults/`. Delete `development/TestResults/` afterwards (it is build output).

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/development-ci.yml
git commit -m "ci: build and unit-test the development solution"
```

---

### Task 12: README

**Files:**

- Create: `development/README.md`

- [ ] **Step 1: Write the README**

````markdown
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
| `localhost:5432` | Postgres (`sis` / `sis`, demo only) |

The web image reads `API_BASE_URL` at container start and writes it into `appsettings.json`,
so the same image can point at any API.

## Run locally

```bash
docker compose up -d db
dotnet run --project src/Sis.Api   # http://localhost:5081
dotnet run --project src/Sis.Web   # http://localhost:5080
```

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

## CI

`.github/workflows/development-ci.yml` restores, builds (warnings as errors) and runs the unit
tests on pushes to `main` and on pull requests that touch `development/`. It needs the
repository secret **`GH_PACKAGES_TOKEN`**: a PAT with `read:packages` for NimbleLoop-Technologies.
````

- [ ] **Step 2: Commit**

```bash
git add development/README.md
git commit -m "docs(sis): add development README"
```

---

## Final verification (after all tasks)

- [ ] `dotnet build Sis.slnx -c Release`: 0 warnings, 0 errors.
- [ ] `dotnet test Sis.slnx -c Release`: Api.Tests 3 passed, Web.Tests 27 passed.
- [ ] `docker compose up --build -d`, then the Task 10 Step 7 checks pass; `docker compose down`.
- [ ] `git status` is clean, and nothing under `TailwindUi.Blazor`/`tailwindui.css` is tracked: `git ls-files | grep -i tailwindui.css` prints nothing.
