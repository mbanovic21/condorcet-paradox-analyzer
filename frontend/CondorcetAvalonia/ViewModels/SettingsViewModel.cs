using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CondorcetAvalonia.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly MainWindowViewModel _shell;

    public SettingsViewModel(MainWindowViewModel shell)
    {
        _shell = shell;
    }

    public string BackendBaseUrl => _shell.BackendBaseUrl;

    public string Notes
        => "Future-ready: this UI is built to integrate an AI worker (job-based) later without breaking navigation.";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
