using System.Reflection;
using Microsoft.AspNetCore.Components;
using Sis.Web.Layout;

namespace Sis.Web.Tests.Pages;

public sealed class RoutingTests
{
    private static readonly HashSet<string> RouteTemplates = typeof(App).Assembly.GetTypes()
        .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
        .Select(route => route.Template)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static TheoryData<string> MenuLinks => new(AppMenu.Items.Select(item => item.Href));

    [Theory]
    [MemberData(nameof(MenuLinks))]
    public void Every_menu_link_has_a_page(string href) =>
        RouteTemplates.Should().Contain(href);
}
