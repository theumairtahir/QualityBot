using System.Net.Http.Json;
using Sis.Contracts.SystemInfo;

namespace Sis.Web.Services;

public sealed class SystemInfoClient(HttpClient http)
{
    public async Task<SystemInfoResponse> GetAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<SystemInfoResponse>("api/system/info", cancellationToken)
        ?? throw new InvalidOperationException("The API returned an empty system info response.");
}
