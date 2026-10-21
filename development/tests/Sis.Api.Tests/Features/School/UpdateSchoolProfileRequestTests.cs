using Sis.Api.Features.School;
using Sis.Contracts.School;

namespace Sis.Api.Tests.Features.School;

public sealed class UpdateSchoolProfileRequestTests
{
    [Fact]
    public void A_name_and_academic_year_are_valid() =>
        new UpdateSchoolProfileRequest("Greenfield Academy", "2026-2027").Validate().Should().BeEmpty();

    [Fact]
    public void The_academic_year_is_optional() =>
        new UpdateSchoolProfileRequest("Greenfield Academy", null).Validate().Should().BeEmpty();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void The_school_name_is_required(string? name) =>
        new UpdateSchoolProfileRequest(name, "2026-2027").Validate().Should().ContainKey("Name");

    [Fact]
    public void A_name_longer_than_the_limit_is_rejected() =>
        new UpdateSchoolProfileRequest(new string('a', 101), null).Validate().Should().ContainKey("Name");

    [Theory]
    [InlineData("2026")]
    [InlineData("2026-27")]
    [InlineData("2026/2027")]
    [InlineData("2026-2028")]
    [InlineData("2027-2026")]
    [InlineData("abcd-efgh")]
    public void A_malformed_academic_year_is_rejected(string year) =>
        new UpdateSchoolProfileRequest("Greenfield Academy", year).Validate().Should().ContainKey("AcademicYear");

    [Fact]
    public void Surrounding_spaces_on_the_academic_year_are_ignored() =>
        new UpdateSchoolProfileRequest("Greenfield Academy", " 2026-2027 ").Validate().Should().BeEmpty();

    [Fact]
    public void Apply_trims_the_name_and_clears_a_blank_academic_year()
    {
        var profile = new SchoolProfile { AcademicYear = "2025-2026" };

        profile.Apply(new UpdateSchoolProfileRequest("  Greenfield Academy ", "  "));

        profile.Name.Should().Be("Greenfield Academy");
        profile.AcademicYear.Should().BeNull();
    }

    [Fact]
    public void The_profile_is_always_the_single_fixed_row() =>
        new SchoolProfile().Id.Should().Be(SchoolProfile.SingletonId);
}
