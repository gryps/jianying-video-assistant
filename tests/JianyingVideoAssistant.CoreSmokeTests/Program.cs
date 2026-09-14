using JianyingVideoAssistant.Adapters;
using JianyingVideoAssistant.Models;
using JianyingVideoAssistant.Services;
using JianyingVideoAssistant.ViewModels;

var classifier = new MediaCategoryClassifier();
AssertEqual("商品", classifier.Suggest(@"C:\素材\商品\product_front.mp4"), "商品分类");
AssertEqual("人物", classifier.Suggest(@"C:\素材\model_tryon.jpg"), "人物分类");
AssertEqual("场景", classifier.Suggest(@"C:\素材\outdoor_park.mov"), "场景分类");
AssertEqual("口播", classifier.Suggest(@"C:\素材\主播口播.mp4"), "口播分类");
AssertEqual("其他", classifier.Suggest(@"C:\素材\DSC_0001.png"), "默认分类");

var fixturePath = Path.Combine(Path.GetTempPath(), $"jva-core-smoke-{Guid.NewGuid():N}");
var draftRoot = Path.Combine(fixturePath, "draft-output");
var projectStatePath = Path.Combine(fixturePath, "state", "current-project.json");
Directory.CreateDirectory(fixturePath);

try
{
    var productPath = Path.Combine(fixturePath, "商品");
    var personPath = Path.Combine(fixturePath, "人物");
    Directory.CreateDirectory(productPath);
    Directory.CreateDirectory(personPath);
    var productVideo = Path.Combine(productPath, "front.mp4");
    var personImage = Path.Combine(personPath, "model.jpg");
    var otherImage = Path.Combine(fixturePath, "DSC_0002.png");
    var musicPath = Path.Combine(fixturePath, "轻快-happy.mp3");
    await File.WriteAllBytesAsync(productVideo, new byte[2048]);
    await File.WriteAllBytesAsync(personImage, new byte[1024]);
    await File.WriteAllBytesAsync(otherImage, new byte[768]);
    await File.WriteAllBytesAsync(musicPath, new byte[512]);
    await File.WriteAllTextAsync(Path.Combine(fixturePath, "notes.txt"), "不应导入");

    var assetService = new LocalAssetImportService(classifier);
    var result = await assetService.ImportFolderAsync(fixturePath);
    AssertEqual(3, result.Assets.Count, "只导入支持的图片和视频");
    AssertEqual("人物", result.Assets.Single(asset => asset.Name == "model.jpg").Category, "按目录建议人物分类");
    AssertEqual("商品", result.Assets.Single(asset => asset.Name == "front.mp4").Category, "按目录建议商品分类");
    var correctedAsset = result.Assets[0];
    correctedAsset.Category = "场景";
    AssertEqual("已手动调整", correctedAsset.CategorySourceLabel, "记录人工纠错状态");
    var droppedResult = await assetService.ImportPathsAsync([productPath, productVideo]);
    AssertEqual(1, droppedResult.Assets.Count, "拖入文件夹与重复文件时按路径去重");

    var contentLibrary = new LocalContentLibraryService();
    AssertEqual(3, contentLibrary.Search(null, "开头").Count, "按用途筛选本地文案");
    AssertEqual(1, contentLibrary.Search("材质", "卖点").Count, "按关键词筛选本地文案");
    AssertEqual(0, contentLibrary.Search("不存在的关键词", "全部").Count, "文案搜索空结果");

    var musicLibrary = new LocalMusicLibraryService();
    var tracks = musicLibrary.ImportFiles([musicPath, Path.Combine(fixturePath, "notes.txt")]);
    AssertEqual(1, tracks.Count, "只导入支持的本地音频");
    AssertEqual("轻快", tracks[0].Mood, "按文件名建议音乐情绪");

    var preview = new FakeMusicPreviewService();
    var viewModel = new MainViewModel(
        assetService,
        contentLibrary,
        musicLibrary,
        preview,
        new JianyingDraftPreviewAdapter(draftRoot),
        new JsonProjectStore(projectStatePath),
        () => Task.FromResult<string?>(fixturePath),
        () => Task.FromResult<IReadOnlyList<string>>([musicPath]),
        _ => { });

    AssertEqual(4, viewModel.WorkflowSteps.Count, "首页保持四步任务流");
    AssertEqual(4, viewModel.WorkflowSteps.Count(step => step.IsActionEnabled), "四项功能均可用");
    await ExecuteAsync(viewModel.ImportMediaCommand);
    AssertEqual(3, viewModel.RecentAssets.Count, "素材命令完成真实导入");
    AssertEqual(Path.GetFileName(fixturePath), viewModel.ProjectName, "未命名项目采用素材文件夹名称");
    viewModel.ProjectName = "秋季新品短视频";
    viewModel.SelectedBulkCategory = "场景";
    viewModel.ApplyBulkCategoryCommand.Execute(null);
    AssertEqual(0, viewModel.RecentAssets.Count(asset => asset.Category == "其他"), "批量归类待确认素材");

    viewModel.OpenContentPanelCommand.Execute(null);
    AssertEqual(true, viewModel.IsContentPanelOpen, "打开文案侧边面板");
    viewModel.ApplySelectedContentCommand.Execute(null);
    AssertEqual(1, viewModel.ProjectScriptSegments.Count, "采用文案加入项目脚本");
    viewModel.CustomContentPurpose = "结尾";
    viewModel.CustomContentText = "这是用户自己输入的结尾文案。";
    viewModel.AddCustomContentCommand.Execute(null);
    AssertEqual(2, viewModel.ProjectScriptSegments.Count, "自定义文案加入脚本");
    viewModel.MoveScriptSegmentUpCommand.Execute(null);
    AssertEqual("这是用户自己输入的结尾文案。", viewModel.ProjectScriptSegments[0].Text, "调整脚本文案顺序");

    viewModel.OpenMusicPanelCommand.Execute(null);
    await ExecuteAsync(viewModel.ImportMusicCommand);
    AssertEqual(1, viewModel.MusicTracks.Count, "音乐命令导入本地音频");
    await ExecuteAsync(viewModel.ToggleMusicPreviewCommand);
    AssertEqual(musicPath, preview.PlayingPath, "试听服务收到选中音乐");
    viewModel.ApplySelectedMusicCommand.Execute(null);
    AssertEqual(musicPath, viewModel.AppliedMusic?.FullPath, "应用背景音乐");
    viewModel.UndoMusicCommand.Execute(null);
    AssertEqual<MusicTrack?>(null, viewModel.AppliedMusic, "撤销背景音乐");
    viewModel.ApplySelectedMusicCommand.Execute(null);

    viewModel.OpenDraftPanelCommand.Execute(null);
    AssertEqual(0, viewModel.DraftBlockingCount, "草稿预检通过");
    await ExecuteAsync(viewModel.GenerateDraftCommand);
    AssertEqual(true, viewModel.LastDraftPath is not null, "生成项目预览目录");
    AssertEqual(true, File.Exists(Path.Combine(viewModel.LastDraftPath!, "project-preview.json")), "生成项目预览清单");
    AssertEqual(false, File.Exists(Path.Combine(viewModel.LastDraftPath!, "draft_content.json")), "不生成伪造的剪映格式文件");
    AssertEqual(100d, viewModel.Progress, "四步完成度达到百分之百");

    var restoredViewModel = new MainViewModel(
        assetService,
        contentLibrary,
        musicLibrary,
        new FakeMusicPreviewService(),
        new JianyingDraftPreviewAdapter(draftRoot),
        new JsonProjectStore(projectStatePath),
        () => Task.FromResult<string?>(null),
        () => Task.FromResult<IReadOnlyList<string>>([]),
        _ => { });
    AssertEqual("秋季新品短视频", restoredViewModel.ProjectName, "恢复项目名称");
    AssertEqual(3, restoredViewModel.RecentAssets.Count, "恢复素材列表");
    AssertEqual("场景", restoredViewModel.RecentAssets[0].Category, "恢复人工素材分类");
    AssertEqual(2, restoredViewModel.ProjectScriptSegments.Count, "恢复项目文案与顺序");
    AssertEqual("这是用户自己输入的结尾文案。", restoredViewModel.ProjectScriptSegments[0].Text, "恢复自定义文案顺序");
    AssertEqual(musicPath, restoredViewModel.AppliedMusic?.FullPath, "恢复背景音乐");
    AssertEqual(true, restoredViewModel.LastDraftPath is not null, "恢复最近项目预览位置");

    File.Delete(personImage);
    File.Delete(musicPath);
    restoredViewModel.RemoveMissingAssetsCommand.Execute(null);
    restoredViewModel.RemoveMissingMusicCommand.Execute(null);
    AssertEqual(2, restoredViewModel.RecentAssets.Count, "清理失效素材路径");
    AssertEqual(0, restoredViewModel.MusicTracks.Count, "清理失效音乐路径");
    AssertEqual<MusicTrack?>(null, restoredViewModel.AppliedMusic, "清理已应用但失效的音乐");

    restoredViewModel.NewProjectCommand.Execute(null);
    var clearedViewModel = new MainViewModel(
        assetService,
        contentLibrary,
        musicLibrary,
        new FakeMusicPreviewService(),
        new JianyingDraftPreviewAdapter(draftRoot),
        new JsonProjectStore(projectStatePath),
        () => Task.FromResult<string?>(null),
        () => Task.FromResult<IReadOnlyList<string>>([]),
        _ => { });
    AssertEqual("未命名视频项目", clearedViewModel.ProjectName, "新建项目清除保存名称");
    AssertEqual(0, clearedViewModel.RecentAssets.Count, "新建项目清除保存素材");

    Console.WriteLine("四模块核心流程与项目自动恢复测试通过。");
}
finally
{
    if (Directory.Exists(fixturePath)) Directory.Delete(fixturePath, recursive: true);
}

static async Task ExecuteAsync(JianyingVideoAssistant.Infrastructure.AsyncRelayCommand command)
{
    command.Execute(null);
    while (!command.CanExecute(null)) await Task.Delay(10);
}

static void AssertEqual<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}失败：期望 {expected}，实际 {actual}");
}

sealed class FakeMusicPreviewService : IMusicPreviewService
{
    public string? PlayingPath { get; private set; }
    public Task PlayAsync(string filePath) { PlayingPath = filePath; return Task.CompletedTask; }
    public void Stop() => PlayingPath = null;
}
