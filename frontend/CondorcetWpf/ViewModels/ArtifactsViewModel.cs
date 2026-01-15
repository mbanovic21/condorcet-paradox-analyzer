using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CondorcetWpf.ViewModels;

public sealed class ArtifactsViewModel : INotifyPropertyChanged
{
    private readonly AnalyzeViewModel _analyze;

    public ArtifactsViewModel(AnalyzeViewModel analyze)
    {
        _analyze = analyze;
        _analyze.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(LastRunId));
            OnPropertyChanged(nameof(HasRunId));
        };

        CopyRunIdCommand = new RelayCommand(CopyRunId, () => HasRunId);
    }

    public string? LastRunId => _analyze.LastArtifactRunId;
    public bool HasRunId => !string.IsNullOrWhiteSpace(LastRunId);

    public RelayCommand CopyRunIdCommand { get; }

    private void CopyRunId()
    {
        if (!HasRunId) return;
        Clipboard.SetText(LastRunId!);
        MessageBox.Show("Artifact run id copied.", "Artifacts", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
