using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

public sealed class Student
{
    public Guid Id { get; set; }

    /// <summary>Assigned by the database when the student is added; never edited.</summary>
    public int RollNumber { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string ClassName { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Copies a validated request's details onto the student, trimming surrounding spaces.</summary>
    public void Apply(CreateStudentRequest request)
    {
        FirstName = request.FirstName!.Trim();
        LastName = request.LastName!.Trim();
        DateOfBirth = request.DateOfBirth;
        Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        ClassName = request.ClassName!;
    }

    public StudentResponse ToResponse() => new(Id, RollNumber, FirstName, LastName, DateOfBirth, Email, ClassName);
}

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(student => student.Id);
        builder.Property(student => student.RollNumber).UseIdentityByDefaultColumn();
        builder.HasIndex(student => student.RollNumber).IsUnique();
        builder.Property(student => student.FirstName).HasMaxLength(CreateStudentRequest.MaxNameLength);
        builder.Property(student => student.LastName).HasMaxLength(CreateStudentRequest.MaxNameLength);
        builder.Property(student => student.Email).HasMaxLength(CreateStudentRequest.MaxEmailLength);
        builder.Property(student => student.ClassName).HasMaxLength(50);
    }
}
