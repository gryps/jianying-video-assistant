using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IDraftExporter
{
    Task<DraftExportResult> ExportPreviewAsync(
        DraftExportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DraftExportRequest(
    string ProjectName,
    IReadOnlyList<DraftMediaItem> Media,
    IReadOnlyList<ProjectScriptSegment> Script,
    MusicTrack? Music);

public sealed record DraftExportResult(
    bool Succeeded,
    string? PreviewPath,
    IReadOnlyList<string> Messages);
