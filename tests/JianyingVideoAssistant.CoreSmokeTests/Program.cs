using JianyingVideoAssistant.Services;
using JianyingVideoAssistant.ViewModels;

var classifier = new MediaCategoryClassifier();
AssertEqual("商品", classifier.Suggest(@"C:\素材\商品\product_front.mp4"), "商品分类");
AssertEqual("人物", classifier.Suggest(@"C:\素材\model_tryon.jpg"), "人物分类");
AssertEqual("场景", classifier.Suggest(@"C:\素材\outdoor_park.mov"), "场景分类");
AssertEqual("口播", classifier.Suggest(@"C:\素材\主播口播.mp4"), "口播分类");
AssertEqual("其他", classifier.Suggest(@"C:\素材\DSC_0001.png"), "默认分类");

var mainViewModel = new MainViewModel(
    new LocalAssetImportService(classifier),
    () => Task.FromResult<string?>(null));
AssertEqual(4, mainViewModel.WorkflowSteps.Count, "首页保持四步任务流");
AssertEqual(1, mainViewModel.WorkflowSteps.Count(step => step.IsActionEnabled), "首页只启用真实可用功能");
AssertEqual("选择素材文件夹", mainViewModel.WorkflowSteps[0].ActionLabel, "素材入口使用明确动作名称");
AssertEqual(false, mainViewModel.WorkflowSteps.Any(step => step.ActionLabel is "找一句开头" or "查看推荐"), "移除模糊演示按钮");

var fixturePath = Path.Combine(Path.GetTempPath(), $"jva-asset-import-{Guid.NewGuid():N}");

try
{
    var productPath = Path.Combine(fixturePath, "商品");
    var personPath = Path.Combine(fixturePath, "人物");
    Directory.CreateDirectory(productPath);
    Directory.CreateDirectory(personPath);
    await File.WriteAllBytesAsync(Path.Combine(productPath, "front.mp4"), new byte[2048]);
    await File.WriteAllBytesAsync(Path.Combine(personPath, "model.jpg"), new byte[1024]);
    await File.WriteAllTextAsync(Path.Combine(fixturePath, "notes.txt"), "不应导入");

    var service = new LocalAssetImportService(classifier);
    var result = await service.ImportFolderAsync(fixturePath);

    AssertEqual(2, result.Assets.Count, "只导入支持的媒体格式");
    AssertEqual("人物", result.Assets.Single(asset => asset.Name == "model.jpg").Category, "按目录建议人物分类");
    AssertEqual("商品", result.Assets.Single(asset => asset.Name == "front.mp4").Category, "按目录建议商品分类");

    var correctedAsset = result.Assets[0];
    correctedAsset.Category = "场景";
    AssertEqual("已手动调整", correctedAsset.CategorySourceLabel, "记录人工纠错状态");

    Console.WriteLine("首页任务状态、素材扫描、格式过滤、自动分类和人工纠错冒烟测试通过。");
}
finally
{
    if (Directory.Exists(fixturePath))
    {
        Directory.Delete(fixturePath, recursive: true);
    }
}

static void AssertEqual<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{scenario}失败：期望 {expected}，实际 {actual}");
    }
}
