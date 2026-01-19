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
using Avalonia.Media.Imaging;
using CondorcetAvalonia.Models;
using CondorcetAvalonia.Services;

namespace CondorcetAvalonia.ViewModels;

public sealed class AnalyzeViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _api;
    private readonly Action<string> _setStatus;
    private readonly Action _notifyHasInputChanged;

    private readonly JsonSerializerOptions _jsonOpts = new()
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

    private List<DfsStep> _traceSteps = new();

    public AnalyzeViewModel(ApiClient api, Action<string> setStatus, Action notifyHasInputChanged)
    {
        _api = api;
        _setStatus = setStatus;
        _notifyHasInputChanged = notifyHasInputChanged;

        LoadJsonCommand = new RelayCommand(async () => await LoadJsonAsync());
        AnalyzeCommand = new RelayCommand(async () => await AnalyzeAsync(), () => HasInput);

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

    // Artifact options (set by shell / MainWindowViewModel)
    private bool _saveArtifact;
    public bool SaveArtifact { get => _saveArtifact; set { _saveArtifact = value; OnPropertyChanged(); } }

    private string _artifactTag = "run";
    public string ArtifactTag { get => _artifactTag; set { _artifactTag = value; OnPropertyChanged(); } }

    private string _artifactNotes = "";
    public string ArtifactNotes { get => _artifactNotes; set { _artifactNotes = value; OnPropertyChanged(); } }

    private bool _includeTrace = true;
    public bool IncludeTrace { get => _includeTrace; set { _includeTrace = value; OnPropertyChanged(); } }

    // ===== DFS trace access for DfsSimulationViewModel =====
    private DfsTrace? _lastDfsTrace;
    public DfsTrace? LastDfsTrace
    {
        get => _lastDfsTrace;
        private set { _lastDfsTrace = value; OnPropertyChanged(); }
    }

    // Trace (table + step index)
    private int _currentTraceIndex;
    public bool HasTrace => _traceSteps.Count > 0;

    public int CurrentTraceIndex
    {
        get => _currentTraceIndex;
        set
        {
            _currentTraceIndex = value;
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

    // Graph layout / image
    private Dictionary<string, List<double>>? _graphLayout;
    public Dictionary<string, List<double>>? GraphLayout
    {
        get => _graphLayout;
        private set { _graphLayout = value; OnPropertyChanged(); }
    }

    private Bitmap? _graphImage;
    public Bitmap? GraphImage { get => _graphImage; set { _graphImage = value; OnPropertyChanged(); } }

    // Summary
    private string _summary1 = "";
    public string SummaryLine1 { get => _summary1; set { _summary1 = value; OnPropertyChanged(); } }

    private string _summary2 = "";
    public string SummaryLine2 { get => _summary2; set { _summary2 = value; OnPropertyChanged(); } }

    private string _cycleText = "";
    public string CycleText { get => _cycleText; set { _cycleText = value; OnPropertyChanged(); } }

    private string? _lastArtifactRunId;
    public string? LastArtifactRunId { get => _lastArtifactRunId; set { _lastArtifactRunId = value; OnPropertyChanged(); } }

    // Tables
    public ObservableCollection<RowKV> WinnersRows { get; } = new();
    public ObservableCollection<RowScore> BordaRows { get; } = new();
    public ObservableCollection<RowScore> PluralityRows { get; } = new();
    public ObservableCollection<RowScore> CopelandRows { get; } = new();
    public ObservableCollection<RowScore> MinimaxRows { get; } = new();

    // Matrices (dictionary rows so DataGrid can AutoGenerateColumns)
    public ObservableCollection<Dictionary<string, object>> MatrixVotesRows { get; } = new();    // from N
    public ObservableCollection<Dictionary<string, object>> MatrixARows { get; } = new();       // from A
    public ObservableCollection<Dictionary<string, object>> MatrixMarginRows { get; } = new();  // from margin
    public ObservableCollection<Dictionary<string, object>> MatrixPercentRows { get; } = new(); // computed from N
    public ObservableCollection<Dictionary<string, object>> MatrixSchulzeRows { get; } = new(); // computed from margin

    public ObservableCollection<RankedPairRow> RankedPairsRows { get; } = new(); // computed from margin (+N)

    // DFS trace table
    public ObservableCollection<TraceRow> TraceRows { get; } = new();

    private async Task LoadJsonAsync()
    {
        var path = await AvaloniaFileDialog.PickJsonFilePathAsync();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var text = await File.ReadAllTextAsync(path, Encoding.UTF8);
            _input = JsonSerializer.Deserialize<ElectionInput>(text, _jsonOpts);
            if (_input is null) throw new InvalidOperationException("Could not parse JSON.");

            _setStatus($"Loaded: {Path.GetFileName(path)} (candidates: {string.Join(", ", _input.Candidates)})");

            SummaryLine1 = "Ready to analyze.";
            SummaryLine2 = "";
            CycleText = "";
            LastArtifactRunId = null;

            ClearTables();
            GraphImage = null;
            LastDfsTrace = null;

            OnPropertyChanged(nameof(HasInput));
            _notifyHasInputChanged();
            AnalyzeCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            await AvaloniaFileDialog.ShowErrorAsync("Load error", ex.Message);
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

            if (_result is null) return;

            // expose for other VMs (DFS simulation)
            LastDfsTrace = _result.DfsTrace;

            GraphLayout = _result.GraphLayout;

            if (SaveArtifact && !string.IsNullOrWhiteSpace(_result.ArtifactRunId))
            {
                LastArtifactRunId = _result.ArtifactRunId;
                _setStatus($"Done. Saved artifact: {LastArtifactRunId}");
            }
            else
            {
                _setStatus("Done.");
            }

            ApplyResultToUi(_result);
            LoadTrace(_result.DfsTrace);
        }
        catch (Exception ex)
        {
            _setStatus("Error.");
            await AvaloniaFileDialog.ShowErrorAsync("Analyze error", ex.Message);
        }
    }

    private void LoadTrace(DfsTrace? trace)
    {
        _traceSteps = trace?.Steps ?? new List<DfsStep>();

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

        CurrentTraceIndex = 0;

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

        MatrixVotesRows.Clear();
        MatrixARows.Clear();
        MatrixMarginRows.Clear();
        MatrixPercentRows.Clear();
        MatrixSchulzeRows.Clear();

        RankedPairsRows.Clear();

        TraceRows.Clear();
        _traceSteps = new List<DfsStep>();
        CurrentTraceIndex = 0;
        OnPropertyChanged(nameof(HasTrace));
    }

    private void ApplyResultToUi(AnalysisResult result)
    {
        SummaryLine1 = !string.IsNullOrWhiteSpace(result.CondorcetWinner)
            ? $"Condorcet winner: {result.CondorcetWinner}"
            : "No Condorcet winner.";

        SummaryLine2 = result.CycleInfo.HasCycle
            ? "Condorcet paradox detected!"
            : "No directed cycle detected.";

        CycleText = result.CycleInfo.Cycle is not null
            ? "Cycle: " + string.Join(" → ", result.CycleInfo.Cycle)
            : "";

        WinnersRows.Clear();
        foreach (var kv in result.Winners.OrderBy(k => k.Key))
            WinnersRows.Add(new RowKV(kv.Key, kv.Value ?? "(none)"));

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

        // Graph image
        GraphImage = DecodeBase64Png(result.GraphPngBase64);

        // Matrices
        var cands = result.Pairwise.Candidates;

        MatrixVotesRows.Clear();
        FillIntMatrix(MatrixVotesRows, cands, result.Pairwise.N);

        MatrixARows.Clear();
        FillIntMatrix(MatrixARows, cands, result.Pairwise.A);

        MatrixMarginRows.Clear();
        FillIntMatrix(MatrixMarginRows, cands, result.Pairwise.Margin);

        // Percent computed from N
        MatrixPercentRows.Clear();
        FillPercentFromN(MatrixPercentRows, cands, result.Pairwise.N);

        // Schulze strongest paths computed from margin
        MatrixSchulzeRows.Clear();
        var schulze = ComputeSchulzePaths(result.Pairwise.Margin);
        FillIntMatrix(MatrixSchulzeRows, cands, schulze);

        // Ranked pairs computed from margin (+ N for strength)
        RankedPairsRows.Clear();
        var rp = ComputeRankedPairs(cands, result.Pairwise.Margin, result.Pairwise.N);
        foreach (var row in rp)
            RankedPairsRows.Add(row);
    }

    private static void FillIntMatrix(
        ObservableCollection<Dictionary<string, object>> target,
        List<string> candidates,
        List<List<int>> data)
    {
        int n = candidates.Count;
        for (int i = 0; i < n; i++)
        {
            var row = new Dictionary<string, object> { ["Row"] = candidates[i] };
            for (int j = 0; j < n; j++)
            {
                int val = 0;
                if (i < data.Count && data[i] is not null && j < data[i].Count)
                    val = data[i][j];
                row[candidates[j]] = val;
            }
            target.Add(row);
        }
    }

    private static void FillPercentFromN(
        ObservableCollection<Dictionary<string, object>> target,
        List<string> candidates,
        List<List<int>> nMat)
    {
        int n = candidates.Count;
        for (int i = 0; i < n; i++)
        {
            var row = new Dictionary<string, object> { ["Row"] = candidates[i] };
            for (int j = 0; j < n; j++)
            {
                if (i == j)
                {
                    row[candidates[j]] = "—";
                    continue;
                }

                int nij = SafeGet(nMat, i, j);
                int nji = SafeGet(nMat, j, i);
                int denom = nij + nji;

                if (denom == 0)
                    row[candidates[j]] = "0.0%";
                else
                {
                    double pct = (double)nij / denom * 100.0;
                    row[candidates[j]] = $"{pct:0.0}%";
                }
            }
            target.Add(row);
        }
    }

    private static int SafeGet(List<List<int>> m, int i, int j)
    {
        if (i < 0 || j < 0) return 0;
        if (i >= m.Count) return 0;
        if (m[i] is null) return 0;
        if (j >= m[i].Count) return 0;
        return m[i][j];
    }

    // Schulze strongest paths (margin as strength)
    private static List<List<int>> ComputeSchulzePaths(List<List<int>> margin)
    {
        int n = margin.Count;
        var p = new int[n][];
        for (int i = 0; i < n; i++)
        {
            p[i] = new int[n];
            for (int j = 0; j < n; j++)
            {
                if (i == j) { p[i][j] = 0; continue; }
                int mij = (i < margin.Count && j < margin[i].Count) ? margin[i][j] : 0;
                p[i][j] = mij > 0 ? mij : 0;
            }
        }

        for (int k = 0; k < n; k++)
        {
            for (int i = 0; i < n; i++)
            {
                if (i == k) continue;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || j == k) continue;
                    int via = Math.Min(p[i][k], p[k][j]);
                    if (via > p[i][j]) p[i][j] = via;
                }
            }
        }

        var res = new List<List<int>>(n);
        for (int i = 0; i < n; i++)
            res.Add(p[i].ToList());
        return res;
    }

    // Ranked Pairs: lock edges in descending (margin, then N) order, skipping edges that create a cycle
    private static List<RankedPairRow> ComputeRankedPairs(
        List<string> cands,
        List<List<int>> margin,
        List<List<int>> nMat)
    {
        int n = cands.Count;

        var edges = new List<(int w, int l, int m, int nij)>();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                int mij = (i < margin.Count && j < margin[i].Count) ? margin[i][j] : 0;
                if (mij > 0)
                {
                    int nij = SafeGet(nMat, i, j);
                    edges.Add((i, j, mij, nij));
                }
            }
        }

        var sorted = edges
            .OrderByDescending(e => e.m)
            .ThenByDescending(e => e.nij)
            .ThenBy(e => cands[e.w])
            .ThenBy(e => cands[e.l])
            .ToList();

        var locked = new List<int>[n];
        for (int i = 0; i < n; i++) locked[i] = new List<int>();

        bool CreatesCycle(int from, int to)
        {
            // if there's already a path to -> from, adding from->to creates a cycle
            var seen = new bool[n];
            var st = new Stack<int>();
            st.Push(to);

            while (st.Count > 0)
            {
                int cur = st.Pop();
                if (cur == from) return true;
                if (seen[cur]) continue;
                seen[cur] = true;
                foreach (var nx in locked[cur])
                    if (!seen[nx]) st.Push(nx);
            }
            return false;
        }

        var result = new List<RankedPairRow>();
        foreach (var e in sorted)
        {
            if (!CreatesCycle(e.w, e.l))
            {
                locked[e.w].Add(e.l);
                result.Add(new RankedPairRow(
                    Winner: cands[e.w],
                    Loser: cands[e.l],
                    Strength: e.nij,
                    Margin: e.m
                ));
            }
        }

        return result;
    }

    private static Bitmap? DecodeBase64Png(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64)) return null;
        var bytes = Convert.FromBase64String(b64);
        var ms = new MemoryStream(bytes);
        return new Bitmap(ms);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record RowKV(string Method, string Winner);
public sealed record RowScore(string Candidate, int Score);
public sealed record TraceRow(int Index, string Action, string U, string V, string Message, string Stack);

public sealed record RankedPairRow(string Winner, string Loser, int Strength, int Margin)
{
    public string Arrow => "→";
}
