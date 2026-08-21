using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CodePlus.Models;

namespace CodePlus.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    public ObservableCollection<ProjectSummary> Projects { get; } = new()
    {
        new("Avalonia Starter",     "C#",     "In Progress", 0.72),
        new("Mobile Companion",     "Dart",   "Planning",    0.18),
        new("ML Pipeline",          "Python", "Completed",   1.00),
        new("Design System",        "Figma",  "Review",      0.55),
        new("Internal CLI",         "Go",     "In Progress", 0.40),
        new("Marketing Site",       "NextJS", "Completed",   1.00),
    };
}
