using System.Diagnostics;
using System.Windows;
using JianyingVideoAssistant.Adapters;
using JianyingVideoAssistant.Infrastructure;
using JianyingVideoAssistant.Services;
using JianyingVideoAssistant.ViewModels;
using Microsoft.Win32;

namespace JianyingVideoAssistant;

public partial class MainWindow : Window
{
    private readonly WpfMusicPreviewService _musicPreviewService = new();

    public MainWindow()
    {
        InitializeComponent();
        var draftRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JianyingVideoAssistant",
            "DraftPreviews");
        var projectStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JianyingVideoAssistant",
            "current-project.json");
        DataContext = new MainViewModel(
            new LocalAssetImportService(new MediaCategoryClassifier()),
            new LocalContentLibraryService(),
            new LocalMusicLibraryService(),
            _musicPreviewService,
            new JianyingDraftPreviewAdapter(draftRoot),
            new JsonProjectStore(projectStatePath),
            PickMediaFolderAsync,
            PickMusicFilesAsync,
            OpenFolder);
    }

    private void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        await viewModel.ImportDroppedPathsAsync(paths);
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => WindowBackdrop.TryApply(this);

    private static Task<string?> PickMediaFolderAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择素材文件夹",
            Multiselect = false
        };
        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FolderName : null);
    }

    private static Task<IReadOnlyList<string>> PickMusicFilesAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择本地背景音乐",
            Filter = "音频文件|*.mp3;*.wav;*.m4a;*.aac;*.wma;*.flac;*.ogg|所有文件|*.*",
            Multiselect = true
        };
        IReadOnlyList<string> result = dialog.ShowDialog() == true ? dialog.FileNames : [];
        return Task.FromResult(result);
    }

    private static void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel) viewModel.SaveNow();
        _musicPreviewService.Dispose();
    }
}
