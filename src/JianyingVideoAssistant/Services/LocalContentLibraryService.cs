using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public sealed class LocalContentLibraryService : IContentLibraryService
{
    private static readonly IReadOnlyList<ContentSnippet> Snippets =
    [
        new("opening-problem", "开头", "还在为每天搭配什么而纠结吗？这套方案把选择时间直接省下来。", ["痛点", "穿搭", "省时"]),
        new("opening-result", "开头", "先看效果：不用复杂步骤，三十秒就能完成一次清爽升级。", ["效果", "快速", "演示"]),
        new("opening-curiosity", "开头", "为什么同样的预算，有人买到的体验就是更好？关键就在这个细节。", ["悬念", "对比", "细节"]),
        new("selling-comfort", "卖点", "轻盈材质贴合但不紧绷，长时间使用也能保持舒适。", ["材质", "舒适", "体验"]),
        new("selling-detail", "卖点", "边缘和接口都做了加固处理，日常反复使用也不容易松动。", ["工艺", "耐用", "细节"]),
        new("selling-value", "卖点", "常用功能一次配齐，不需要再为零散配件重复花钱。", ["性价比", "套装", "省钱"]),
        new("transition-scene", "转场", "换到真实使用场景里，再看看它的表现。", ["场景", "实测", "过渡"]),
        new("transition-detail", "转场", "接下来把镜头拉近，这几个细节更值得关注。", ["特写", "细节", "镜头"]),
        new("transition-compare", "转场", "和普通方案放在一起对比，差别会更加直观。", ["对比", "效果", "过渡"]),
        new("ending-action", "结尾", "想省时间又不想降低体验，现在就把它加入你的日常清单。", ["行动", "转化", "清单"]),
        new("ending-comment", "结尾", "你最在意哪一个细节？留在评论区，我帮你继续实测。", ["互动", "评论", "实测"]),
        new("ending-save", "结尾", "先收藏这条，需要的时候照着选，就不容易踩坑。", ["收藏", "避坑", "行动"])
    ];

    public IReadOnlyList<ContentSnippet> Search(string? query, string? purpose)
    {
        var normalizedQuery = query?.Trim();
        var normalizedPurpose = purpose?.Trim();

        return Snippets
            .Where(snippet => string.IsNullOrWhiteSpace(normalizedPurpose)
                || normalizedPurpose == "全部"
                || snippet.Purpose == normalizedPurpose)
            .Where(snippet => string.IsNullOrWhiteSpace(normalizedQuery)
                || snippet.Text.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || snippet.Keywords.Any(keyword => keyword.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
