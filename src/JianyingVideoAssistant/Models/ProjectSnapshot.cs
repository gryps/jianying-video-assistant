namespace JianyingVideoAssistant.Models;

public sealed record ProjectSnapshot(
    int Version,
    string ProjectName,
    IReadOnlyList<SavedMediaAsset> Assets,
    IReadOnlyList<ProjectScriptSegment> ScriptSegments,
    IReadOnlyList<MusicTrack> MusicTracks,
    string? AppliedMusicId,
    string? LastPreviewPath)
{
    public const int CurrentVersion = 1;
}

public sealed record SavedMediaAsset(
    string FullPath,
    string Name,
    string Detail,
    string Category,
    string CategorySourceLabel);
