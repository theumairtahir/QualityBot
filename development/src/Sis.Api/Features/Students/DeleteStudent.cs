using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;

namespace Sis.Api.Features.Students;

/// <summary>Removes a student's record. 404 if it is already gone.</summary>
public sealed class DeleteStudent : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/students/{id:guid}", HandleAsync)
            .WithName(nameof(DeleteStudent))
            .WithTags("Students");

    public static async Task<IResult> HandleAsync(Guid id, SisDbContext db, CancellationToken cancellationToken)
    {
        var deleted = await db.Set<Student>().Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? Results.NotFound() : Results.NoContent();
    }
}
