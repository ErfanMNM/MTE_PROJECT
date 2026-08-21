namespace CodePlus.Models;

public enum NavigationKind
{
    Dashboard,
    Projects,
    Snippets,
    AiChat,
    Settings,
}

public sealed record NavigationItem(string Title, string Icon, NavigationKind Kind);
