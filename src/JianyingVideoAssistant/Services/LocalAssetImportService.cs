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
        return Task.Run(() => ScanFolder(folderPath));
    }

    private AssetImportResult ScanFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"找不到素材文件夹：{folderPath}");
        }

        var assets = new List<MediaAsset>();
        var directories = new Stack<string>();
        var skippedDirectoryCount = 0;
        directories.Push(folderPath);

        while (directories.TryPop(out var currentDirectory))
        {
            try
            {
                foreach (var filePath in Directory.EnumerateFiles(currentDirectory))
                {
                    if (TryCreateAsset(folderPath, filePath, out var asset))
                    {
                        assets.Add(asset);
                    }
                }

                foreach (var childDirectory in Directory.EnumerateDirectories(currentDirectory))
                {
                    try
                    {
                        var attributes = File.GetAttributes(childDirectory);
                        if (!attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            directories.Push(childDirectory);
                        }
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        skippedDirectoryCount++;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                skippedDirectoryCount++;
            }
        }

        assets.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
        return new AssetImportResult(folderPath, assets, skippedDirectoryCount);
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
