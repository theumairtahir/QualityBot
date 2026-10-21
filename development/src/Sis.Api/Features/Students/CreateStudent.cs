using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

/// <summary>Adds a student with their basic details.</summary>
public sealed class CreateStudent : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/students", HandleAsync)
            .WithName(nameof(CreateStudent))
            .WithTags("Students");

    public static async Task<IResult> HandleAsync(
        CreateStudentRequest request,
        SisDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var errors = request.Validate(DateOnly.FromDateTime(now.UtcDateTime));
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        var student = new Student { Id = Guid.NewGuid(), CreatedAtUtc = now };
        student.Apply(request);

        db.Add(student);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/students/{student.Id}", student.ToResponse());
    }
}
