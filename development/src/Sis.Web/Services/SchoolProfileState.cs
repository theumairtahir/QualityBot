using System.Text.Json;
using Sis.Contracts.School;

namespace Sis.Web.Services;

/// <summary>
/// The school profile as the whole app sees it. The layout shows it, the Settings page changes it;
/// <see cref="Changed"/> keeps the two in step without a page reload.
/// </summary>
public sealed class SchoolProfileState(SchoolProfileClient client)
{
    private bool _loaded;

    public SchoolProfileResponse Profile { get; private set; } = SchoolProfileResponse.Default;

    public event Action? Changed;

    /// <summary>Fetches the profile once. If the API is down the default stays in place.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        try
        {
            Set(await client.GetAsync());
            _loaded = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or NotSupportedException or InvalidOperationException)
        {
            // Leave the default; the next page that asks will try again.
        }
    }

    /// <summary>Publishes a profile the API has just saved.</summary>
    public void Set(SchoolProfileResponse profile)
    {
        Profile = profile;
        _loaded = true;
        Changed?.Invoke();
    }
}
