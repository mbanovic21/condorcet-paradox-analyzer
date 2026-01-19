using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using CondorcetAvalonia.ViewModels;
using DG = Avalonia.Controls.DataGrid;

namespace CondorcetAvalonia.Views;

public partial class AnalyzeView : UserControl
{
    private AnalyzeViewModel? _vm;

    public AnalyzeView()
    {
        InitializeComponent();

        DataContextChanged += (_, __) =>
        {
            Unwire();
            _vm = DataContext as AnalyzeViewModel;
            Wire();
            RebuildAllMatrixColumns();
        };

        AttachedToVisualTree += (_, __) =>
        {
            if (_vm is null)
            {
                _vm = DataContext as AnalyzeViewModel;
                Wire();
            }
            RebuildAllMatrixColumns();
        };
    }

    private void Wire()
    {
        if (_vm is null) return;

        _vm.MatrixVotesRows.CollectionChanged += OnAnyMatrixChanged;
        _vm.MatrixARows.CollectionChanged += OnAnyMatrixChanged;
        _vm.MatrixMarginRows.CollectionChanged += OnAnyMatrixChanged;
        _vm.MatrixSchulzeRows.CollectionChanged += OnAnyMatrixChanged;
        _vm.MatrixPercentRows.CollectionChanged += OnAnyMatrixChanged;
    }

    private void Unwire()
    {
        if (_vm is null) return;

        _vm.MatrixVotesRows.CollectionChanged -= OnAnyMatrixChanged;
        _vm.MatrixARows.CollectionChanged -= OnAnyMatrixChanged;
        _vm.MatrixMarginRows.CollectionChanged -= OnAnyMatrixChanged;
        _vm.MatrixSchulzeRows.CollectionChanged -= OnAnyMatrixChanged;
        _vm.MatrixPercentRows.CollectionChanged -= OnAnyMatrixChanged;
    }

    private void OnAnyMatrixChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Analyze clears and refills collections; when first row appears we can rebuild columns
        RebuildAllMatrixColumns();
    }

    private void RebuildAllMatrixColumns()
    {
        if (_vm is null) return;

        // Best candidate order: from Pairwise matrix rows themselves (they are filled using result.Pairwise.Candidates in VM)
        var cands =
            _vm.MatrixVotesRows.FirstOrDefault()?.Keys.Where(k => k != "Row").ToList()
            ?? _vm.MatrixARows.FirstOrDefault()?.Keys.Where(k => k != "Row").ToList()
            ?? _vm.MatrixMarginRows.FirstOrDefault()?.Keys.Where(k => k != "Row").ToList()
            ?? _vm.MatrixPercentRows.FirstOrDefault()?.Keys.Where(k => k != "Row").ToList()
            ?? _vm.MatrixSchulzeRows.FirstOrDefault()?.Keys.Where(k => k != "Row").ToList();

        // IMPORTANT: Dictionary keys order isn't guaranteed -> derive correct order from first row values by using "Row" progression:
        // We rebuild using the actual row labels in MatrixVotesRows if available (Row column values are in candidate order).
        var rowOrder = _vm.MatrixVotesRows.Select(r => r.TryGetValue("Row", out var v) ? v?.ToString() : null)
                                          .Where(s => !string.IsNullOrWhiteSpace(s))
                                          .ToList();

        if (rowOrder.Count > 0)
            cands = rowOrder;

        RebuildMatrixGrid(VotesGrid, cands);
        RebuildMatrixGrid(AGrid, cands);
        RebuildMatrixGrid(MarginGrid, cands);
        RebuildMatrixGrid(SchulzeGrid, cands);
        RebuildMatrixGrid(PercentGrid, cands);
    }

    private static void RebuildMatrixGrid(DG grid, System.Collections.Generic.List<string>? candidates)
    {
        grid.Columns.Clear();

        // Row label column
        grid.Columns.Add(new Avalonia.Controls.DataGridTextColumn
        {
            Header = "",
            Binding = new Binding("[Row]"),
            Width = new Avalonia.Controls.DataGridLength(160)
        });

        if (candidates is not null)
        {
            foreach (var c in candidates)
            {
                grid.Columns.Add(new Avalonia.Controls.DataGridTextColumn
                {
                    Header = c,
                    Binding = new Binding($"[{c}]"),
                    Width = Avalonia.Controls.DataGridLength.Auto
                });
            }
        }

        grid.IsReadOnly = true;
        grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.All;
        grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All;
    }
}
