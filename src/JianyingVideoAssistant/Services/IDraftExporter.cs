namespace JianyingVideoAssistant.Services;

public interface IDraftExporter
{
    Task<DraftExportResult> ExportPreviewAsync(
        DraftExportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DraftExportRequest(
    string ProjectName,
    IReadOnlyList<string> MediaPaths,
    string OutputDirectory);

public sealed record DraftExportResult(
    bool Succeeded,
    string? PreviewPath,
    IReadOnlyList<string> Messages);
