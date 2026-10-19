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
