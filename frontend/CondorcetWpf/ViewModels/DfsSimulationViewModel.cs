using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CondorcetWpf.Models;

namespace CondorcetWpf.ViewModels;

public sealed class DfsSimulationViewModel : INotifyPropertyChanged
{
    private readonly AnalyzeViewModel _analyze;
    private readonly DispatcherTimer _timer;

    public const double CanvasW = 860;
    public const double CanvasH = 560;

    public ObservableCollection<NodeVm> Nodes { get; } = new();
    public ObservableCollection<EdgeVm> Edges { get; } = new();

    private List<DfsStep> _steps = new();

    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Header));
            OnPropertyChanged(nameof(CurrentStep));
            OnPropertyChanged(nameof(StackText));
            OnPropertyChanged(nameof(MessageText));

            ApplyStep();
            RaiseAllCanExec();
        }
    }

    public bool HasTrace => _steps.Count > 0;
    public string Header => HasTrace ? $"Step {Index} / {_steps.Count - 1}" : "No DFS trace loaded";

    public DfsStep? CurrentStep => HasTrace ? _steps[Index] : null;
    public string StackText => CurrentStep is null ? "" : string.Join(" → ", CurrentStep.Stack);
    public string MessageText => CurrentStep?.Message ?? "";

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            _isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayPauseText));
        }
    }
    public string PlayPauseText => IsPlaying ? "Pause" : "Play";

    private int _speedMs = 450;
    public int SpeedMs
    {
        get => _speedMs;
        set
        {
            _speedMs = Math.Max(50, value);
            OnPropertyChanged();
            _timer.Interval = TimeSpan.FromMilliseconds(_speedMs);
        }
    }

    public RelayCommand LoadFromLastAnalysisCommand { get; }
    public RelayCommand PrevCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand PlayPauseCommand { get; }

    public DfsSimulationViewModel(AnalyzeViewModel analyze)
    {
        _analyze = analyze;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SpeedMs) };
        _timer.Tick += (_, __) =>
        {
            if (!HasTrace) return;
            if (Index < _steps.Count - 1) Index++;
            else TogglePlay(false);
        };

        LoadFromLastAnalysisCommand = new RelayCommand(LoadFromLastAnalysis);

        PrevCommand = new RelayCommand(() => Index = Math.Max(0, Index - 1), () => HasTrace && Index > 0);
        NextCommand = new RelayCommand(() => Index = Math.Min(_steps.Count - 1, Index + 1), () => HasTrace && Index < _steps.Count - 1);
        ResetCommand = new RelayCommand(() => Index = 0, () => HasTrace);
        PlayPauseCommand = new RelayCommand(() => TogglePlay(!IsPlaying), () => HasTrace);

        LoadFromLastAnalysis();
    }

    private void TogglePlay(bool play)
    {
        IsPlaying = play;
        if (play) _timer.Start();
        else _timer.Stop();
    }

    private void LoadFromLastAnalysis()
    {
        TogglePlay(false);

        var trace = _analyze.LastDfsTrace;
        var layout = _analyze.GraphLayout;

        _steps = trace?.Steps ?? new List<DfsStep>();

        BuildGraph(layout);

        Index = 0;

        OnPropertyChanged(nameof(HasTrace));
        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(StackText));
        OnPropertyChanged(nameof(MessageText));

        ApplyStep();
        RaiseAllCanExec();
    }

    private void BuildGraph(Dictionary<string, List<double>>? layout)
    {
        Nodes.Clear();
        Edges.Clear();

        var candidates = _analyze.MatrixARows.Select(r => r["Row"].ToString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        if (candidates.Count == 0 && layout != null)
            candidates = layout.Keys.ToList();

        if (candidates.Count == 0)
            return;

        var normPos = new Dictionary<string, (double x, double y)>();
        if (layout != null && layout.Count > 0)
        {
            foreach (var c in candidates)
            {
                if (layout.TryGetValue(c, out var xy) && xy.Count >= 2)
                    normPos[c] = (Clamp01(xy[0]), Clamp01(xy[1]));
            }
        }

        if (normPos.Count != candidates.Count)
        {
            var n = candidates.Count;
            for (int i = 0; i < n; i++)
            {
                var ang = 2.0 * Math.PI * i / n;
                var x = 0.5 + 0.38 * Math.Cos(ang);
                var y = 0.5 + 0.38 * Math.Sin(ang);
                normPos[candidates[i]] = (x, y);
            }
        }

        const double pad = 60.0;
        double minX = pad, maxX = CanvasW - pad;
        double minY = pad, maxY = CanvasH - pad;

        var px = new Dictionary<string, (double x, double y)>();
        foreach (var c in candidates)
        {
            var (nx, ny) = normPos[c];
            var x = minX + nx * (maxX - minX);
            var y = minY + ny * (maxY - minY);
            px[c] = (x, y);
        }

        foreach (var c in candidates)
        {
            var (x, y) = px[c];
            Nodes.Add(new NodeVm(id: c, x: x, y: y));
        }

        const double nodeRadius = 28.0;

        if (_analyze.MatrixARows.Count > 0)
        {
            foreach (var row in _analyze.MatrixARows)
            {
                var from = row["Row"].ToString()!;
                foreach (var to in candidates)
                {
                    if (!row.ContainsKey(to)) continue;
                    var val = Convert.ToInt32(row[to]);
                    if (val != 1) continue;

                    if (!px.ContainsKey(from) || !px.ContainsKey(to)) continue;

                    var (x1, y1) = px[from];
                    var (x2, y2) = px[to];

                    var (tx1, ty1, tx2, ty2) = TrimLine(x1, y1, x2, y2, nodeRadius);

                    var arrow = ArrowHead(tx1, ty1, tx2, ty2, headLen: 14, headWidth: 10);

                    var edge = new EdgeVm(from, to)
                    {
                        X1 = tx1,
                        Y1 = ty1,
                        X2 = tx2,
                        Y2 = ty2,
                    };

                    edge.ArrowPoints = new PointCollection
                    {
                        new Point(tx2, ty2),
                        new Point(arrow.x3, arrow.y3),
                        new Point(arrow.x4, arrow.y4),
                    };

                    Edges.Add(edge);
                }
            }
        }
    }

    private void ApplyStep()
    {
        foreach (var n in Nodes)
        {
            n.State = "WHITE";
            n.IsInStack = false;
        }
        foreach (var e in Edges)
        {
            e.IsActive = false;
            e.IsCycle = false;
        }

        var step = CurrentStep;
        if (step == null) return;

        foreach (var kv in step.Colors)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == kv.Key);
            if (node != null) node.State = kv.Value;
        }

        foreach (var s in step.Stack)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == s);
            if (node != null) node.IsInStack = true;
        }

        if (!string.IsNullOrWhiteSpace(step.U) && !string.IsNullOrWhiteSpace(step.V))
        {
            var edge = Edges.FirstOrDefault(ed => ed.From == step.U && ed.To == step.V);
            if (edge != null) edge.IsActive = true;
        }

        if (step.Cycle != null && step.Cycle.Count >= 2)
        {
            for (int i = 0; i < step.Cycle.Count - 1; i++)
            {
                var a = step.Cycle[i];
                var b = step.Cycle[i + 1];
                var edge = Edges.FirstOrDefault(ed => ed.From == a && ed.To == b);
                if (edge != null) edge.IsCycle = true;
            }
        }
    }

    private void RaiseAllCanExec()
    {
        PrevCommand.RaiseCanExecuteChanged();
        NextCommand.RaiseCanExecuteChanged();
        ResetCommand.RaiseCanExecuteChanged();
        PlayPauseCommand.RaiseCanExecuteChanged();
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static (double x1, double y1, double x2, double y2) TrimLine(double x1, double y1, double x2, double y2, double r)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1e-6) return (x1, y1, x2, y2);

        var ux = dx / dist;
        var uy = dy / dist;

        var tx1 = x1 + ux * r;
        var ty1 = y1 + uy * r;
        var tx2 = x2 - ux * r;
        var ty2 = y2 - uy * r;

        return (tx1, ty1, tx2, ty2);
    }

    private static (double x3, double y3, double x4, double y4) ArrowHead(double x1, double y1, double x2, double y2, double headLen, double headWidth)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1e-6) return (x2, y2, x2, y2);

        var ux = dx / dist;
        var uy = dy / dist;

        var px = -uy;
        var py = ux;

        var bx = x2 - ux * headLen;
        var by = y2 - uy * headLen;

        var x3 = bx + px * (headWidth / 2.0);
        var y3 = by + py * (headWidth / 2.0);
        var x4 = bx - px * (headWidth / 2.0);
        var y4 = by - py * (headWidth / 2.0);

        return (x3, y3, x4, y4);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class NodeVm : INotifyPropertyChanged
{
    public string Id { get; }
    public double X { get; }
    public double Y { get; }

    private string _state = "WHITE";
    public string State { get => _state; set { _state = value; OnPropertyChanged(); } }

    private bool _isInStack;
    public bool IsInStack { get => _isInStack; set { _isInStack = value; OnPropertyChanged(); } }

    public NodeVm(string id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class EdgeVm : INotifyPropertyChanged
{
    public string From { get; }
    public string To { get; }

    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    private PointCollection _arrowPoints = new();
    public PointCollection ArrowPoints
    {
        get => _arrowPoints;
        set { _arrowPoints = value; OnPropertyChanged(); }
    }

    private bool _isActive;
    public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); } }

    private bool _isCycle;
    public bool IsCycle { get => _isCycle; set { _isCycle = value; OnPropertyChanged(); } }

    public EdgeVm(string from, string to)
    {
        From = from;
        To = to;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
