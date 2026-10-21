using Sis.Contracts.Students;

namespace Sis.Api.Tests.Features.Students;

public sealed class CreateStudentRequestTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);

    private static CreateStudentRequest Valid() =>
        new("Ada", "Lovelace", new DateOnly(2012, 5, 1), "ada@example.com", "Grade 5");

    [Fact]
    public void A_complete_request_is_valid() =>
        Valid().Validate(Today).Should().BeEmpty();

    [Fact]
    public void Only_first_name_last_name_and_class_are_required() =>
        (Valid() with { DateOfBirth = null, Email = null }).Validate(Today).Should().BeEmpty();

    [Fact]
    public void Missing_required_fields_are_reported()
    {
        var errors = new CreateStudentRequest(" ", null, null, null, "").Validate(Today);

        errors.Keys.Should().BeEquivalentTo("FirstName", "LastName", "ClassName");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("ada@")]
    [InlineData("ada@example")]
    public void A_malformed_email_is_rejected(string email) =>
        (Valid() with { Email = email }).Validate(Today).Should().ContainKey("Email");

    [Fact]
    public void An_email_longer_than_the_column_allows_is_rejected()
    {
        var email = new string('a', 250) + "@example.com";

        var errors = (Valid() with { Email = email }).Validate(Today);

        errors["Email"].Should().ContainSingle().Which.Should().Contain("254");
    }

    [Fact]
    public void An_email_of_exactly_254_characters_is_accepted()
    {
        var email = new string('a', 242) + "@example.com";

        email.Length.Should().Be(254);
        (Valid() with { Email = email }).Validate(Today).Should().BeEmpty();
    }

    [Theory]
    [InlineData("J0hn@#")]
    [InlineData("John3")]
    [InlineData("Ann@!#")]
    [InlineData("-Ann")]
    public void A_first_name_with_digits_or_special_characters_is_rejected(string name) =>
        (Valid() with { FirstName = name }).Validate(Today).Should().ContainKey("FirstName");

    [Theory]
    [InlineData("QAui123!")]
    [InlineData("Smith_2")]
    public void A_last_name_with_digits_or_special_characters_is_rejected(string name) =>
        (Valid() with { LastName = name }).Validate(Today).Should().ContainKey("LastName");

    [Theory]
    [InlineData("Anne-Marie")]
    [InlineData("O'Brien")]
    [InlineData("O’Neil")]
    [InlineData("Zoë")]
    [InlineData("  Ada Lovelace  ")]
    public void Real_world_names_are_accepted(string name) =>
        (Valid() with { FirstName = name, LastName = name }).Validate(Today).Should().BeEmpty();

    [Fact]
    public void An_unknown_class_is_rejected() =>
        (Valid() with { ClassName = "Grade 99" }).Validate(Today).Should().ContainKey("ClassName");

    [Fact]
    public void A_future_date_of_birth_is_rejected() =>
        (Valid() with { DateOfBirth = Today.AddDays(1) }).Validate(Today).Should().ContainKey("DateOfBirth");
}
