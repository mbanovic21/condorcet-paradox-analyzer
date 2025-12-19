using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CondorcetWpf.Models;

// Input
public sealed class Ballot
{
    [JsonPropertyName("ranking")]
    public List<string> Ranking { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
}

public sealed class ElectionInput
{
    [JsonPropertyName("candidates")]
    public List<string> Candidates { get; set; } = new();

    [JsonPropertyName("ballots")]
    public List<Ballot> Ballots { get; set; } = new();
}

// Output
public sealed class PairwiseResult
{
    [JsonPropertyName("candidates")]
    public List<string> Candidates { get; set; } = new();

    [JsonPropertyName("N")]
    public List<List<int>> N { get; set; } = new();

    [JsonPropertyName("A")]
    public List<List<int>> A { get; set; } = new();
}

public sealed class CycleResult
{
    [JsonPropertyName("has_cycle")]
    public bool HasCycle { get; set; }

    [JsonPropertyName("cycle")]
    public List<string>? Cycle { get; set; }
}

public sealed class MethodScores
{
    [JsonPropertyName("borda")]
    public Dictionary<string, int> Borda { get; set; } = new();

    [JsonPropertyName("plurality")]
    public Dictionary<string, int> Plurality { get; set; } = new();
}

public sealed class AnalysisResult
{
    [JsonPropertyName("candidates")]
    public List<string> Candidates { get; set; } = new();

    [JsonPropertyName("pairwise")]
    public PairwiseResult Pairwise { get; set; } = new();

    [JsonPropertyName("condorcet_winner")]
    public string? CondorcetWinner { get; set; }

    [JsonPropertyName("cycle_info")]
    public CycleResult CycleInfo { get; set; } = new();

    [JsonPropertyName("scores")]
    public MethodScores Scores { get; set; } = new();

    [JsonPropertyName("winners")]
    public Dictionary<string, string?> Winners { get; set; } = new();

    [JsonPropertyName("graph_png_base64")]
    public string? GraphPngBase64 { get; set; }
}
