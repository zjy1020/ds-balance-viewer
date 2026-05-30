using System.Net.Http.Headers;
using System.Text.Json;
using DSBalanceViewer.Models;

namespace DSBalanceViewer.Services;

public class DeepSeekApiService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.deepseek.com";

    public DeepSeekApiService(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<BalanceResponse> GetBalanceAsync()
    {
        var response = await _http.GetAsync($"{BaseUrl}/user/balance");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BalanceResponse>(json)
               ?? throw new InvalidOperationException("Failed to parse balance response");
    }

    public async Task<UsageResponse> GetUsageAsync()
    {
        var response = await _http.GetAsync($"{BaseUrl}/billing/usage");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new UsageResponse(); // 404 = 该账户暂无用量数据
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UsageResponse>(json)
               ?? new UsageResponse();
    }

    public void Dispose() => _http.Dispose();
}
