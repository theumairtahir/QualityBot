using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;

namespace Sis.Api.Features.Students;

/// <summary>Returns one student, or 404.</summary>
public sealed class GetStudent : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/students/{id:guid}", HandleAsync)
            .WithName(nameof(GetStudent))
            .WithTags("Students");

    public static async Task<IResult> HandleAsync(Guid id, SisDbContext db, CancellationToken cancellationToken)
    {
        var student = await db.Set<Student>().AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        return student is null ? Results.NotFound() : Results.Ok(student.ToResponse());
    }
}
