using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Sis.Contracts.Students;

/// <summary>The body of <c>POST /api/students</c>. The API and the form share <see cref="Validate"/>.</summary>
public sealed partial record CreateStudentRequest(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Email,
    string? ClassName)
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 254;

    /// <summary>Field name → messages. Empty when the request is valid.</summary>
    public IReadOnlyDictionary<string, string[]> Validate(DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        CheckName(errors, nameof(FirstName), "First name", FirstName);
        CheckName(errors, nameof(LastName), "Last name", LastName);

        if (string.IsNullOrWhiteSpace(ClassName))
        {
            errors[nameof(ClassName)] = ["Class is required."];
        }
        else if (!StudentClasses.Names.Contains(ClassName))
        {
            errors[nameof(ClassName)] = ["Pick a class from the list."];
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            if (Email.Trim().Length > MaxEmailLength)
            {
                errors[nameof(Email)] = [$"Email must be at most {MaxEmailLength} characters."];
            }
            else if (!IsEmail(Email))
            {
                errors[nameof(Email)] = ["Enter a valid email address."];
            }
        }

        if (DateOfBirth is { } dob && dob > today)
        {
            errors[nameof(DateOfBirth)] = ["Date of birth can't be in the future."];
        }

        return errors;
    }

    private static void CheckName(Dictionary<string, string[]> errors, string key, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = [$"{label} is required."];
        }
        else if (value.Trim().Length > MaxNameLength)
        {
            errors[key] = [$"{label} must be at most {MaxNameLength} characters."];
        }
        else if (!NamePattern().IsMatch(value.Trim()))
        {
            errors[key] = [$"{label} can only contain letters, spaces, hyphens and apostrophes."];
        }
    }

    // Letters (any script) with optional marks, spaces, hyphens and apostrophes: "Anne-Marie", "O'Brien".
    [GeneratedRegex(@"^\p{L}[\p{L}\p{M} '’-]*$")]
    private static partial Regex NamePattern();

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value.Trim(), out var address) && address.Address == value.Trim() && value.Contains('.');
}
