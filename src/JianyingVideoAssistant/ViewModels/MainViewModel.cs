using System.Collections.ObjectModel;
using JianyingVideoAssistant.Infrastructure;
using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string _projectName = "春季上新 · 商品短视频";
    private string _statusText = "项目已准备 3 / 4，确认音乐后即可检查草稿";
    private double _progress = 75;

    public MainViewModel()
    {
        NewProjectCommand = new RelayCommand(CreateNewProject);
        ImportMediaCommand = new RelayCommand(ImportMedia);
        UseCopyCommand = new RelayCommand(UseCopy);
        ApplyMusicCommand = new RelayCommand(ApplyMusic);
        GenerateDraftCommand = new RelayCommand(GenerateDraftPreview);

        WorkflowSteps =
        [
            new("01", "\uEB9F", "素材", "拖入即可用，分类建议稍后确认", "24 个素材 · 2 个待确认", "继续导入", ImportMediaCommand),
            new("02", "\uE8A5", "文案", "按用途找片段，一键加入脚本", "已采用 5 段 · 缺少结尾", "找一句结尾", UseCopyCommand),
            new("03", "\uE8D6", "音乐", "按情绪和时长，只推荐少量结果", "4 首推荐 · 尚未应用", "试听并应用", ApplyMusicCommand),
            new("04", "\uE74E", "剪映草稿", "一次检查后生成新的安全副本", "等待音乐 · 原草稿不会被覆盖", "检查并生成", GenerateDraftCommand)
        ];

        RecentAssets =
        [
            new("product_front_01.mp4", "00:06 · 1080 × 1920", "商品"),
            new("model_tryon_03.mp4", "00:09 · 1080 × 1920", "人物"),
            new("detail_closeup_02.mov", "00:04 · 4K", "细节")
        ];
    }

    public string Greeting => "晚上好，继续完成你的下一条视频";
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
    public RelayCommand ImportMediaCommand { get; }
    public RelayCommand UseCopyCommand { get; }
    public RelayCommand ApplyMusicCommand { get; }
    public RelayCommand GenerateDraftCommand { get; }

    private void CreateNewProject()
    {
        ProjectName = "未命名视频项目";
        StatusText = "项目已建立，把素材拖进来即可开始";
        Progress = 0;
    }

    private void ImportMedia()
    {
        StatusText = "已进入素材导入；未分类素材也可以直接使用";
        WorkflowSteps[0].Summary = "准备选择文件或文件夹 · 分类不是必填项";
    }

    private void UseCopy()
    {
        StatusText = "已采用推荐结尾，可立即撤销或换一句";
        WorkflowSteps[1].Summary = "已采用 6 段 · 脚本结构完整";
        Progress = Math.Max(Progress, 75);
    }

    private void ApplyMusic()
    {
        StatusText = "已应用“清晨气泡”，将按 30 秒自动裁切并淡出";
        WorkflowSteps[2].Summary = "已应用：清晨气泡 · 30 秒自动裁切";
        WorkflowSteps[3].Summary = "要素齐全 · 可以进行生成前检查";
        Progress = 100;
    }

    private void GenerateDraftPreview()
    {
        StatusText = Progress < 100
            ? "草稿检查发现背景音乐未确认；可以补充，也可以无音乐继续"
            : "草稿检查通过；首版只预览输出，不会写入剪映目录";
    }
}
