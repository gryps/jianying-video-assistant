namespace JianyingVideoAssistant.Services;

public sealed class MediaCategoryClassifier
{
    private static readonly (string Category, string[] Keywords)[] Rules =
    [
        ("口播", ["口播", "解说", "讲解", "talking", "speech", "voice", "intro"]),
        ("人物", ["人物", "模特", "真人", "人像", "试穿", "model", "person", "portrait", "tryon"]),
        ("商品", ["商品", "产品", "宝贝", "包装", "主图", "product", "item", "goods", "package"]),
        ("场景", ["场景", "环境", "街景", "室内", "室外", "风景", "scene", "location", "indoor", "outdoor"])
    ];

    public string Suggest(string filePath)
    {
        var searchableText = Path.ChangeExtension(filePath, null).ToLowerInvariant();

        foreach (var (category, keywords) in Rules)
        {
            if (keywords.Any(searchableText.Contains))
            {
                return category;
            }
        }

        return "其他";
    }
}
