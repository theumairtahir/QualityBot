namespace Sis.Web.Layout;

public sealed record AppMenuItem(string Label, string Href, string IconName);

public sealed record AppMenuSection(string Title, IReadOnlyList<AppMenuItem> Items);

/// <summary>
/// The app's navigation, in one place. The sidebar, the mobile title and the header title all
/// read from here, so adding a module is a one-line change.
/// </summary>
public static class AppMenu
{
    public static IReadOnlyList<AppMenuSection> Sections { get; } =
    [
        new("Overview",
        [
            new("Dashboard", "/", "home"),
        ]),
        new("Students",
        [
            new("Students", "/students", "academic-cap"),
            new("Admissions", "/admissions", "clipboard-document-check"),
        ]),
        new("Academics",
        [
            new("Classes", "/classes", "user-group"),
            new("Courses", "/courses", "book-open"),
            new("Attendance", "/attendance", "calendar-days"),
            new("Grades", "/grades", "chart-bar"),
        ]),
        new("Administration",
        [
            new("Staff", "/staff", "briefcase"),
            new("Fees", "/fees", "banknotes"),
            new("Reports", "/reports", "document-chart-bar"),
            new("Settings", "/settings", "cog-6-tooth"),
        ]),
    ];

    public static IEnumerable<AppMenuItem> Items => Sections.SelectMany(section => section.Items);

    /// <summary>
    /// The item whose route owns <paramref name="relativePath"/> — the path as returned by
    /// <c>NavigationManager.ToBaseRelativePath</c>, e.g. <c>"students/42?tab=fees"</c> → Students.
    /// </summary>
    public static AppMenuItem? FindByPath(string relativePath)
    {
        var path = "/" + relativePath.Split('?', '#')[0].Trim('/');

        return Items.FirstOrDefault(item =>
            string.Equals(path, item.Href, StringComparison.OrdinalIgnoreCase) ||
            (item.Href != "/" && path.StartsWith(item.Href + "/", StringComparison.OrdinalIgnoreCase)));
    }
}
