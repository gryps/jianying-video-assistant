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
Directory.CreateDirectory(fixturePath);

try
{
    var productPath = Path.Combine(fixturePath, "商品");
    var personPath = Path.Combine(fixturePath, "人物");
    Directory.CreateDirectory(productPath);
    Directory.CreateDirectory(personPath);
    var productVideo = Path.Combine(productPath, "front.mp4");
    var personImage = Path.Combine(personPath, "model.jpg");
    var musicPath = Path.Combine(fixturePath, "轻快-happy.mp3");
    await File.WriteAllBytesAsync(productVideo, new byte[2048]);
    await File.WriteAllBytesAsync(personImage, new byte[1024]);
    await File.WriteAllBytesAsync(musicPath, new byte[512]);
    await File.WriteAllTextAsync(Path.Combine(fixturePath, "notes.txt"), "不应导入");

    var assetService = new LocalAssetImportService(classifier);
    var result = await assetService.ImportFolderAsync(fixturePath);
    AssertEqual(2, result.Assets.Count, "只导入支持的图片和视频");
    AssertEqual("人物", result.Assets.Single(asset => asset.Name == "model.jpg").Category, "按目录建议人物分类");
    AssertEqual("商品", result.Assets.Single(asset => asset.Name == "front.mp4").Category, "按目录建议商品分类");
    var correctedAsset = result.Assets[0];
    correctedAsset.Category = "场景";
    AssertEqual("已手动调整", correctedAsset.CategorySourceLabel, "记录人工纠错状态");

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
        () => Task.FromResult<string?>(fixturePath),
        () => Task.FromResult<IReadOnlyList<string>>([musicPath]),
        _ => { });

    AssertEqual(4, viewModel.WorkflowSteps.Count, "首页保持四步任务流");
    AssertEqual(4, viewModel.WorkflowSteps.Count(step => step.IsActionEnabled), "四项功能均可用");
    await ExecuteAsync(viewModel.ImportMediaCommand);
    AssertEqual(2, viewModel.RecentAssets.Count, "素材命令完成真实导入");

    viewModel.OpenContentPanelCommand.Execute(null);
    AssertEqual(true, viewModel.IsContentPanelOpen, "打开文案侧边面板");
    viewModel.ApplySelectedContentCommand.Execute(null);
    AssertEqual(1, viewModel.ProjectScriptSegments.Count, "采用文案加入项目脚本");

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
    AssertEqual(true, viewModel.LastDraftPath is not null, "生成安全预览目录");
    AssertEqual(true, File.Exists(Path.Combine(viewModel.LastDraftPath!, "project-preview.json")), "生成项目预览清单");
    AssertEqual(false, File.Exists(Path.Combine(viewModel.LastDraftPath!, "draft_content.json")), "不生成伪造的剪映格式文件");
    AssertEqual(100d, viewModel.Progress, "四步完成度达到百分之百");

    Console.WriteLine("素材、文案、背景音乐与安全草稿预览核心流程测试通过。");
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
