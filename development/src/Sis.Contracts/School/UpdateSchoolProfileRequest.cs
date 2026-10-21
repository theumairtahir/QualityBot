using System.Text.RegularExpressions;

namespace Sis.Contracts.School;

/// <summary>The body of <c>PUT /api/school</c>. The API and the form share <see cref="Validate"/>.</summary>
public sealed partial record UpdateSchoolProfileRequest(string? Name, string? AcademicYear)
{
    public const int MaxNameLength = 100;

    /// <summary>Field name → messages. Empty when the request is valid.</summary>
    public IReadOnlyDictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors[nameof(Name)] = ["School name is required."];
        }
        else if (Name.Trim().Length > MaxNameLength)
        {
            errors[nameof(Name)] = [$"School name must be at most {MaxNameLength} characters."];
        }

        if (!string.IsNullOrWhiteSpace(AcademicYear) && !IsAcademicYear(AcademicYear.Trim()))
        {
            errors[nameof(AcademicYear)] = ["Use two consecutive years, like 2026-2027."];
        }

        return errors;
    }

    // "2026-2027": four digits, a hyphen, then the following year.
    private static bool IsAcademicYear(string value) =>
        YearPattern().Match(value) is { Success: true } match &&
        int.Parse(match.Groups[2].Value) == int.Parse(match.Groups[1].Value) + 1;

    [GeneratedRegex(@"^(\d{4})-(\d{4})$")]
    private static partial Regex YearPattern();
}
