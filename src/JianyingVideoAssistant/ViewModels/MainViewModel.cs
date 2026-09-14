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
    private readonly Func<Task<string?>> _selectMediaFolder;
    private readonly Func<Task<IReadOnlyList<string>>> _selectMusicFiles;
    private readonly Action<string> _openFolder;
    private readonly Stack<MusicTrack?> _musicHistory = new();

    private string _projectName = "未命名视频项目";
    private string _statusText = "项目已建立，先导入一个素材文件夹";
    private string _importStatusTitle = "尚未导入素材";
    private string _importStatusDetail = "支持递归扫描常见图片和视频；不会移动、重命名或修改源文件。";
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

    public MainViewModel(
        IAssetImportService assetImportService,
        IContentLibraryService contentLibraryService,
        IMusicLibraryService musicLibraryService,
        IMusicPreviewService musicPreviewService,
        IDraftExporter draftExporter,
        Func<Task<string?>> selectMediaFolder,
        Func<Task<IReadOnlyList<string>>> selectMusicFiles,
        Action<string> openFolder)
    {
        _assetImportService = assetImportService;
        _contentLibraryService = contentLibraryService;
        _musicLibraryService = musicLibraryService;
        _musicPreviewService = musicPreviewService;
        _draftExporter = draftExporter;
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
        OpenContentPanelCommand = new RelayCommand(OpenContentPanel);
        CloseContentPanelCommand = new RelayCommand(ClosePanels);
        ApplySelectedContentCommand = new RelayCommand(ApplySelectedContent, () => SelectedContentSnippet is not null);
        UndoLastScriptSegmentCommand = new RelayCommand(UndoLastScriptSegment, () => ProjectScriptSegments.Count > 0);
        OpenMusicPanelCommand = new RelayCommand(OpenMusicPanel);
        CloseMusicPanelCommand = new RelayCommand(ClosePanels);
        ImportMusicCommand = new AsyncRelayCommand(ImportMusicAsync);
        ToggleMusicPreviewCommand = new AsyncRelayCommand(ToggleMusicPreviewAsync, () => SelectedMusicTrack is not null);
        ApplySelectedMusicCommand = new RelayCommand(ApplySelectedMusic, () => SelectedMusicTrack is not null);
        UndoMusicCommand = new RelayCommand(UndoMusic, () => _musicHistory.Count > 0);
        OpenDraftPanelCommand = new RelayCommand(OpenDraftPanel);
        CloseDraftPanelCommand = new RelayCommand(ClosePanels);
        GenerateDraftCommand = new AsyncRelayCommand(GenerateDraftAsync, CanGenerateDraft);
        OpenLastDraftCommand = new RelayCommand(OpenLastDraft, () => LastDraftPath is not null);

        WorkflowSteps =
        [
            new("01", "\uEB9F", "添加素材", "选择素材文件夹，自动扫描视频和图片", "导入后可直接修正分类建议", "选择素材文件夹", "现在可用", true, ImportMediaCommand),
            new("02", "\uE8A5", "添加视频文案", "按用途或关键词挑选完整句子", "采用后直接组成当前项目脚本", "添加视频文案", "现在可用", true, OpenContentPanelCommand),
            new("03", "\uE8D6", "选择背景音乐", "导入本地音乐，按情绪筛选并试听", "应用后可随时替换或撤销", "选择背景音乐", "现在可用", true, OpenMusicPanelCommand),
            new("04", "\uE74E", "导出剪映草稿", "集中检查素材、文案、音乐和输出位置", "生成安全预览副本，不覆盖原草稿", "预览并导出", "现在可用", true, OpenDraftPanelCommand)
        ];

        RefreshContentResults();
        RefreshMusicResults();
        RefreshDraftChecks();
    }

    public string Greeting => "按四步准备视频，所有功能都围绕当前项目展开";

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
    public string DraftActionLabel => IsExportingDraft ? "正在生成…" : "生成安全预览副本";
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
    public IReadOnlyList<string> ContentPurposeOptions { get; } = ["全部", "开头", "卖点", "转场", "结尾"];
    public IReadOnlyList<string> MusicMoodOptions => MusicMoods.All;

    public RelayCommand NewProjectCommand { get; }
    public AsyncRelayCommand ImportMediaCommand { get; }
    public RelayCommand OpenContentPanelCommand { get; }
    public RelayCommand CloseContentPanelCommand { get; }
    public RelayCommand ApplySelectedContentCommand { get; }
    public RelayCommand UndoLastScriptSegmentCommand { get; }
    public RelayCommand OpenMusicPanelCommand { get; }
    public RelayCommand CloseMusicPanelCommand { get; }
    public AsyncRelayCommand ImportMusicCommand { get; }
    public AsyncRelayCommand ToggleMusicPreviewCommand { get; }
    public RelayCommand ApplySelectedMusicCommand { get; }
    public RelayCommand UndoMusicCommand { get; }
    public RelayCommand OpenDraftPanelCommand { get; }
    public RelayCommand CloseDraftPanelCommand { get; }
    public AsyncRelayCommand GenerateDraftCommand { get; }
    public RelayCommand OpenLastDraftCommand { get; }

    private void CreateNewProject()
    {
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
        SelectedMusicMood = "全部";
        ProjectName = "未命名视频项目";
        StatusText = "已建立新项目，先导入一个素材文件夹";
        ImportStatusTitle = "尚未导入素材";
        ImportStatusDetail = "支持递归扫描常见图片和视频；不会移动、重命名或修改源文件。";
        WorkflowSteps[0].Summary = "导入后可直接修正分类建议";
        WorkflowSteps[1].Summary = "采用后直接组成当前项目脚本";
        WorkflowSteps[2].Summary = "应用后可随时替换或撤销";
        WorkflowSteps[3].Summary = "生成安全预览副本，不覆盖原草稿";
        NotifyScriptChanged();
        NotifyMusicChanged();
        RefreshDraftChecks();
        UpdateProjectProgress();
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
            ImportStatusTitle = result.Assets.Count == 0 ? $"{folderName} 中没有支持的素材" : $"已从 {folderName} 识别 {result.Assets.Count} 个素材";
            ImportStatusDetail = BuildImportDetail(addedCount, result.Assets.Count, result.SkippedDirectoryCount);
            StatusText = addedCount > 0 ? $"已加入 {addedCount} 个素材；分类建议可在列表中修改" : "扫描完成，没有新增素材";
            InvalidateDraft();
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
        var details = new List<string> { $"新增 {addedCount} 个" };
        if (scannedCount - addedCount > 0) details.Add($"跳过 {scannedCount - addedCount} 个重复文件");
        if (skippedDirectoryCount > 0) details.Add($"{skippedDirectoryCount} 个目录无权访问");
        details.Add("源文件保持不变");
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

    private void NotifyScriptChanged()
    {
        WorkflowSteps[1].Summary = ProjectScriptSegments.Count == 0 ? "采用后直接组成当前项目脚本" : $"项目脚本已有 {ProjectScriptSegments.Count} 段";
        OnPropertyChanged(nameof(ScriptSummary));
        UndoLastScriptSegmentCommand.NotifyCanExecuteChanged();
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
    }

    private void OpenDraftPanel()
    {
        ClosePanels();
        RefreshDraftChecks();
        IsDraftPanelOpen = true;
        StatusText = DraftBlockingCount == 0 ? "草稿预检通过，可以生成安全预览副本" : "请先处理草稿检查中的阻塞项";
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
        DraftCheckItems.Add(new DraftCheckItem(
            "安全输出",
            "仅写入应用工作目录，不读取或覆盖剪映原草稿",
            false));
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
            WorkflowSteps[3].Summary = "安全预览副本已生成 · 可打开所在位置";
            StatusText = result.Messages.FirstOrDefault() ?? "安全预览副本已生成";
            UpdateProjectProgress();
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
