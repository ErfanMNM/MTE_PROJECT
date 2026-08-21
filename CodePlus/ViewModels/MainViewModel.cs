using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePlus.Models;

namespace CodePlus.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage = null!;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    public NavigationItem[] NavigationItems { get; } =
    {
        new("Dashboard", "Home", NavigationKind.Dashboard),
        new("Projects",  "FolderMultiple", NavigationKind.Projects),
        new("Snippets",  "CodeTags", NavigationKind.Snippets),
        new("AI Chat",   "Robot", NavigationKind.AiChat),
        new("Settings",  "Cog", NavigationKind.Settings),
    };

    public DashboardViewModel Dashboard { get; } = new();
    public ProjectsViewModel Projects { get; } = new();
    public SnippetsViewModel Snippets { get; } = new();
    public AiChatViewModel AiChat { get; } = new();
    public SettingsViewModel Settings { get; } = new();

    public MainViewModel()
    {
        _selectedNavigationItem = NavigationItems[0];
        _currentPage = Dashboard;
    }

    [RelayCommand]
    private void SelectNavigation(NavigationItem? item)
    {
        SelectedNavigationItem = item;
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null) return;
        CurrentPage = value.Kind switch
        {
            NavigationKind.Projects  => Projects,
            NavigationKind.Snippets  => Snippets,
            NavigationKind.AiChat    => AiChat,
            NavigationKind.Settings  => Settings,
            _                        => Dashboard,
        };
    }
}
