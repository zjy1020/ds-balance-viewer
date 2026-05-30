using DSBalanceViewer.Models;

namespace DSBalanceViewer.Services;

public class PricingService
{
    // 价格单位: 元/百万 token（DeepSeek 官方定价，2025 年）
    // 输入价格 / 输出价格
    private readonly Dictionary<string, (decimal Input, decimal Output)> _pricing = new()
    {
        ["deepseek-chat"] = (1.00m, 2.00m),
        ["deepseek-reasoner"] = (4.00m, 16.00m),
    };

    public decimal EstimateCost(IEnumerable<UsageItem> items)
    {
        decimal total = 0;
        foreach (var item in items)
        {
            var (inputPrice, outputPrice) = GetPrice(item.Model);
            total += (item.PromptTokens / 1_000_000m) * inputPrice;
            total += (item.CompletionTokens / 1_000_000m) * outputPrice;
        }
        return Math.Round(total, 2);
    }

    public (decimal InputPrice, decimal OutputPrice) GetPrice(string model)
    {
        if (_pricing.TryGetValue(model, out var price))
            return price;
        // 未知模型默认按 deepseek-chat 价格
        return _pricing["deepseek-chat"];
    }

    public Dictionary<string, decimal> EstimateCostByDate(IEnumerable<UsageItem> items)
    {
        return items
            .GroupBy(i => i.Date)
            .ToDictionary(g => g.Key, g => EstimateCost(g));
    }

    public Dictionary<string, decimal> EstimateCostByModel(IEnumerable<UsageItem> items)
    {
        return items
            .GroupBy(i => i.Model)
            .ToDictionary(g => g.Key, g => EstimateCost(g));
    }
}
