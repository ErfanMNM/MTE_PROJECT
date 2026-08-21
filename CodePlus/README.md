# CodePlus

A cross-platform desktop app built with **Avalonia 12** + **Material.Avalonia 3.18** (Material Design / Material 3 Expressive-inspired UI) on **.NET 10**.

## Features

- Sidebar navigation with expressive rounded cards (Dashboard / Projects / Snippets / AI Chat / Settings)
- Material You-style palette (`MaterialTheme` with Primary + Secondary colors)
- Springy page transitions (`TransitioningContentControl` + `CrossFade`)
- Expressive shapes: extra-rounded corners, pill chips, soft gradient hero
- MVVM via `CommunityToolkit.Mvvm` source generators

## Run

```powershell
cd CodePlus
dotnet run
```

## Project layout

```
CodePlus/
├─ App.axaml(.cs)            # Material 3 theme registration
├─ Program.cs                # Avalonia bootstrap
├─ Models/                   # NavigationItem, ProjectSummary, Snippet
├─ ViewModels/               # MVVM with [ObservableProperty] / [RelayCommand]
└─ Views/
   ├─ MainWindow.axaml       # Shell: sidebar + top app bar + page host
   ├─ DashboardView.axaml    # Hero + stats + project cards + snippets
   ├─ ProjectsView.axaml     # Project grid
   ├─ SnippetsView.axaml     # Snippet list with code blocks
   ├─ AiChatView.axaml       # Chat bubbles with role converters
   └─ SettingsView.axaml     # Theme / dynamic color / corner radius
```

## Tech

- .NET 10 (`net10.0`)
- Avalonia 12.1.1 (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`)
- Material.Avalonia 3.18.0 (+ `Material.Avalonia.Dialogs`)
- AvaloniaUI.DiagnosticsSupport 2.2.3 (Debug only)
- CommunityToolkit.Mvvm 8.4.2
