using Sis.Api.Features.Students;
using Sis.Contracts.Students;

namespace Sis.Api.Tests.Features.Students;

public sealed class StudentTests
{
    [Fact]
    public void Apply_copies_the_details_and_trims_surrounding_spaces()
    {
        var student = new Student();

        student.Apply(new CreateStudentRequest("  Ada ", " Lovelace", new DateOnly(2012, 5, 1), " ada@example.com ", "Grade 5"));

        student.FirstName.Should().Be("Ada");
        student.LastName.Should().Be("Lovelace");
        student.DateOfBirth.Should().Be(new DateOnly(2012, 5, 1));
        student.Email.Should().Be("ada@example.com");
        student.ClassName.Should().Be("Grade 5");
    }

    [Fact]
    public void Apply_clears_the_email_and_date_of_birth_when_they_are_removed()
    {
        var student = new Student { Email = "ada@example.com", DateOfBirth = new DateOnly(2012, 5, 1) };

        student.Apply(new CreateStudentRequest("Ada", "Lovelace", null, "  ", "Grade 5"));

        student.Email.Should().BeNull();
        student.DateOfBirth.Should().BeNull();
    }

    [Fact]
    public void Apply_never_changes_the_roll_number_or_id()
    {
        var id = Guid.NewGuid();
        var student = new Student { Id = id, RollNumber = 12 };

        student.Apply(new CreateStudentRequest("Ada", "Lovelace", null, null, "Grade 5"));

        student.Id.Should().Be(id);
        student.RollNumber.Should().Be(12);
    }
}
