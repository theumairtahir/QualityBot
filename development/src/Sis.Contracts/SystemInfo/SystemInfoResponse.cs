namespace Sis.Contracts.SystemInfo;

/// <summary>What <c>GET /api/system/info</c> returns: which API build is answering, and where.</summary>
public sealed record SystemInfoResponse(
    string ApplicationName,
    string Version,
    string Environment,
    DateTimeOffset ServerTimeUtc);
