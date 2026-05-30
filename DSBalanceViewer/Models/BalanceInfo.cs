using System.Text.Json.Serialization;

namespace DSBalanceViewer.Models;

public class BalanceResponse
{
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("balance_infos")]
    public List<BalanceInfo> BalanceInfos { get; set; } = new();
}

public class BalanceInfo
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "CNY";

    [JsonPropertyName("total_balance")]
    public string TotalBalance { get; set; } = "0.00";

    [JsonPropertyName("granted_balance")]
    public string GrantedBalance { get; set; } = "0.00";

    [JsonPropertyName("topped_up_balance")]
    public string ToppedUpBalance { get; set; } = "0.00";
}
