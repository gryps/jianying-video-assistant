using System.Collections.ObjectModel;
using JianyingVideoAssistant.Infrastructure;
using JianyingVideoAssistant.Models;
using JianyingVideoAssistant.Services;

namespace JianyingVideoAssistant.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IAssetImportService _assetImportService;
    private readonly IContentLibraryService _contentLibraryService;
    private readonly IMusicLibraryService _musicLibraryService;
    private readonly IMusicPreviewService _musicPreviewService;
    private readonly IDraftExporter _draftExporter;
    private readonly IProjectStore _projectStore;
    private readonly Func<Task<string?>> _selectMediaFolder;
    private readonly Func<Task<IReadOnlyList<string>>> _selectMusicFiles;
    private readonly Action<string> _openFolder;
    private readonly Stack<MusicTrack?> _musicHistory = new();

    private string _projectName = "未命名视频项目";
    private string _statusText = "项目已建立，先导入一个素材文件夹";
    private string _importStatusTitle = "尚未导入素材";
    private string _importStatusDetail = "选择文件夹，或把视频、图片和文件夹直接拖到窗口。";
    private double _progress;
    private bool _isImporting;
    private bool _isContentPanelOpen;
    private bool _isMusicPanelOpen;
    private bool _isDraftPanelOpen;
    private string _contentSearchText = string.Empty;
    private string _selectedContentPurpose = "全部";
    private ContentSnippet? _selectedContentSnippet;
    private string _selectedMusicMood = "全部";
    private MusicTrack? _selectedMusicTrack;
    private MusicTrack? _appliedMusic;
    private bool _isPreviewingMusic;
    private bool _isExportingDraft;
    private string? _lastDraftPath;
    private bool _isRestoringProject;
    private string _selectedBulkCategory = "商品";
    private string _customContentText = string.Empty;
    private string _customContentPurpose = "卖点";
    private ProjectScriptSegment? _selectedScriptSegment;

    public MainViewModel(
        IAssetImportService assetImportService,
        IContentLibraryService contentLibraryService,
        IMusicLibraryService musicLibraryService,
        IMusicPreviewService musicPreviewService,
        IDraftExporter draftExporter,
        IProjectStore projectStore,
        Func<Task<string?>> selectMediaFolder,
        Func<Task<IReadOnlyList<string>>> selectMusicFiles,
        Action<string> openFolder)
    {
        _assetImportService = assetImportService;
        _contentLibraryService = contentLibraryService;
        _musicLibraryService = musicLibraryService;
        _musicPreviewService = musicPreviewService;
        _draftExporter = draftExporter;
        _projectStore = projectStore;
        _selectMediaFolder = selectMediaFolder;
        _selectMusicFiles = selectMusicFiles;
        _openFolder = openFolder;

        RecentAssets = [];
        FilteredContentSnippets = [];
        ProjectScriptSegments = [];
        MusicTracks = [];
        FilteredMusicTracks = [];
        DraftCheckItems = [];

        NewProjectCommand = new RelayCommand(CreateNewProject);
        ImportMediaCommand = new AsyncRelayCommand(ImportMediaAsync, () => !IsImporting);
        ApplyBulkCategoryCommand = new RelayCommand(ApplyBulkCategory, () => RecentAssets.Any(asset => asset.Category == "其他"));
        RemoveMissingAssetsCommand = new RelayCommand(RemoveMissingAssets, () => RecentAssets.Any(asset => !File.Exists(asset.FullPath)));
        OpenContentPanelCommand = new RelayCommand(OpenContentPanel);
        CloseContentPanelCommand = new RelayCommand(ClosePanels);
        ApplySelectedContentCommand = new RelayCommand(ApplySelectedContent, () => SelectedContentSnippet is not null);
        UndoLastScriptSegmentCommand = new RelayCommand(UndoLastScriptSegment, () => ProjectScriptSegments.Count > 0);
        AddCustomContentCommand = new RelayCommand(AddCustomContent, () => !string.IsNullOrWhiteSpace(CustomContentText));
        RemoveSelectedScriptSegmentCommand = new RelayCommand(RemoveSelectedScriptSegment, () => SelectedScriptSegment is not null);
        MoveScriptSegmentUpCommand = new RelayCommand(() => MoveSelectedScriptSegment(-1), CanMoveScriptSegmentUp);
        MoveScriptSegmentDownCommand = new RelayCommand(() => MoveSelectedScriptSegment(1), CanMoveScriptSegmentDown);
        OpenMusicPanelCommand = new RelayCommand(OpenMusicPanel);
        CloseMusicPanelCommand = new RelayCommand(ClosePanels);
        ImportMusicCommand = new AsyncRelayCommand(ImportMusicAsync);
        ToggleMusicPreviewCommand = new AsyncRelayCommand(ToggleMusicPreviewAsync, () => SelectedMusicTrack is not null);
        ApplySelectedMusicCommand = new RelayCommand(ApplySelectedMusic, () => SelectedMusicTrack is not null);
        UndoMusicCommand = new RelayCommand(UndoMusic, () => _musicHistory.Count > 0);
        RemoveSelectedMusicCommand = new RelayCommand(RemoveSelectedMusic, () => SelectedMusicTrack is not null);
        RemoveMissingMusicCommand = new RelayCommand(RemoveMissingMusic, () => MusicTracks.Any(track => !File.Exists(track.FullPath)));
        OpenDraftPanelCommand = new RelayCommand(OpenDraftPanel);
        CloseDraftPanelCommand = new RelayCommand(ClosePanels);
        GenerateDraftCommand = new AsyncRelayCommand(GenerateDraftAsync, CanGenerateDraft);
        OpenLastDraftCommand = new RelayCommand(OpenLastDraft, () => LastDraftPath is not null);

        WorkflowSteps =
        [
            new("01", "\uEB9F", "添加素材", "选择素材文件夹，自动扫描视频和图片", "导入后可直接修正分类建议", "选择素材文件夹", true, ImportMediaCommand),
            new("02", "\uE8A5", "添加视频文案", "按用途或关键词挑选完整句子", "采用后直接组成当前项目脚本", "添加视频文案", true, OpenContentPanelCommand),
            new("03", "\uE8D6", "选择背景音乐", "导入本地音乐，按情绪筛选并试听", "应用后可随时替换或撤销", "选择背景音乐", true, OpenMusicPanelCommand),
            new("04", "\uE74E", "准备剪映草稿", "集中检查素材、文案、音乐和输出位置", "生成项目预览并可打开输出位置", "检查并生成预览", true, OpenDraftPanelCommand)
        ];

        RestoreProject();
        RefreshContentResults();
        RefreshMusicResults();
        RefreshDraftChecks();
    }

    public string Greeting => "无需先命名；导入素材后会自动采用文件夹名称";

    public string SelectedBulkCategory
    {
        get => _selectedBulkCategory;
        set => SetProperty(ref _selectedBulkCategory, value);
    }

    public string CustomContentText
    {
        get => _customContentText;
        set
        {
            if (SetProperty(ref _customContentText, value)) AddCustomContentCommand.NotifyCanExecuteChanged();
        }
    }

    public string CustomContentPurpose
    {
        get => _customContentPurpose;
        set => SetProperty(ref _customContentPurpose, value);
    }

    public ProjectScriptSegment? SelectedScriptSegment
    {
        get => _selectedScriptSegment;
        set
        {
            if (!SetProperty(ref _selectedScriptSegment, value)) return;
            RemoveSelectedScriptSegmentCommand.NotifyCanExecuteChanged();
            MoveScriptSegmentUpCommand.NotifyCanExecuteChanged();
            MoveScriptSegmentDownCommand.NotifyCanExecuteChanged();
        }
    }

    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (SetProperty(ref _projectName, value))
            {
                InvalidateDraft();
            }
        }
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

    public bool IsContentPanelOpen
    {
        get => _isContentPanelOpen;
        private set => SetProperty(ref _isContentPanelOpen, value);
    }

    public bool IsMusicPanelOpen
    {
        get => _isMusicPanelOpen;
        private set => SetProperty(ref _isMusicPanelOpen, value);
    }

    public bool IsDraftPanelOpen
    {
        get => _isDraftPanelOpen;
        private set => SetProperty(ref _isDraftPanelOpen, value);
    }

    public string ContentSearchText
    {
        get => _contentSearchText;
        set
        {
            if (SetProperty(ref _contentSearchText, value))
            {
                RefreshContentResults();
            }
        }
    }

    public string SelectedContentPurpose
    {
        get => _selectedContentPurpose;
        set
        {
            if (SetProperty(ref _selectedContentPurpose, value))
            {
                RefreshContentResults();
            }
        }
    }

    public ContentSnippet? SelectedContentSnippet
    {
        get => _selectedContentSnippet;
        set
        {
            if (SetProperty(ref _selectedContentSnippet, value))
            {
                ApplySelectedContentCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedMusicMood
    {
        get => _selectedMusicMood;
        set
        {
            if (SetProperty(ref _selectedMusicMood, value))
            {
                RefreshMusicResults();
            }
        }
    }

    public MusicTrack? SelectedMusicTrack
    {
        get => _selectedMusicTrack;
        set
        {
            if (SetProperty(ref _selectedMusicTrack, value))
            {
                StopPreview();
                ToggleMusicPreviewCommand.NotifyCanExecuteChanged();
                ApplySelectedMusicCommand.NotifyCanExecuteChanged();
                RemoveSelectedMusicCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public MusicTrack? AppliedMusic
    {
        get => _appliedMusic;
        private set
        {
            if (SetProperty(ref _appliedMusic, value))
            {
                OnPropertyChanged(nameof(AppliedMusicSummary));
                InvalidateDraft();
                UpdateProjectProgress();
            }
        }
    }

    public bool IsPreviewingMusic
    {
        get => _isPreviewingMusic;
        private set
        {
            if (SetProperty(ref _isPreviewingMusic, value))
            {
                OnPropertyChanged(nameof(PreviewActionLabel));
            }
        }
    }

    public bool IsExportingDraft
    {
        get => _isExportingDraft;
        private set
        {
            if (SetProperty(ref _isExportingDraft, value))
            {
                OnPropertyChanged(nameof(DraftActionLabel));
                GenerateDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? LastDraftPath
    {
        get => _lastDraftPath;
        private set
        {
            if (SetProperty(ref _lastDraftPath, value))
            {
                OnPropertyChanged(nameof(HasLastDraft));
                OpenLastDraftCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasLastDraft => LastDraftPath is not null;
    public string ContentResultSummary => FilteredContentSnippets.Count == 0 ? "没有匹配文案" : $"找到 {FilteredContentSnippets.Count} 条本地文案";
    public string ScriptSummary => ProjectScriptSegments.Count == 0 ? "当前脚本还是空的" : $"当前脚本已有 {ProjectScriptSegments.Count} 段";
    public string MusicResultSummary => MusicTracks.Count == 0 ? "还没有导入本地音乐" : $"当前显示 {FilteredMusicTracks.Count} / {MusicTracks.Count} 首";
    public string AppliedMusicSummary => AppliedMusic is null ? "尚未应用背景音乐" : $"已应用：{AppliedMusic.Name} · {AppliedMusic.Mood}";
    public string PreviewActionLabel => IsPreviewingMusic ? "停止试听" : "试听选中音乐";
    public string DraftActionLabel => IsExportingDraft ? "正在生成…" : "生成项目预览";
    public string DraftCheckSummary => DraftBlockingCount == 0 ? "检查通过，可以生成" : $"有 {DraftBlockingCount} 项需要处理";
    public int DraftBlockingCount => DraftCheckItems.Count(item => item.IsBlocking);

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
    public ObservableCollection<ContentSnippet> FilteredContentSnippets { get; }
    public ObservableCollection<ProjectScriptSegment> ProjectScriptSegments { get; }
    public ObservableCollection<MusicTrack> MusicTracks { get; }
    public ObservableCollection<MusicTrack> FilteredMusicTracks { get; }
    public ObservableCollection<DraftCheckItem> DraftCheckItems { get; }
    public IReadOnlyList<string> AssetCategoryOptions => AssetCategories.All;
    public IReadOnlyList<string> ContentPurposeOptions { get; } = ["全部", "开头", "卖点", "转场", "结尾"];
    public IReadOnlyList<string> MusicMoodOptions => MusicMoods.All;

    public RelayCommand NewProjectCommand { get; }
    public AsyncRelayCommand ImportMediaCommand { get; }
    public RelayCommand ApplyBulkCategoryCommand { get; }
    public RelayCommand RemoveMissingAssetsCommand { get; }
    public RelayCommand OpenContentPanelCommand { get; }
    public RelayCommand CloseContentPanelCommand { get; }
    public RelayCommand ApplySelectedContentCommand { get; }
    public RelayCommand UndoLastScriptSegmentCommand { get; }
    public RelayCommand AddCustomContentCommand { get; }
    public RelayCommand RemoveSelectedScriptSegmentCommand { get; }
    public RelayCommand MoveScriptSegmentUpCommand { get; }
    public RelayCommand MoveScriptSegmentDownCommand { get; }
    public RelayCommand OpenMusicPanelCommand { get; }
    public RelayCommand CloseMusicPanelCommand { get; }
    public AsyncRelayCommand ImportMusicCommand { get; }
    public AsyncRelayCommand ToggleMusicPreviewCommand { get; }
    public RelayCommand ApplySelectedMusicCommand { get; }
    public RelayCommand UndoMusicCommand { get; }
    public RelayCommand RemoveSelectedMusicCommand { get; }
    public RelayCommand RemoveMissingMusicCommand { get; }
    public RelayCommand OpenDraftPanelCommand { get; }
    public RelayCommand CloseDraftPanelCommand { get; }
    public AsyncRelayCommand GenerateDraftCommand { get; }
    public RelayCommand OpenLastDraftCommand { get; }

    private void CreateNewProject()
    {
        _isRestoringProject = true;
        foreach (var asset in RecentAssets)
        {
            asset.PropertyChanged -= OnAssetPropertyChanged;
        }

        StopPreview();
        RecentAssets.Clear();
        ProjectScriptSegments.Clear();
        MusicTracks.Clear();
        FilteredMusicTracks.Clear();
        _musicHistory.Clear();
        AppliedMusic = null;
        LastDraftPath = null;
        ClosePanels();
        ContentSearchText = string.Empty;
        SelectedContentPurpose = "全部";
        CustomContentText = string.Empty;
        CustomContentPurpose = "卖点";
        SelectedScriptSegment = null;
        SelectedMusicMood = "全部";
        ProjectName = "未命名视频项目";
        StatusText = "已建立新项目，先导入一个素材文件夹";
        ImportStatusTitle = "尚未导入素材";
        ImportStatusDetail = "选择文件夹，或把视频、图片和文件夹直接拖到窗口。";
        WorkflowSteps[0].Summary = "导入后可直接修正分类建议";
        WorkflowSteps[1].Summary = "采用后直接组成当前项目脚本";
        WorkflowSteps[2].Summary = "应用后可随时替换或撤销";
        WorkflowSteps[3].Summary = "生成项目预览并可打开输出位置";
        NotifyScriptChanged();
        NotifyMusicChanged();
        RefreshDraftChecks();
        UpdateProjectProgress();
        _isRestoringProject = false;
        try
        {
            _projectStore.Clear();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText = $"无法清除上次保存的项目：{exception.Message}";
        }
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
            AddImportedAssets(result, "扫描完成");
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

    public async Task ImportDroppedPathsAsync(IReadOnlyList<string> paths)
    {
        if (IsImporting || paths.Count == 0) return;
        IsImporting = true;
        StatusText = "正在导入拖入的素材…";
        ImportStatusTitle = "正在读取拖入内容";
        ImportStatusDetail = "文件夹中的子目录会一并扫描。";
        try
        {
            var result = await _assetImportService.ImportPathsAsync(paths);
            AddImportedAssets(result, "拖入完成");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            ImportStatusTitle = "拖入素材失败";
            ImportStatusDetail = exception.Message;
            StatusText = "无法读取拖入内容，请检查文件是否仍然存在";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private void AddImportedAssets(AssetImportResult result, string completedLabel)
    {
        var existingPaths = RecentAssets.Select(asset => asset.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedCount = 0;
        foreach (var asset in result.Assets.Where(asset => existingPaths.Add(asset.FullPath)))
        {
            asset.PropertyChanged += OnAssetPropertyChanged;
            RecentAssets.Add(asset);
            addedCount++;
        }

        UpdateAssetSummary();
        var folderName = Path.GetFileName(result.FolderPath.TrimEnd(Path.DirectorySeparatorChar));
        if ((string.IsNullOrWhiteSpace(ProjectName) || ProjectName == "未命名视频项目") && !string.IsNullOrWhiteSpace(folderName))
        {
            ProjectName = folderName;
        }
        ImportStatusTitle = result.Assets.Count == 0
            ? "没有找到支持的素材"
            : $"{completedLabel}：识别 {result.Assets.Count} 个素材";
        ImportStatusDetail = BuildImportDetail(addedCount, result.Assets.Count, result.SkippedDirectoryCount);
        StatusText = addedCount > 0 ? $"已加入 {addedCount} 个素材；分类建议可在列表中修改" : "没有新增素材";
        InvalidateDraft();
    }

    private static string BuildImportDetail(int addedCount, int scannedCount, int skippedDirectoryCount)
    {
        var details = new List<string> { $"新增 {addedCount} 个" };
        if (scannedCount - addedCount > 0) details.Add($"跳过 {scannedCount - addedCount} 个重复文件");
        if (skippedDirectoryCount > 0) details.Add($"{skippedDirectoryCount} 个目录无权访问");
        return string.Join(" · ", details);
    }

    private void OnAssetPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaAsset.Category))
        {
            UpdateAssetSummary();
            InvalidateDraft();
        }
    }

    private void UpdateAssetSummary()
    {
        var pendingCount = RecentAssets.Count(asset => asset.Category == "其他");
        WorkflowSteps[0].Summary = RecentAssets.Count == 0
            ? "导入后可直接修正分类建议"
            : pendingCount == 0
                ? $"{RecentAssets.Count} 个素材 · 分类已确认"
                : $"{RecentAssets.Count} 个素材 · {pendingCount} 个建议待确认";
        UpdateProjectProgress();
        ApplyBulkCategoryCommand.NotifyCanExecuteChanged();
        RemoveMissingAssetsCommand.NotifyCanExecuteChanged();
    }

    private void ApplyBulkCategory()
    {
        var targets = RecentAssets.Where(asset => asset.Category == "其他").ToArray();
        if (targets.Length == 0) return;
        _isRestoringProject = true;
        foreach (var asset in targets) asset.Category = SelectedBulkCategory;
        _isRestoringProject = false;
        UpdateAssetSummary();
        StatusText = $"已将 {targets.Length} 个待确认素材设为{SelectedBulkCategory}";
        InvalidateDraft();
    }

    private void RemoveMissingAssets()
    {
        var missing = RecentAssets.Where(asset => !File.Exists(asset.FullPath)).ToArray();
        foreach (var asset in missing)
        {
            asset.PropertyChanged -= OnAssetPropertyChanged;
            RecentAssets.Remove(asset);
        }
        UpdateAssetSummary();
        StatusText = $"已移除 {missing.Length} 个失效素材";
        InvalidateDraft();
    }

    private void OpenContentPanel()
    {
        ClosePanels();
        IsContentPanelOpen = true;
        StatusText = "选择文案；采用后会直接加入当前项目脚本";
    }

    private void RefreshContentResults()
    {
        var previousId = SelectedContentSnippet?.Id;
        var results = _contentLibraryService.Search(ContentSearchText, SelectedContentPurpose);
        FilteredContentSnippets.Clear();
        foreach (var snippet in results) FilteredContentSnippets.Add(snippet);
        SelectedContentSnippet = FilteredContentSnippets.FirstOrDefault(item => item.Id == previousId) ?? FilteredContentSnippets.FirstOrDefault();
        OnPropertyChanged(nameof(ContentResultSummary));
    }

    private void ApplySelectedContent()
    {
        if (SelectedContentSnippet is not { } snippet) return;
        var duplicate = ProjectScriptSegments.Any(segment => segment.SourceId == snippet.Id);
        ProjectScriptSegments.Add(new ProjectScriptSegment(snippet.Id, snippet.Purpose, snippet.Text));
        StatusText = duplicate ? $"已再次加入一段{snippet.Purpose}文案；脚本中已有相同片段" : $"已加入一段{snippet.Purpose}文案";
        NotifyScriptChanged();
        InvalidateDraft();
    }

    private void UndoLastScriptSegment()
    {
        if (ProjectScriptSegments.Count == 0) return;
        var removed = ProjectScriptSegments[^1];
        ProjectScriptSegments.RemoveAt(ProjectScriptSegments.Count - 1);
        StatusText = $"已撤销最近加入的{removed.Purpose}文案";
        NotifyScriptChanged();
        InvalidateDraft();
    }

    private void AddCustomContent()
    {
        var text = CustomContentText.Trim();
        if (text.Length == 0) return;
        var segment = new ProjectScriptSegment($"custom-{Guid.NewGuid():N}", CustomContentPurpose, text);
        ProjectScriptSegments.Add(segment);
        SelectedScriptSegment = segment;
        CustomContentText = string.Empty;
        StatusText = $"已加入一段自定义{segment.Purpose}文案";
        NotifyScriptChanged();
        InvalidateDraft();
    }

    private void RemoveSelectedScriptSegment()
    {
        if (SelectedScriptSegment is not { } segment) return;
        ProjectScriptSegments.Remove(segment);
        SelectedScriptSegment = null;
        StatusText = $"已从脚本移除一段{segment.Purpose}文案";
        NotifyScriptChanged();
        InvalidateDraft();
    }

    private bool CanMoveScriptSegmentUp()
        => SelectedScriptSegment is not null && ProjectScriptSegments.IndexOf(SelectedScriptSegment) > 0;

    private bool CanMoveScriptSegmentDown()
        => SelectedScriptSegment is not null
            && ProjectScriptSegments.IndexOf(SelectedScriptSegment) is var index
            && index >= 0
            && index < ProjectScriptSegments.Count - 1;

    private void MoveSelectedScriptSegment(int offset)
    {
        if (SelectedScriptSegment is not { } segment) return;
        var oldIndex = ProjectScriptSegments.IndexOf(segment);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= ProjectScriptSegments.Count) return;
        ProjectScriptSegments.Move(oldIndex, newIndex);
        StatusText = "已调整脚本文案顺序";
        MoveScriptSegmentUpCommand.NotifyCanExecuteChanged();
        MoveScriptSegmentDownCommand.NotifyCanExecuteChanged();
        InvalidateDraft();
    }

    private void NotifyScriptChanged()
    {
        WorkflowSteps[1].Summary = ProjectScriptSegments.Count == 0 ? "采用后直接组成当前项目脚本" : $"项目脚本已有 {ProjectScriptSegments.Count} 段";
        OnPropertyChanged(nameof(ScriptSummary));
        UndoLastScriptSegmentCommand.NotifyCanExecuteChanged();
        RemoveSelectedScriptSegmentCommand.NotifyCanExecuteChanged();
        MoveScriptSegmentUpCommand.NotifyCanExecuteChanged();
        MoveScriptSegmentDownCommand.NotifyCanExecuteChanged();
        UpdateProjectProgress();
    }

    private void OpenMusicPanel()
    {
        ClosePanels();
        IsMusicPanelOpen = true;
        StatusText = MusicTracks.Count == 0 ? "先导入本地音乐，再试听并应用" : "选择一首音乐试听或应用到当前项目";
    }

    private async Task ImportMusicAsync()
    {
        var paths = await _selectMusicFiles();
        if (paths.Count == 0) return;
        var existing = MusicTracks.Select(track => track.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var imported = _musicLibraryService.ImportFiles(paths);
        var added = 0;
        foreach (var track in imported.Where(track => existing.Add(track.FullPath)))
        {
            MusicTracks.Add(track);
            added++;
        }

        RefreshMusicResults();
        StatusText = added > 0 ? $"已加入 {added} 首本地音乐，可试听后应用" : "没有新增音乐；不支持的格式或重复文件已跳过";
        SaveProject();
    }

    private void RefreshMusicResults()
    {
        var previousId = SelectedMusicTrack?.Id;
        var tracks = MusicTracks.Where(track => SelectedMusicMood == "全部" || track.Mood == SelectedMusicMood).ToArray();
        FilteredMusicTracks.Clear();
        foreach (var track in tracks) FilteredMusicTracks.Add(track);
        SelectedMusicTrack = FilteredMusicTracks.FirstOrDefault(track => track.Id == previousId) ?? FilteredMusicTracks.FirstOrDefault();
        OnPropertyChanged(nameof(MusicResultSummary));
    }

    private async Task ToggleMusicPreviewAsync()
    {
        if (IsPreviewingMusic)
        {
            StopPreview();
            StatusText = "已停止试听";
            return;
        }

        if (SelectedMusicTrack is not { } track) return;
        try
        {
            await _musicPreviewService.PlayAsync(track.FullPath);
            IsPreviewingMusic = true;
            StatusText = $"正在试听：{track.Name}";
        }
        catch (Exception exception)
        {
            StopPreview();
            StatusText = $"无法试听该音乐：{exception.Message}";
        }
    }

    private void StopPreview()
    {
        _musicPreviewService.Stop();
        IsPreviewingMusic = false;
    }

    private void ApplySelectedMusic()
    {
        if (SelectedMusicTrack is not { } track) return;
        _musicHistory.Push(AppliedMusic);
        AppliedMusic = track;
        WorkflowSteps[2].Summary = $"已应用 {track.Name} · {track.Mood}";
        StatusText = $"已将 {track.Name} 应用为背景音乐";
        UndoMusicCommand.NotifyCanExecuteChanged();
    }

    private void UndoMusic()
    {
        if (_musicHistory.Count == 0) return;
        AppliedMusic = _musicHistory.Pop();
        WorkflowSteps[2].Summary = AppliedMusic is null ? "应用后可随时替换或撤销" : $"已恢复 {AppliedMusic.Name} · {AppliedMusic.Mood}";
        StatusText = AppliedMusic is null ? "已撤销背景音乐选择" : $"已恢复背景音乐：{AppliedMusic.Name}";
        UndoMusicCommand.NotifyCanExecuteChanged();
    }

    private void NotifyMusicChanged()
    {
        OnPropertyChanged(nameof(MusicResultSummary));
        OnPropertyChanged(nameof(AppliedMusicSummary));
        UndoMusicCommand.NotifyCanExecuteChanged();
        RemoveMissingMusicCommand.NotifyCanExecuteChanged();
    }

    private void RemoveSelectedMusic()
    {
        if (SelectedMusicTrack is not { } track) return;
        StopPreview();
        if (AppliedMusic?.Id == track.Id) AppliedMusic = null;
        MusicTracks.Remove(track);
        RefreshMusicResults();
        StatusText = $"已从列表移除 {track.Name}";
        NotifyMusicChanged();
        SaveProject();
    }

    private void RemoveMissingMusic()
    {
        var missing = MusicTracks.Where(track => !File.Exists(track.FullPath)).ToArray();
        if (missing.Length == 0) return;
        StopPreview();
        if (AppliedMusic is not null && missing.Any(track => track.Id == AppliedMusic.Id)) AppliedMusic = null;
        foreach (var track in missing) MusicTracks.Remove(track);
        RefreshMusicResults();
        StatusText = $"已清理 {missing.Length} 个失效音乐文件";
        NotifyMusicChanged();
        SaveProject();
    }

    private void OpenDraftPanel()
    {
        ClosePanels();
        RefreshDraftChecks();
        IsDraftPanelOpen = true;
        StatusText = DraftBlockingCount == 0 ? "检查通过，可以生成项目预览" : "请先处理草稿检查中的阻塞项";
    }

    private void RefreshDraftChecks()
    {
        DraftCheckItems.Clear();
        var missingMedia = RecentAssets.Count(asset => !File.Exists(asset.FullPath));
        DraftCheckItems.Add(new DraftCheckItem(
            "素材文件",
            RecentAssets.Count == 0 ? "尚未导入素材" : missingMedia > 0 ? $"{missingMedia} 个素材路径已失效" : $"{RecentAssets.Count} 个素材路径有效",
            RecentAssets.Count == 0 || missingMedia > 0));
        DraftCheckItems.Add(new DraftCheckItem(
            "视频文案",
            ProjectScriptSegments.Count == 0 ? "未添加文案，可继续生成后自行编辑" : $"已准备 {ProjectScriptSegments.Count} 段文案",
            false));
        DraftCheckItems.Add(new DraftCheckItem(
            "背景音乐",
            AppliedMusic is null ? "未选择音乐，可继续生成" : File.Exists(AppliedMusic.FullPath) ? $"已选择 {AppliedMusic.Name}" : "所选音乐路径已失效",
            AppliedMusic is not null && !File.Exists(AppliedMusic.FullPath)));
        OnPropertyChanged(nameof(DraftBlockingCount));
        OnPropertyChanged(nameof(DraftCheckSummary));
        GenerateDraftCommand.NotifyCanExecuteChanged();
    }

    private bool CanGenerateDraft() => !IsExportingDraft && DraftBlockingCount == 0;

    private async Task GenerateDraftAsync()
    {
        RefreshDraftChecks();
        if (!CanGenerateDraft()) return;
        IsExportingDraft = true;
        try
        {
            var request = new DraftExportRequest(
                ProjectName,
                RecentAssets.Select(asset => new DraftMediaItem(asset.FullPath, asset.Category)).ToArray(),
                ProjectScriptSegments.ToArray(),
                AppliedMusic);
            var result = await _draftExporter.ExportPreviewAsync(request);
            if (!result.Succeeded || result.PreviewPath is null)
            {
                StatusText = string.Join(" ", result.Messages);
                RefreshDraftChecks();
                return;
            }

            LastDraftPath = result.PreviewPath;
            WorkflowSteps[3].Summary = "项目预览已生成 · 可打开所在位置";
            StatusText = result.Messages.FirstOrDefault() ?? "项目预览已生成，可打开所在位置";
            UpdateProjectProgress();
            SaveProject();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText = $"草稿预览生成失败：{exception.Message}";
        }
        finally
        {
            IsExportingDraft = false;
        }
    }

    private void OpenLastDraft()
    {
        if (LastDraftPath is not null) _openFolder(LastDraftPath);
    }

    private void InvalidateDraft()
    {
        if (LastDraftPath is not null)
        {
            LastDraftPath = null;
            WorkflowSteps[3].Summary = "项目内容已变化，请重新生成预览副本";
        }
        if (IsDraftPanelOpen) RefreshDraftChecks();
        UpdateProjectProgress();
        SaveProject();
    }

    public void SaveNow() => SaveProject();

    private void RestoreProject()
    {
        _isRestoringProject = true;
        try
        {
            var snapshot = _projectStore.Load();
            if (snapshot is null) return;

            _projectName = string.IsNullOrWhiteSpace(snapshot.ProjectName) ? "未命名视频项目" : snapshot.ProjectName;
            foreach (var savedAsset in snapshot.Assets)
            {
                var asset = new MediaAsset(
                    savedAsset.FullPath,
                    savedAsset.Name,
                    savedAsset.Detail,
                    savedAsset.Category,
                    savedAsset.CategorySourceLabel);
                asset.PropertyChanged += OnAssetPropertyChanged;
                RecentAssets.Add(asset);
            }

            foreach (var segment in snapshot.ScriptSegments) ProjectScriptSegments.Add(segment);
            foreach (var track in snapshot.MusicTracks) MusicTracks.Add(track);
            _appliedMusic = MusicTracks.FirstOrDefault(track => track.Id == snapshot.AppliedMusicId);
            _lastDraftPath = snapshot.LastPreviewPath is not null && Directory.Exists(snapshot.LastPreviewPath)
                ? snapshot.LastPreviewPath
                : null;

            UpdateAssetSummary();
            NotifyScriptChanged();
            NotifyMusicChanged();
            if (AppliedMusic is not null) WorkflowSteps[2].Summary = $"已应用 {AppliedMusic.Name} · {AppliedMusic.Mood}";
            if (LastDraftPath is not null) WorkflowSteps[3].Summary = "项目预览已生成 · 可打开所在位置";
            StatusText = "已恢复上次项目，可以继续编辑";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            StatusText = $"无法恢复上次项目：{exception.Message}";
        }
        finally
        {
            _isRestoringProject = false;
        }
    }

    private void SaveProject()
    {
        if (_isRestoringProject) return;
        try
        {
            var snapshot = new ProjectSnapshot(
                ProjectSnapshot.CurrentVersion,
                ProjectName,
                RecentAssets.Select(asset => new SavedMediaAsset(
                    asset.FullPath,
                    asset.Name,
                    asset.Detail,
                    asset.Category,
                    asset.CategorySourceLabel)).ToArray(),
                ProjectScriptSegments.ToArray(),
                MusicTracks.ToArray(),
                AppliedMusic?.Id,
                LastDraftPath);
            _projectStore.Save(snapshot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText = $"项目暂未保存：{exception.Message}";
        }
    }

    private void ClosePanels()
    {
        IsContentPanelOpen = false;
        IsMusicPanelOpen = false;
        IsDraftPanelOpen = false;
        StopPreview();
    }

    private void UpdateProjectProgress()
    {
        Progress = (RecentAssets.Count > 0 ? 25 : 0)
            + (ProjectScriptSegments.Count > 0 ? 25 : 0)
            + (AppliedMusic is not null ? 25 : 0)
            + (LastDraftPath is not null ? 25 : 0);
    }
}
