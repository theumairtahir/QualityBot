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
