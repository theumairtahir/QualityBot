using Sis.Web.Layout;
using TailwindUi.Blazor.Components;
using TailwindUi.Blazor.Theming;

namespace Sis.Web.Tests.Layout;

public sealed class AppMenuTests
{
    [Fact]
    public void Menu_links_are_unique() =>
        AppMenu.Items.Select(item => item.Href).Should().OnlyHaveUniqueItems();

    [Fact]
    public void Every_menu_icon_exists_in_the_icon_set() =>
        AppMenu.Items.Should().OnlyContain(item => HeroIconData.Contains(IconVariant.Outline, item.IconName));

    [Theory]
    [InlineData("", "Dashboard")]
    [InlineData("students", "Students")]
    [InlineData("students/42", "Students")]
    [InlineData("Attendance?date=2026-10-19", "Attendance")]
    public void FindByPath_returns_the_menu_item_that_owns_the_path(string relativePath, string expectedLabel) =>
        AppMenu.FindByPath(relativePath)!.Label.Should().Be(expectedLabel);

    [Theory]
    [InlineData("not-found")]
    [InlineData("studentsx")]
    public void FindByPath_returns_null_outside_the_menu(string relativePath) =>
        AppMenu.FindByPath(relativePath).Should().BeNull();
}
