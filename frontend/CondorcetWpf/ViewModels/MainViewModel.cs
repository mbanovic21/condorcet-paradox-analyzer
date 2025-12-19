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

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _api = new ApiClient("http://127.0.0.1:8000");
    private readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private ElectionInput? _input;
    private AnalysisResult? _result;

    public RelayCommand LoadJsonCommand { get; }
    public RelayCommand AnalyzeCommand { get; }

    public MainViewModel()
    {
        LoadJsonCommand = new RelayCommand(LoadJson);
        AnalyzeCommand = new RelayCommand(async () => await AnalyzeAsync(), () => HasInput);

        Status = "Load a JSON file, then click Analyze (backend must be running).";
        SummaryLine1 = "No analysis yet.";
        SummaryLine2 = "";
        CycleText = "";
    }

    public bool HasInput => _input is not null;

    private string _status = "";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _summary1 = "";
    public string SummaryLine1
    {
        get => _summary1;
        set { _summary1 = value; OnPropertyChanged(); }
    }

    private string _summary2 = "";
    public string SummaryLine2
    {
        get => _summary2;
        set { _summary2 = value; OnPropertyChanged(); }
    }

    private string _cycleText = "";
    public string CycleText
    {
        get => _cycleText;
        set { _cycleText = value; OnPropertyChanged(); }
    }

    private BitmapImage? _graphImage;
    public BitmapImage? GraphImage
    {
        get => _graphImage;
        set { _graphImage = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RowKV> WinnersRows { get; } = new();
    public ObservableCollection<RowScore> BordaRows { get; } = new();
    public ObservableCollection<RowScore> PluralityRows { get; } = new();

    // For matrix: use dynamic rows where each row is a dictionary
    public ObservableCollection<Dictionary<string, object>> MatrixRows { get; } = new();

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
            Status = $"Loaded: {Path.GetFileName(dlg.FileName)} (candidates: {string.Join(", ", _input.Candidates)})";
            SummaryLine1 = "Ready to analyze.";
            SummaryLine2 = "";
            CycleText = "";

            WinnersRows.Clear();
            BordaRows.Clear();
            PluralityRows.Clear();
            MatrixRows.Clear();
            GraphImage = null;

            OnPropertyChanged(nameof(HasInput));
            AnalyzeCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AnalyzeAsync()
    {
        if (_input is null) return;

        try
        {
            Status = "Analyzing...";
            _result = await _api.AnalyzeAsync(_input);
            Status = "Done.";

            // Summary
            if (!string.IsNullOrWhiteSpace(_result.CondorcetWinner))
                SummaryLine1 = $"Condorcet winner: {_result.CondorcetWinner}";
            else
                SummaryLine1 = "No Condorcet winner.";

            SummaryLine2 = _result.CycleInfo.HasCycle ? "Condorcet paradox detected (directed cycle exists)." : "No directed cycle detected.";

            if (_result.CycleInfo.HasCycle && _result.CycleInfo.Cycle is not null)
                CycleText = "Cycle: " + string.Join(" → ", _result.CycleInfo.Cycle);
            else
                CycleText = "";

            // Winners table
            WinnersRows.Clear();
            foreach (var kv in _result.Winners.OrderBy(k => k.Key))
                WinnersRows.Add(new RowKV(kv.Key, kv.Value ?? "(tie/none)"));

            // Scores
            BordaRows.Clear();
            foreach (var kv in _result.Scores.Borda.OrderByDescending(k => k.Value))
                BordaRows.Add(new RowScore(kv.Key, kv.Value));

            PluralityRows.Clear();
            foreach (var kv in _result.Scores.Plurality.OrderByDescending(k => k.Value))
                PluralityRows.Add(new RowScore(kv.Key, kv.Value));

            // Matrix A
            MatrixRows.Clear();
            var cands = _result.Pairwise.Candidates;
            for (int i = 0; i < cands.Count; i++)
            {
                var row = new Dictionary<string, object>();
                row["Row"] = cands[i];
                for (int j = 0; j < cands.Count; j++)
                    row[cands[j]] = _result.Pairwise.A[i][j];
                MatrixRows.Add(row);
            }

            // Graph image
            GraphImage = DecodeBase64Png(_result.GraphPngBase64);
        }
        catch (Exception ex)
        {
            Status = "Error.";
            MessageBox.Show(ex.Message, "Analyze error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
