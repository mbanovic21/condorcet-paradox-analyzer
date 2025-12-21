using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using CondorcetWpf.Models;
using CondorcetWpf.Services;

namespace CondorcetWpf.ViewModels;

public sealed class AnalyzeViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _api;
    private readonly Action<string> _setStatus;
    private readonly Action _notifyHasInputChanged;
    private List<DfsStep> _traceSteps = new();

    private readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private ElectionInput? _input;
    private AnalysisResult? _result;

    public RelayCommand LoadJsonCommand { get; }
    public RelayCommand AnalyzeCommand { get; }

    public RelayCommand TracePrevCommand { get; }
    public RelayCommand TraceNextCommand { get; }
    public RelayCommand TraceResetCommand { get; }

    public AnalyzeViewModel(ApiClient api, Action<string> setStatus, Action notifyHasInputChanged)
    {
        _api = api;
        _setStatus = setStatus;
        _notifyHasInputChanged = notifyHasInputChanged;

        LoadJsonCommand = new RelayCommand(LoadJson);
        AnalyzeCommand = new RelayCommand(async () => await AnalyzeAsync(), () => HasInput);

        TracePrevCommand = new RelayCommand(() => CurrentTraceIndex = Math.Max(0, CurrentTraceIndex - 1), () => _traceSteps.Count > 0 && CurrentTraceIndex > 0);
        TraceNextCommand = new RelayCommand(() => CurrentTraceIndex = Math.Min(_traceSteps.Count - 1, CurrentTraceIndex + 1), () => _traceSteps.Count > 0 && CurrentTraceIndex < _traceSteps.Count - 1);
        TraceResetCommand = new RelayCommand(() => CurrentTraceIndex = 0, () => _traceSteps.Count > 0);

        TracePrevCommand = new RelayCommand(
            () => CurrentTraceIndex = Math.Max(0, CurrentTraceIndex - 1),
            () => HasTrace && CurrentTraceIndex > 0
        );

        TraceNextCommand = new RelayCommand(
            () => CurrentTraceIndex = Math.Min(_traceSteps.Count - 1, CurrentTraceIndex + 1),
            () => HasTrace && CurrentTraceIndex < _traceSteps.Count - 1
        );

        TraceResetCommand = new RelayCommand(
            () => CurrentTraceIndex = 0,
            () => HasTrace
        );

        SummaryLine1 = "No analysis yet.";
        SummaryLine2 = "";
        CycleText = "";
    }

    public bool HasInput => _input is not null;

    // Artifact options (set by shell)
    private bool _saveArtifact;
    public bool SaveArtifact { 
        get => _saveArtifact; 
        set { _saveArtifact = value; OnPropertyChanged(); } 
    }

    private string _artifactTag = "run";
    public string ArtifactTag { 
        get => _artifactTag; 
        set { _artifactTag = value; OnPropertyChanged(); } 
    }

    private string _artifactNotes = "";
    public string ArtifactNotes { 
        get => _artifactNotes; 
        set { _artifactNotes = value; OnPropertyChanged(); } 
    }

    private bool _includeTrace;
    public bool IncludeTrace
    {
        get => _includeTrace;
        set { _includeTrace = value; OnPropertyChanged(); }
    }

    private int _currentTraceIndex;
    public bool HasTrace => _traceSteps.Count > 0;

    public int CurrentTraceIndex
    {
        get => _currentTraceIndex;
        set { _currentTraceIndex = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(CurrentTrace)); 
            OnPropertyChanged(nameof(CurrentStackText));

            TracePrevCommand.RaiseCanExecuteChanged();
            TraceNextCommand.RaiseCanExecuteChanged();
            TraceResetCommand.RaiseCanExecuteChanged();
        }
    }

    public DfsStep? CurrentTrace
    => (_traceSteps.Count == 0 || CurrentTraceIndex < 0 || CurrentTraceIndex >= _traceSteps.Count)
        ? null
        : _traceSteps[CurrentTraceIndex];

    public string CurrentStackText
        => CurrentTrace is null ? "" : string.Join(" → ", CurrentTrace.Stack);


    // Result summary
    private string _summary1 = "";
    public string SummaryLine1 { 
        get => _summary1; 
        set { _summary1 = value; OnPropertyChanged(); } 
    }

    private string _summary2 = "";
    public string SummaryLine2 { 
        get => _summary2; 
        set { _summary2 = value; OnPropertyChanged(); } 
    }

    private string _cycleText = "";
    public string CycleText { 
        get => _cycleText; 
        set { _cycleText = value; OnPropertyChanged(); } 
    }

    private BitmapImage? _graphImage;
    public BitmapImage? GraphImage { get => _graphImage; set { _graphImage = value; OnPropertyChanged(); } }

    private string? _lastArtifactRunId;
    public string? LastArtifactRunId { get => _lastArtifactRunId; set { _lastArtifactRunId = value; OnPropertyChanged(); } }

    public ObservableCollection<RowKV> WinnersRows { get; } = new();
    public ObservableCollection<RowScore> BordaRows { get; } = new();
    public ObservableCollection<RowScore> PluralityRows { get; } = new();
    public ObservableCollection<RowScore> CopelandRows { get; } = new();
    public ObservableCollection<RowScore> MinimaxRows { get; } = new();

    public ObservableCollection<Dictionary<string, object>> MatrixARows { get; } = new();
    public ObservableCollection<Dictionary<string, object>> MatrixMarginRows { get; } = new();

    public ObservableCollection<TraceRow> TraceRows { get; } = new();

    private void LoadJson()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
            _input = JsonSerializer.Deserialize<ElectionInput>(text, _jsonOpts);
            if (_input is null) throw new InvalidOperationException("Could not parse JSON.");

            _setStatus($"Loaded: {Path.GetFileName(dlg.FileName)} (candidates: {string.Join(", ", _input.Candidates)})");

            SummaryLine1 = "Ready to analyze.";
            SummaryLine2 = "";
            CycleText = "";
            LastArtifactRunId = null;

            ClearTables();
            GraphImage = null;

            OnPropertyChanged(nameof(HasInput));
            _notifyHasInputChanged();
            AnalyzeCommand.RaiseCanExecuteChanged();
        } catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AnalyzeAsync()
    {
        if (_input is null) return;

        try
        {
            _setStatus("Analyzing...");
            _result = await _api.AnalyzeAsync(
                _input,
                saveArtifact: SaveArtifact,
                tag: string.IsNullOrWhiteSpace(ArtifactTag) ? "run" : ArtifactTag,
                notes: ArtifactNotes ?? "",
                includeTrace: IncludeTrace
            );

            if (SaveArtifact && !string.IsNullOrWhiteSpace(_result.ArtifactRunId))
            {
                LastArtifactRunId = _result.ArtifactRunId;
                _setStatus($"Done. Saved artifact: {LastArtifactRunId}");
            } else
            {
                _setStatus("Done.");
            }

            ApplyResultToUi(_result);
            LoadTrace(_result.DfsTrace);
        } catch (Exception ex)
        {
            _setStatus("Error.");
            MessageBox.Show(ex.Message, "Analyze error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTrace(DfsTrace? trace)
    {
        _traceSteps = trace?.Steps ?? new List<CondorcetWpf.Models.DfsStep>();

        TraceRows.Clear();
        foreach (var s in _traceSteps)
        {
            TraceRows.Add(new TraceRow(
                s.Index,
                s.Action,
                s.U ?? "",
                s.V ?? "",
                s.Message ?? "",
                string.Join(" → ", s.Stack)
            ));
        }

        CurrentTraceIndex = _traceSteps.Count > 0 ? 0 : 0;

        OnPropertyChanged(nameof(HasTrace));
        TracePrevCommand.RaiseCanExecuteChanged();
        TraceNextCommand.RaiseCanExecuteChanged();
        TraceResetCommand.RaiseCanExecuteChanged();
    }

    private void ClearTables()
    {
        WinnersRows.Clear();
        BordaRows.Clear();
        PluralityRows.Clear();
        CopelandRows.Clear();
        MinimaxRows.Clear();
        MatrixARows.Clear();
        MatrixMarginRows.Clear();
    }

    private void ApplyResultToUi(AnalysisResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.CondorcetWinner))
            SummaryLine1 = $"Condorcet winner: {result.CondorcetWinner}";
        else
            SummaryLine1 = "No Condorcet winner.";

        SummaryLine2 = result.CycleInfo.HasCycle
            ? "Condorcet paradox detected (directed cycle exists)."
            : "No directed cycle detected.";

        if (result.CycleInfo.HasCycle && result.CycleInfo.Cycle is not null)
            CycleText = "Cycle: " + string.Join(" → ", result.CycleInfo.Cycle);
        else
            CycleText = "";

        WinnersRows.Clear();
        foreach (var kv in result.Winners.OrderBy(k => k.Key))
            WinnersRows.Add(new RowKV(kv.Key, kv.Value ?? "(tie/none)"));

        BordaRows.Clear();
        foreach (var kv in result.Scores.Borda.OrderByDescending(k => k.Value))
            BordaRows.Add(new RowScore(kv.Key, kv.Value));

        PluralityRows.Clear();
        foreach (var kv in result.Scores.Plurality.OrderByDescending(k => k.Value))
            PluralityRows.Add(new RowScore(kv.Key, kv.Value));

        CopelandRows.Clear();
        foreach (var kv in result.Scores.Copeland.OrderByDescending(k => k.Value))
            CopelandRows.Add(new RowScore(kv.Key, kv.Value));

        MinimaxRows.Clear();
        foreach (var kv in result.Scores.Minimax.OrderByDescending(k => k.Value))
            MinimaxRows.Add(new RowScore(kv.Key, kv.Value));

        var cands = result.Pairwise.Candidates;

        MatrixARows.Clear();
        for (int i = 0; i < cands.Count; i++)
        {
            var row = new Dictionary<string, object> { ["Row"] = cands[i] };
            for (int j = 0; j < cands.Count; j++)
                row[cands[j]] = result.Pairwise.A[i][j];
            MatrixARows.Add(row);
        }

        MatrixMarginRows.Clear();
        for (int i = 0; i < cands.Count; i++)
        {
            var row = new Dictionary<string, object> { ["Row"] = cands[i] };
            for (int j = 0; j < cands.Count; j++)
                row[cands[j]] = result.Pairwise.Margin[i][j];
            MatrixMarginRows.Add(row);
        }

        GraphImage = DecodeBase64Png(result.GraphPngBase64);
    }

    private static BitmapImage? DecodeBase64Png(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64)) return null;
        var bytes = Convert.FromBase64String(b64);

        var img = new BitmapImage();
        using var ms = new MemoryStream(bytes);
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record RowKV(string Method, string Winner);
public sealed record RowScore(string Candidate, int Score);
public sealed record TraceRow(int Index, string Action, string U, string V, string Message, string Stack);
