using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodePlus.ViewModels;

public partial class AiChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _input = string.Empty;

    public ObservableCollection<ChatMessage> Messages { get; } = new()
    {
        new("assistant", "Hi! I'm CodePlus AI. Ask me about your code, snippets, or architecture."),
        new("user",      "Explain the M3 expressive motion principles."),
        new("assistant", "Material 3 expressive emphasizes springy transitions, layered color, " +
                         "and shapes that respond to focus and interaction."),
    };

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrWhiteSpace(Input)) return;
        Messages.Add(new ChatMessage("user", Input));
        Messages.Add(new ChatMessage("assistant", $"You said: \"{Input}\". (Stub response)"));
        Input = string.Empty;
    }
}

public sealed record ChatMessage(string Role, string Text);
