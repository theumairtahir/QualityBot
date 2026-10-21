using System.Net;
using System.Net.Http.Json;
using Sis.Contracts.School;

namespace Sis.Web.Services;

/// <summary>The outcome of saving the profile: the saved profile, or the field errors the API reported.</summary>
public sealed record SaveSchoolProfileResult(SchoolProfileResponse? Profile, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => Profile is not null;
}

public sealed class SchoolProfileClient(HttpClient http)
{
    public async Task<SchoolProfileResponse> GetAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<SchoolProfileResponse>("api/school", cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty school profile.");

    public async Task<SaveSchoolProfileResult> UpdateAsync(
        UpdateSchoolProfileRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PutAsJsonAsync("api/school", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
            return new SaveSchoolProfileResult(null, problem?.Errors ?? new Dictionary<string, string[]>());
        }

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<SchoolProfileResponse>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty school profile.");
        return new SaveSchoolProfileResult(profile, new Dictionary<string, string[]>());
    }

    private sealed record ValidationProblem(Dictionary<string, string[]>? Errors);
}
