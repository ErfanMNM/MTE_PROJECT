using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CodePlus.Models;

namespace CodePlus.ViewModels;

public partial class SnippetsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<Snippet> Snippets { get; } = new()
    {
        new("Toggle theme", "csharp",
            "var theme = MaterialThemeBase.CurrentTheme.SetBaseTheme(Theme.Dark);\nMaterialThemeBase.CurrentTheme = theme;",
            "theme, material"),
        new("Clipboard copy", "csharp",
            "await TopLevel.GetTopLevel(this).Clipboard.SetTextAsync(text);",
            "ui, clipboard"),
        new("Responsive grid", "xaml",
            "<UniformGrid Columns=\"3\" ColumnSpacing=\"12\" RowSpacing=\"12\" />",
            "layout"),
        new("Debounce helper", "csharp",
            "public static IDisposable Debounce(Action act, int ms) { ... }",
            "async, util"),
        new("SQL row count", "sql",
            "SELECT COUNT(*) FROM users WHERE active = 1;",
            "db"),
    };
}
