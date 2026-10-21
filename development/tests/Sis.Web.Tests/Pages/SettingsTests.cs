using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.School;
using Sis.Web.Pages;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class SettingsTests : BunitContext
{
    private static readonly SchoolProfileResponse Saved = new("Greenfield Academy", "2026-2027");

    private readonly List<HttpRequestMessage> _requests = [];

    public SettingsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Shows_the_current_name_and_academic_year()
    {
        UseApi(_ => StubHttp.Json(Saved));

        var cut = Render<Settings>();

        cut.WaitForAssertion(() =>
        {
            var fields = cut.FindComponents<TextField<string>>();
            fields[0].Instance.Value.Should().Be("Greenfield Academy");
            fields[1].Instance.Value.Should().Be("2026-2027");
        });
    }

    [Fact]
    public void A_blank_school_name_is_rejected_without_calling_the_api()
    {
        UseApi(_ => StubHttp.Json(Saved));
        var cut = Render<Settings>();
        cut.WaitForElement("[data-testid=school-form]");
        Type(cut, 0, "  ");

        cut.Find("[data-testid=school-form]").Submit();

        cut.Markup.Should().Contain("School name is required.");
        _requests.Should().NotContain(r => r.Method == HttpMethod.Put);
    }

    [Fact]
    public void A_malformed_academic_year_is_rejected_without_calling_the_api()
    {
        UseApi(_ => StubHttp.Json(Saved));
        var cut = Render<Settings>();
        cut.WaitForElement("[data-testid=school-form]");
        Type(cut, 1, "2026-27");

        cut.Find("[data-testid=school-form]").Submit();

        cut.Markup.Should().Contain("Use two consecutive years");
        _requests.Should().NotContain(r => r.Method == HttpMethod.Put);
    }

    [Fact]
    public void Saving_sends_the_profile_and_publishes_it_to_the_rest_of_the_app()
    {
        UseApi(request => StubHttp.Json(request.Method == HttpMethod.Put
            ? new SchoolProfileResponse("Riverside High", "2027-2028")
            : Saved));
        var cut = Render<Settings>();
        cut.WaitForElement("[data-testid=school-form]");
        Type(cut, 0, "Riverside High");
        Type(cut, 1, "2027-2028");

        cut.Find("[data-testid=school-form]").Submit();

        cut.WaitForAssertion(() =>
        {
            _requests.Should().ContainSingle(r => r.Method == HttpMethod.Put);
            Services.GetRequiredService<SchoolProfileState>().Profile.Name.Should().Be("Riverside High");
        });
    }

    [Fact]
    public void Server_side_validation_errors_show_next_to_the_field()
    {
        UseApi(request => request.Method == HttpMethod.Put
            ? new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new { errors = new Dictionary<string, string[]> { ["Name"] = ["School name is required."] } }),
            }
            : StubHttp.Json(Saved));
        var cut = Render<Settings>();
        cut.WaitForElement("[data-testid=school-form]");

        cut.Find("[data-testid=school-form]").Submit();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("School name is required."));
    }

    [Fact]
    public void Shows_an_error_when_the_api_is_unreachable()
    {
        UseApi(_ => throw new HttpRequestException("connection refused"));

        var cut = Render<Settings>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Couldn't load the school profile"));
    }

    private static void Type(IRenderedComponent<Settings> cut, int field, string value) =>
        cut.InvokeAsync(() => cut.FindComponents<TextField<string>>()[field].Instance.ValueChanged.InvokeAsync(value));

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var client = new SchoolProfileClient(StubHttp.Create(request =>
        {
            _requests.Add(request);
            return respond(request);
        }));
        Services.AddSingleton(client);
        Services.AddScoped(_ => new SchoolProfileState(client));
    }
}
