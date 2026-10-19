using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sis.Web.Layout;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();

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
    public void Header_has_a_user_menu()
    {
        var cut = Render<MainLayout>();

        cut.Markup.Should().Contain("Demo Admin").And.Contain("Sign out");
    }
}
