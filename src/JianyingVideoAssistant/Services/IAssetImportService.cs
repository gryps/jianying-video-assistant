using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IAssetImportService
{
    Task<AssetImportResult> ImportFolderAsync(string folderPath);
    Task<AssetImportResult> ImportPathsAsync(IReadOnlyList<string> paths);
}

public sealed record AssetImportResult(
    string FolderPath,
    IReadOnlyList<MediaAsset> Assets,
    int SkippedDirectoryCount);
