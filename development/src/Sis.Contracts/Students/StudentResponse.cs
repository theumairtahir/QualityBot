namespace Sis.Contracts.Students;

public sealed record StudentResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Email,
    string ClassName);
