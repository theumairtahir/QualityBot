namespace Sis.Contracts.School;

/// <summary>What <c>GET /api/school</c> returns: the school's name and the current academic year.</summary>
public sealed record SchoolProfileResponse(string Name, string? AcademicYear)
{
    /// <summary>Shown until an admin saves a profile.</summary>
    public static SchoolProfileResponse Default { get; } = new("Demo campus", null);
}
