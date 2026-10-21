using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sis.Contracts.School;
using Sis.Web.Layout;
using Sis.Web.Services;
using Sis.Web.Tests.Support;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
        UseSchool(new SchoolProfileResponse("Demo campus", null));

        // ThemeSwitcher reads its JS module's init result into a private type that loose JS
        // interop cannot fake; it is the library's concern, not the shell's, so stub it out.
        ComponentFactories.AddStub<ThemeSwitcher>();
    }

    [Fact]
    public void Sidebar_lists_every_menu_section_and_item()
    {
        var cut = Render<MainLayout>();

        foreach (var section in AppMenu.Sections)
        {
            cut.Markup.Should().Contain(section.Title);
            foreach (var item in section.Items)
            {
                cut.Markup.Should().Contain(item.Label);
            }
        }
    }

    [Fact]
    public void Header_names_the_current_module()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/attendance");

        var cut = Render<MainLayout>();

        cut.Find("[data-testid=page-title]").TextContent.Trim().Should().Be("Attendance");
    }

    [Fact]
    public void Header_falls_back_for_paths_outside_the_menu()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/not-found");

        var cut = Render<MainLayout>();

        cut.Find("[data-testid=page-title]").TextContent.Trim().Should().Be("Page not found");
    }

    [Fact]
    public void Sidebar_header_shows_the_saved_school_name_and_academic_year()
    {
        UseSchool(new SchoolProfileResponse("Greenfield Academy", "2026-2027"));

        var cut = Render<MainLayout>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid=school-name]").TextContent.Trim().Should().Be("Greenfield Academy");
            cut.Find("[data-testid=school-subtitle]").TextContent.Should().Contain("2026-2027");
        });
    }

    [Fact]
    public void Sidebar_header_updates_when_the_profile_is_saved()
    {
        var cut = Render<MainLayout>();

        cut.InvokeAsync(() => Services.GetRequiredService<SchoolProfileState>()
            .Set(new SchoolProfileResponse("Riverside High", "2027-2028")));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid=school-name]").TextContent.Trim().Should().Be("Riverside High"));
    }

    [Fact]
    public void Sidebar_header_keeps_the_default_when_the_api_is_unreachable()
    {
        Services.AddScoped(_ => new SchoolProfileState(new SchoolProfileClient(
            StubHttp.Create(_ => throw new HttpRequestException("connection refused")))));

        var cut = Render<MainLayout>();

        cut.Find("[data-testid=school-name]").TextContent.Trim().Should().Be("Demo campus");
    }

    [Fact]
    public void Header_has_a_user_menu()
    {
        var cut = Render<MainLayout>();

        cut.Markup.Should().Contain("Demo Admin").And.Contain("Sign out");
    }

    private void UseSchool(SchoolProfileResponse profile) =>
        Services.AddScoped(_ => new SchoolProfileState(new SchoolProfileClient(
            StubHttp.Create(_ => StubHttp.Json(profile)))));
}
