using System.Text.Json.Serialization;

namespace DSBalanceViewer.Models;

public class UsageResponse
{
    [JsonPropertyName("daily")]
    public List<UsagePoint> Daily { get; set; } = new();

    [JsonPropertyName("by_model")]
    public List<ModelUsage> ByModel { get; set; } = new();
}

public class UsagePoint
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("calls")]
    public long Calls { get; set; }

    [JsonPropertyName("tokens")]
    public long Tokens { get; set; }

    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }
}

public class ModelUsage
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("calls")]
    public long Calls { get; set; }

    [JsonPropertyName("tokens")]
    public long Tokens { get; set; }

    [JsonPropertyName("cost")]
    public decimal Cost { get; set; }
}
