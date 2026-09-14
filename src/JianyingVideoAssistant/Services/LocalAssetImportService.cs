using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public sealed class LocalAssetImportService(MediaCategoryClassifier classifier) : IAssetImportService
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".avi", ".m4v", ".wmv", ".webm"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".heic", ".heif"
    };

    public Task<AssetImportResult> ImportFolderAsync(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        return ImportPathsAsync([folderPath]);
    }

    public Task<AssetImportResult> ImportPathsAsync(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0) return Task.FromResult(new AssetImportResult(string.Empty, [], 0));
        return Task.Run(() => ScanPaths(paths));
    }

    private AssetImportResult ScanPaths(IReadOnlyList<string> paths)
    {
        var assets = new List<MediaAsset>();
        var assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedDirectoryCount = 0;
        var sourceRoot = paths.Select(path => Directory.Exists(path) ? path : Path.GetDirectoryName(path))
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path)) ?? string.Empty;

        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(path))
            {
                ScanFolder(path, assets, assetPaths, ref skippedDirectoryCount);
            }
            else if (File.Exists(path) && TryCreateAsset(Path.GetDirectoryName(path) ?? string.Empty, path, out var asset)
                && assetPaths.Add(asset.FullPath))
            {
                assets.Add(asset);
            }
        }

        assets.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
        return new AssetImportResult(sourceRoot, assets, skippedDirectoryCount);
    }

    private void ScanFolder(
        string folderPath,
        ICollection<MediaAsset> assets,
        ISet<string> assetPaths,
        ref int skippedDirectoryCount)
    {
        var directories = new Stack<string>();
        directories.Push(folderPath);
        while (directories.TryPop(out var currentDirectory))
        {
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
                {
                    if (TryCreateAsset(folderPath, filePath, out var asset) && assetPaths.Add(asset.FullPath))
                    {
                        assets.Add(asset);
                    }
                }

                foreach (var childDirectory in Directory.EnumerateDirectories(currentDirectory))
                {
                    try
                    {
                        if (!File.GetAttributes(childDirectory).HasFlag(FileAttributes.ReparsePoint)) directories.Push(childDirectory);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { skippedDirectoryCount++; }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                skippedDirectoryCount++;
            }
        }
    }

    private bool TryCreateAsset(string rootPath, string filePath, out MediaAsset asset)
    {
        asset = null!;
        var extension = Path.GetExtension(filePath);
        var isVideo = VideoExtensions.Contains(extension);
        var isImage = ImageExtensions.Contains(extension);

        if (!isVideo && !isImage)
        {
            return false;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(rootPath, filePath));
            var location = string.IsNullOrWhiteSpace(relativeDirectory) || relativeDirectory == "."
                ? "根目录"
                : relativeDirectory;
            var detail = $"{(isVideo ? "视频" : "图片")} · {FormatFileSize(fileInfo.Length)} · {location}";
            asset = new MediaAsset(fileInfo.FullName, fileInfo.Name, detail, classifier.Suggest(fileInfo.FullName));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{size:0} {units[unitIndex]}" : $"{size:0.#} {units[unitIndex]}";
    }
}
