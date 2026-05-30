using System.Text.Json.Serialization;

namespace DSBalanceViewer.Models;

public class UsageResponse
{
    [JsonPropertyName("data")]
    public List<UsageItem> Data { get; set; } = new();
}

public class UsageItem
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("prompt_tokens")]
    public long PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public long CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public long TotalTokens { get; set; }
}
