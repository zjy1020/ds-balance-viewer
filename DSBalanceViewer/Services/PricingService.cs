using DSBalanceViewer.Models;

namespace DSBalanceViewer.Services;

public class PricingService
{
    // 价格单位: 元/百万 token（DeepSeek 官方定价，2025 年）
    // 输入价格 / 输出价格（用于 API 不返回 cost 时的后备估算）
    private readonly Dictionary<string, (decimal Input, decimal Output)> _pricing = new()
    {
        ["deepseek-chat"] = (1.00m, 2.00m),
        ["deepseek-reasoner"] = (4.00m, 16.00m),
    };

    /// <summary>从 API 返回的 daily 数据汇总总费用（API 已计算好 cost）</summary>
    public decimal TotalCost(IEnumerable<UsagePoint> daily)
    {
        return Math.Round(daily.Sum(d => d.Cost), 4);
    }

    /// <summary>从 API 返回的 daily 数据汇总总 Token</summary>
    public long TotalTokens(IEnumerable<UsagePoint> daily)
    {
        return daily.Sum(d => d.Tokens);
    }

    /// <summary>从 API 返回的 daily 数据汇总总调用次数</summary>
    public long TotalCalls(IEnumerable<UsagePoint> daily)
    {
        return daily.Sum(d => d.Calls);
    }

    /// <summary>按模型汇总费用</summary>
    public Dictionary<string, decimal> CostByModel(IEnumerable<ModelUsage> byModel)
    {
        return byModel.ToDictionary(m => m.Model, m => m.Cost);
    }
}
