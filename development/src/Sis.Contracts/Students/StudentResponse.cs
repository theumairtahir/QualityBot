namespace Sis.Contracts.Students;

public sealed record StudentResponse(
    Guid Id,
    int RollNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Email,
    string ClassName);
