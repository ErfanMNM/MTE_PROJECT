using System.Collections.Generic;
using CodePlus.Models;

namespace CodePlus.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public IReadOnlyList<ProjectSummary> Projects { get; } = new[]
    {
        new ProjectSummary("Avalonia Starter", "C#", "In Progress", 0.72),
        new ProjectSummary("Mobile Companion", "Dart", "Planning", 0.18),
        new ProjectSummary("ML Pipeline", "Python", "Completed", 1.0),
        new ProjectSummary("Design System", "Figma", "Review", 0.55),
    };

    public IReadOnlyList<Snippet> RecentSnippets { get; } = new[]
    {
        new Snippet("Toggle theme", "csharp",
            "var theme = MaterialThemeBase.CurrentTheme.SetBaseTheme(Theme.Dark);\nMaterialThemeBase.CurrentTheme = theme;",
            "theme, material"),
        new Snippet("Clipboard copy", "csharp",
            "await TopLevel.GetTopLevel(this).Clipboard.SetTextAsync(text);",
            "ui, clipboard"),
        new Snippet("Responsive grid", "xaml",
            "<UniformGrid Columns=\"3\" ColumnSpacing=\"12\" RowSpacing=\"12\" />",
            "layout"),
    };

    public string WelcomeTitle { get; } = "Welcome back, Thanh!";
    public string WelcomeSubtitle { get; } = "Today is a great day to ship something expressive.";
}
