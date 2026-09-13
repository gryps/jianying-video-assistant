using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IAssetImportService
{
    Task<AssetImportResult> ImportFolderAsync(string folderPath);
}

public sealed record AssetImportResult(
    string FolderPath,
    IReadOnlyList<MediaAsset> Assets,
    int SkippedDirectoryCount);
