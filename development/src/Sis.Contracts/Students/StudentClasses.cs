namespace Sis.Contracts.Students;

/// <summary>The classes a student can be placed in. A fixed list until the Classes module exists.</summary>
public static class StudentClasses
{
    public static IReadOnlyList<string> Names { get; } =
        Enumerable.Range(1, 10).Select(grade => $"Grade {grade}").ToArray();
}
