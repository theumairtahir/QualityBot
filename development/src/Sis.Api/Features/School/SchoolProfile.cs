using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sis.Contracts.School;

namespace Sis.Api.Features.School;

/// <summary>The one school this installation belongs to. There is only ever one row.</summary>
public sealed class SchoolProfile
{
    /// <summary>The fixed key of the single row.</summary>
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string Name { get; set; } = "";
    public string? AcademicYear { get; set; }

    /// <summary>Copies a validated request's details onto the profile, trimming surrounding spaces.</summary>
    public void Apply(UpdateSchoolProfileRequest request)
    {
        Name = request.Name!.Trim();
        AcademicYear = string.IsNullOrWhiteSpace(request.AcademicYear) ? null : request.AcademicYear.Trim();
    }

    public SchoolProfileResponse ToResponse() => new(Name, AcademicYear);
}

public sealed class SchoolProfileConfiguration : IEntityTypeConfiguration<SchoolProfile>
{
    public void Configure(EntityTypeBuilder<SchoolProfile> builder)
    {
        builder.ToTable("school_profile");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.Name).HasMaxLength(UpdateSchoolProfileRequest.MaxNameLength);
        builder.Property(profile => profile.AcademicYear).HasMaxLength(9);
    }
}
