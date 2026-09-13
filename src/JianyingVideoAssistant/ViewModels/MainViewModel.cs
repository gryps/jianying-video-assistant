using System.Collections.ObjectModel;
using JianyingVideoAssistant.Infrastructure;
using JianyingVideoAssistant.Models;
using JianyingVideoAssistant.Services;

namespace JianyingVideoAssistant.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IAssetImportService _assetImportService;
    private readonly Func<Task<string?>> _selectMediaFolder;
    private string _projectName = "未命名视频项目";
    private string _statusText = "项目已建立，先导入一个素材文件夹";
    private string _importStatusTitle = "尚未导入素材";
    private string _importStatusDetail = "支持递归扫描常见图片和视频；不会移动、重命名或修改源文件。";
    private double _progress;
    private bool _isImporting;

    public MainViewModel(IAssetImportService assetImportService, Func<Task<string?>> selectMediaFolder)
    {
        _assetImportService = assetImportService;
        _selectMediaFolder = selectMediaFolder;

        NewProjectCommand = new RelayCommand(CreateNewProject);
        ImportMediaCommand = new AsyncRelayCommand(ImportMediaAsync, () => !IsImporting);
        WorkflowSteps =
        [
            new("01", "\uEB9F", "添加素材", "从电脑选择一个文件夹，扫描其中的视频和图片", "导入后会在下方显示分类结果，可随时纠正", "选择素材文件夹", "现在可用", true, ImportMediaCommand),
            new("02", "\uE8A5", "添加视频文案", "将在侧边面板按开头、卖点、转场和结尾展示完整句子", "选中的句子会直接组成当前项目脚本", "添加视频文案", "后续版本", false, null),
            new("03", "\uE8D6", "选择背景音乐", "将在侧边面板列出少量本地音乐，可先试听再应用", "应用后会显示所选音乐和计划使用的时长", "选择背景音乐", "后续版本", false, null),
            new("04", "\uE74E", "导出剪映草稿", "将先检查缺失内容和输出位置，再生成新的草稿副本", "始终创建副本，不会覆盖已有剪映草稿", "预览并导出草稿", "后续版本", false, null)
        ];

        RecentAssets = [];
    }

    public string Greeting => "按下面四步准备视频；目前先从导入素材开始";

    public string ProjectName
    {
        get => _projectName;
        private set => SetProperty(ref _projectName, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ImportStatusTitle
    {
        get => _importStatusTitle;
        private set => SetProperty(ref _importStatusTitle, value);
    }

    public string ImportStatusDetail
    {
        get => _importStatusDetail;
        private set => SetProperty(ref _importStatusDetail, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        private set
        {
            if (SetProperty(ref _isImporting, value))
            {
                OnPropertyChanged(nameof(ImportActionLabel));
                ImportMediaCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ImportActionLabel => IsImporting ? "正在扫描…" : "导入文件夹";

    public double Progress
    {
        get => _progress;
        private set
        {
            if (SetProperty(ref _progress, value))
            {
                OnPropertyChanged(nameof(ProgressLabel));
            }
        }
    }

    public string ProgressLabel => $"{Progress:0}%";

    public ObservableCollection<WorkflowStepViewModel> WorkflowSteps { get; }
    public ObservableCollection<MediaAsset> RecentAssets { get; }

    public RelayCommand NewProjectCommand { get; }
    public AsyncRelayCommand ImportMediaCommand { get; }

    private void CreateNewProject()
    {
        foreach (var asset in RecentAssets)
        {
            asset.PropertyChanged -= OnAssetPropertyChanged;
        }

        RecentAssets.Clear();
        ProjectName = "未命名视频项目";
        StatusText = "项目已建立，先导入一个素材文件夹";
        ImportStatusTitle = "尚未导入素材";
        ImportStatusDetail = "支持递归扫描常见图片和视频；不会移动、重命名或修改源文件。";
        WorkflowSteps[0].Summary = "等待导入素材";
        Progress = 0;
    }

    private async Task ImportMediaAsync()
    {
        string? folderPath;

        try
        {
            folderPath = await _selectMediaFolder();
        }
        catch (Exception exception)
        {
            ImportStatusTitle = "无法打开文件夹选择器";
            ImportStatusDetail = exception.Message;
            StatusText = "文件夹选择器启动失败，请重试";
            return;
        }

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        IsImporting = true;
        StatusText = "正在扫描素材文件夹…";
        ImportStatusTitle = $"正在扫描 {Path.GetFileName(folderPath)}";
        ImportStatusDetail = "正在识别支持的媒体文件并生成分类建议。";

        try
        {
            var result = await _assetImportService.ImportFolderAsync(folderPath);
            var existingPaths = RecentAssets
                .Select(asset => asset.FullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var addedCount = 0;

            foreach (var asset in result.Assets.Where(asset => existingPaths.Add(asset.FullPath)))
            {
                asset.PropertyChanged += OnAssetPropertyChanged;
                RecentAssets.Add(asset);
                addedCount++;
            }

            UpdateAssetSummary();

            var folderName = Path.GetFileName(result.FolderPath.TrimEnd(Path.DirectorySeparatorChar));
            ImportStatusTitle = result.Assets.Count == 0
                ? $"{folderName} 中没有支持的素材"
                : $"已从 {folderName} 识别 {result.Assets.Count} 个素材";
            ImportStatusDetail = BuildImportDetail(addedCount, result.Assets.Count, result.SkippedDirectoryCount);
            StatusText = addedCount > 0
                ? $"已加入 {addedCount} 个素材；分类建议可直接在列表中修改"
                : "扫描完成，没有新增素材";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            ImportStatusTitle = "素材导入失败";
            ImportStatusDetail = exception.Message;
            StatusText = "无法读取所选文件夹，请检查访问权限后重试";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private static string BuildImportDetail(int addedCount, int scannedCount, int skippedDirectoryCount)
    {
        var duplicateCount = scannedCount - addedCount;
        var details = new List<string> { $"新增 {addedCount} 个" };

        if (duplicateCount > 0)
        {
            details.Add($"跳过 {duplicateCount} 个重复文件");
        }

        if (skippedDirectoryCount > 0)
        {
            details.Add($"{skippedDirectoryCount} 个目录无权访问");
        }

        details.Add("源文件保持不变");
        return string.Join(" · ", details);
    }

    private void OnAssetPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaAsset.Category))
        {
            UpdateAssetSummary();
        }
    }

    private void UpdateAssetSummary()
    {
        var pendingCount = RecentAssets.Count(asset => asset.Category == "其他");
        WorkflowSteps[0].Summary = pendingCount == 0
            ? $"{RecentAssets.Count} 个素材 · 分类已确认"
            : $"{RecentAssets.Count} 个素材 · {pendingCount} 个建议待确认";
        Progress = RecentAssets.Count > 0 ? Math.Max(Progress, 25) : 0;
    }

}
