using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sis.Contracts.Students;

namespace Sis.Api.Features.Students;

public sealed class Student
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public required string ClassName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public StudentResponse ToResponse() => new(Id, FirstName, LastName, DateOfBirth, Email, ClassName);
}

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(student => student.Id);
        builder.Property(student => student.FirstName).HasMaxLength(CreateStudentRequest.MaxNameLength);
        builder.Property(student => student.LastName).HasMaxLength(CreateStudentRequest.MaxNameLength);
        builder.Property(student => student.Email).HasMaxLength(CreateStudentRequest.MaxEmailLength);
        builder.Property(student => student.ClassName).HasMaxLength(50);
    }
}
