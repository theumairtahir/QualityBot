using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

/// <summary>Lists students, optionally filtered by a name search (case-insensitive, first + last name).</summary>
public sealed class ListStudents : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/students", HandleAsync)
            .WithName(nameof(ListStudents))
            .WithTags("Students");

    public static async Task<IReadOnlyList<StudentResponse>> HandleAsync(
        SisDbContext db, string? search, CancellationToken cancellationToken)
    {
        var query = db.Set<Student>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(student =>
                EF.Functions.ILike(student.FirstName + " " + student.LastName, pattern, "\\"));
        }

        var students = await query
            .OrderBy(student => student.LastName).ThenBy(student => student.FirstName)
            .ToListAsync(cancellationToken);

        return students.Select(student => student.ToResponse()).ToList();
    }

    // Typing "50%" or "a_b" must match those characters, not act as wildcards.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", @"\%").Replace("_", @"\_");
}
