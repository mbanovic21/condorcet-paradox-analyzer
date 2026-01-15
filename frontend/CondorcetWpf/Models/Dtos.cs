using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CondorcetWpf.Models;

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

public sealed class PairwiseResult
{
    [JsonPropertyName("candidates")]
    public List<string> Candidates { get; set; } = new();

    [JsonPropertyName("A")]
    public List<List<int>> A { get; set; } = new();

    [JsonPropertyName("margin")]
    public List<List<int>> Margin { get; set; } = new();

    [JsonPropertyName("N")]
    public List<List<int>> Votes { get; set; } = new();

    [JsonPropertyName("schulze_paths")]
    public List<List<double>> SchulzePaths { get; set; } = new();

    [JsonPropertyName("percent")]
    public List<List<double>> Percent { get; set; } = new();
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

    [JsonPropertyName("copeland")]
    public Dictionary<string, int> Copeland { get; set; } = new();

    [JsonPropertyName("minimax")]
    public Dictionary<string, int> Minimax { get; set; } = new();
}

public sealed class DfsTrace
{
    [JsonPropertyName("steps")]
    public List<DfsStep> Steps { get; set; } = new();

    [JsonPropertyName("has_cycle")]
    public bool HasCycle { get; set; }

    [JsonPropertyName("cycle")]
    public List<string>? Cycle { get; set; }
}

public sealed class DfsStep
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("u")]
    public string? U { get; set; }

    [JsonPropertyName("v")]
    public string? V { get; set; }

    [JsonPropertyName("stack")]
    public List<string> Stack { get; set; } = new();

    [JsonPropertyName("colors")]
    public Dictionary<string, string> Colors { get; set; } = new();

    [JsonPropertyName("parent")]
    public Dictionary<string, string?> Parent { get; set; } = new();

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("cycle")]
    public List<string>? Cycle { get; set; }
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

    [JsonPropertyName("artifact_run_id")]
    public string? ArtifactRunId { get; set; }
    
    [JsonPropertyName("dfs_trace")]
    public DfsTrace? DfsTrace { get; set; }

    [JsonPropertyName("graph_layout")]
    public Dictionary<string, List<double>>? GraphLayout { get; set; }

    [JsonPropertyName("ranked_pairs_summary")]
    public List<RankedPair> RankedPairsSummary { get; set; } = new();
}

public sealed class RankedPair
{
    [JsonPropertyName("winner")]
    public string Winner { get; set; } = "";

    [JsonPropertyName("loser")]
    public string Loser { get; set; } = "";

    [JsonPropertyName("strength")]
    public int Strength { get; set; }

    [JsonPropertyName("margin")]
    public int Margin { get; set; }
}
