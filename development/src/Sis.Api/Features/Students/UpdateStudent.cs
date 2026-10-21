using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

/// <summary>Replaces a student's details, with the same validation as adding one.</summary>
public sealed class UpdateStudent : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/students/{id:guid}", HandleAsync)
            .WithName(nameof(UpdateStudent))
            .WithTags("Students");

    public static async Task<IResult> HandleAsync(
        Guid id,
        CreateStudentRequest request,
        SisDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        var student = await db.Set<Student>().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (student is null)
        {
            return Results.NotFound();
        }

        student.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(student.ToResponse());
    }
}
