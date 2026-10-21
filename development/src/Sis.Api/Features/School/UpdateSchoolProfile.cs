using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.School;

namespace Sis.Api.Features.School;

/// <summary>Saves the school's name and academic year, creating the profile the first time.</summary>
public sealed class UpdateSchoolProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/school", HandleAsync)
            .WithName(nameof(UpdateSchoolProfile))
            .WithTags("School");

    public static async Task<IResult> HandleAsync(
        UpdateSchoolProfileRequest request, SisDbContext db, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        var profile = await db.Set<SchoolProfile>()
            .SingleOrDefaultAsync(p => p.Id == SchoolProfile.SingletonId, cancellationToken);

        if (profile is null)
        {
            profile = new SchoolProfile();
            db.Add(profile);
        }

        profile.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(profile.ToResponse());
    }
}
