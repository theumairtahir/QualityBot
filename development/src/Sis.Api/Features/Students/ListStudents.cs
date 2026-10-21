using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

/// <summary>Lists every student. Search and paging arrive with the "view and search" story.</summary>
public sealed class ListStudents : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/students", HandleAsync)
            .WithName(nameof(ListStudents))
            .WithTags("Students");

    public static async Task<IReadOnlyList<StudentResponse>> HandleAsync(
        SisDbContext db, CancellationToken cancellationToken) =>
        await db.Set<Student>()
            .AsNoTracking()
            .OrderBy(student => student.LastName).ThenBy(student => student.FirstName)
            .Select(student => new StudentResponse(
                student.Id, student.FirstName, student.LastName, student.DateOfBirth, student.Email, student.ClassName))
            .ToListAsync(cancellationToken);
}
