using JianyingVideoAssistant.Infrastructure;

namespace JianyingVideoAssistant.Models;

public sealed class MediaAsset : ObservableObject
{
    private string _category;
    private string _categorySourceLabel = "自动建议";

    public MediaAsset(string fullPath, string name, string detail, string category, string categorySourceLabel = "自动建议")
    {
        FullPath = fullPath;
        Name = name;
        Detail = detail;
        _category = category;
        _categorySourceLabel = categorySourceLabel;
    }

    public string FullPath { get; }
    public string Name { get; }
    public string Detail { get; }
    public IReadOnlyList<string> CategoryOptions => AssetCategories.All;

    public string Category
    {
        get => _category;
        set
        {
            if (SetProperty(ref _category, value))
            {
                CategorySourceLabel = "已手动调整";
            }
        }
    }

    public string CategorySourceLabel
    {
        get => _categorySourceLabel;
        private set => SetProperty(ref _categorySourceLabel, value);
    }
}

public static class AssetCategories
{
    public static IReadOnlyList<string> All { get; } = ["商品", "人物", "场景", "口播", "其他"];
}
