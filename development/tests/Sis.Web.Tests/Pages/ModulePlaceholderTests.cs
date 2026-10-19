using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sis.Web.Pages;
using TailwindUi.Blazor.Extensions;

namespace Sis.Web.Tests.Pages;

public sealed class ModulePlaceholderTests : BunitContext
{
    public ModulePlaceholderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddTailwindUi();
    }

    [Fact]
    public void Titles_itself_from_the_menu_item_for_the_current_path()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/fees");

        var cut = Render<ModulePlaceholder>();

        cut.Find("h1").TextContent.Trim().Should().Be("Fees");
        cut.Markup.Should().Contain("Fees is coming soon");
    }
}
