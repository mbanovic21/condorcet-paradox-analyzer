using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace CondorcetAvalonia.ViewModels;

public sealed class ArtifactsViewModel : INotifyPropertyChanged
{
    private readonly AnalyzeViewModel _analyze;

    private string? _copyStatus;
    public string? CopyStatus
    {
        get => _copyStatus;
        private set
        {
            if (_copyStatus == value) return;
            _copyStatus = value;
            OnPropertyChanged();
        }
    }

    public ArtifactsViewModel(AnalyzeViewModel analyze)
    {
        _analyze = analyze;

        // prvo command (da ga compiler ne smatra potencijalno null u handleru)
        CopyRunIdCommand = new RelayCommand(CopyRunId, () => HasRunId);

        _analyze.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(LastRunId));
            OnPropertyChanged(nameof(HasRunId));

            // ako RelayCommand ima ovu metodu, refresh CanExecute
            CopyRunIdCommand.RaiseCanExecuteChanged();
        };
    }

    public string? LastRunId => _analyze.LastArtifactRunId;
    public bool HasRunId => !string.IsNullOrWhiteSpace(LastRunId);

    public RelayCommand CopyRunIdCommand { get; }

    private async void CopyRunId()
    {
        if (!HasRunId)
            return;

        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            CopyStatus = "Clipboard is not available.";
            return;
        }

        await clipboard.SetTextAsync(LastRunId!);
        CopyStatus = "Artifact run id copied.";
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window is null) return null;

            var top = TopLevel.GetTopLevel(window);
            return top?.Clipboard;
        }

        return null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
