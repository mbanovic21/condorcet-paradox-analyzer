using CondorcetAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CondorcetAvalonia.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        ElectionInput input,
        bool saveArtifact = false,
        string tag = "run",
        string notes = "",
        bool includeTrace = false
    )
    {
        var query = new List<string>();

        if (saveArtifact)
        {
            query.Add("save_artifact=true");
            query.Add($"tag={Uri.EscapeDataString(tag ?? "run")}");
            query.Add($"notes={Uri.EscapeDataString(notes ?? "")}");
        }

        if (includeTrace)
            query.Add("include_trace=true");

        var url = "/election/analyze";
        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        var resp = await _http.PostAsJsonAsync(url, input);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<AnalysisResult>();
        if (result is null) throw new InvalidOperationException("Empty response from server.");
        return result;
    }
}
