using System.Net;
using System.Net.Http.Json;
using Sis.Contracts.Students;

namespace Sis.Web.Services;

/// <summary>The outcome of adding a student: the saved student, or the field errors the API reported.</summary>
public sealed record CreateStudentResult(StudentResponse? Student, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Student is not null;
}

public sealed class StudentsClient(HttpClient http)
{
    public async Task<IReadOnlyList<StudentResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<StudentResponse>>("api/students", cancellationToken) ?? [];

    public async Task<CreateStudentResult> CreateAsync(
        CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/students", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
            return new CreateStudentResult(null, problem?.Errors ?? new Dictionary<string, string[]>());
        }

        response.EnsureSuccessStatusCode();
        var student = await response.Content.ReadFromJsonAsync<StudentResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty student.");
        return new CreateStudentResult(student, new Dictionary<string, string[]>());
    }

    private sealed record ValidationProblem(Dictionary<string, string[]>? Errors);
}
