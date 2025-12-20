using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CondorcetWpf.Models;

namespace CondorcetWpf.Services;

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
    string notes = ""
)
    {
        var url = "/election/analyze";

        if (saveArtifact)
        {
            // Query param names MUST match FastAPI: save_artifact, tag, notes
            url += $"?save_artifact=true&tag={Uri.EscapeDataString(tag)}&notes={Uri.EscapeDataString(notes)}";
        }

        var resp = await _http.PostAsJsonAsync(url, input);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<AnalysisResult>();
        if (result is null) throw new InvalidOperationException("Empty response from server.");
        return result;
    }
}
