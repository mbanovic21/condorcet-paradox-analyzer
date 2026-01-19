using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CondorcetAvalonia.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly AnalyzeViewModel _analyze;

    public DashboardViewModel(AnalyzeViewModel analyze)
    {
        _analyze = analyze;

        _analyze.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(Headline));
            OnPropertyChanged(nameof(Subline));
            OnPropertyChanged(nameof(ArtifactInfo));
        };
    }

    public string Headline
        => string.IsNullOrWhiteSpace(_analyze.SummaryLine1) ? "Welcome." : _analyze.SummaryLine1;

    public string Subline
        => string.IsNullOrWhiteSpace(_analyze.SummaryLine2) ? "Load JSON and click Analyze." : _analyze.SummaryLine2;

    public string ArtifactInfo
        => string.IsNullOrWhiteSpace(_analyze.LastArtifactRunId)
            ? "No artifact saved yet."
            : $"Last artifact run: {_analyze.LastArtifactRunId}";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
