using System.Net;
using System.Net.Http.Json;
using Sis.Contracts.Students;

namespace Sis.Web.Services;

/// <summary>The outcome of saving a student: the saved student, or the field errors the API reported.</summary>
public sealed record SaveStudentResult(StudentResponse? Student, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Student is not null;
}

public sealed class StudentsClient(HttpClient http)
{
    public async Task<IReadOnlyList<StudentResponse>> ListAsync(
        string? search = null, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? "api/students"
            : $"api/students?search={Uri.EscapeDataString(search.Trim())}";

        return await http.GetFromJsonAsync<IReadOnlyList<StudentResponse>>(url, cancellationToken) ?? [];
    }

    /// <summary>The student, or null if there is no such student.</summary>
    public async Task<StudentResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/students/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentResponse>(cancellationToken);
    }

    public async Task<SaveStudentResult> CreateAsync(
        CreateStudentRequest request, CancellationToken cancellationToken = default) =>
        await ReadSaveResultAsync(await http.PostAsJsonAsync("api/students", request, cancellationToken), cancellationToken);

    public async Task<SaveStudentResult> UpdateAsync(
        Guid id, CreateStudentRequest request, CancellationToken cancellationToken = default) =>
        await ReadSaveResultAsync(await http.PutAsJsonAsync($"api/students/{id}", request, cancellationToken), cancellationToken);

    /// <summary>Removes the student. A student that is already gone counts as removed.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.DeleteAsync($"api/students/{id}", cancellationToken);

        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private static async Task<SaveStudentResult> ReadSaveResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
                return new SaveStudentResult(null, problem?.Errors ?? new Dictionary<string, string[]>());
            }

            response.EnsureSuccessStatusCode();
            var student = await response.Content.ReadFromJsonAsync<StudentResponse>(cancellationToken)
                ?? throw new InvalidOperationException("The API returned an empty student.");
            return new SaveStudentResult(student, new Dictionary<string, string[]>());
        }
    }

    private sealed record ValidationProblem(Dictionary<string, string[]>? Errors);
}
