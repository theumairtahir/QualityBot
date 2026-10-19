using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.SystemInfo;
using Sis.Web.Pages;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class DashboardTests : BunitContext
{
    public DashboardTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Shows_connected_status_with_version_and_environment()
    {
        var info = new SystemInfoResponse("Student Information System", "1.2.3", "Testing", DateTimeOffset.UnixEpoch);
        UseApi(_ => StubHttp.Json(info));

        var cut = Render<Dashboard>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=api-status]").TextContent
                .Should().Contain("Connected").And.Contain("v1.2.3").And.Contain("Testing"));
    }

    [Fact]
    public void Shows_unreachable_status_when_the_api_call_fails()
    {
        UseApi(_ => throw new HttpRequestException("connection refused"));

        var cut = Render<Dashboard>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=api-status]").TextContent.Should().Contain("API unreachable"));
    }

    private void UseApi(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        Services.AddSingleton(new SystemInfoClient(StubHttp.Create(respond)));
}
