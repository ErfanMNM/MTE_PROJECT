using CommunityToolkit.Mvvm.ComponentModel;

namespace CodePlus.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isDarkMode = true;
    [ObservableProperty] private bool _useDynamicColor = true;
    [ObservableProperty] private bool _enableExpressiveMotion = true;
    [ObservableProperty] private string _primaryColor = "Indigo";
    [ObservableProperty] private double _cornerRadius = 20;
}
