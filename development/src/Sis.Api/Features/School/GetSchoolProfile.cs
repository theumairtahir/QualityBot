using Microsoft.EntityFrameworkCore;
using Sis.Api.Common;
using Sis.Api.Persistence;
using Sis.Contracts.School;

namespace Sis.Api.Features.School;

/// <summary>Returns the school profile, or the default until one has been saved.</summary>
public sealed class GetSchoolProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/school", HandleAsync)
            .WithName(nameof(GetSchoolProfile))
            .WithTags("School");

    public static async Task<SchoolProfileResponse> HandleAsync(SisDbContext db, CancellationToken cancellationToken)
    {
        var profile = await db.Set<SchoolProfile>().AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == SchoolProfile.SingletonId, cancellationToken);

        return profile?.ToResponse() ?? SchoolProfileResponse.Default;
    }
}
