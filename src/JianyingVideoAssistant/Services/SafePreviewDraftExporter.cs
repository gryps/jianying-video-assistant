namespace JianyingVideoAssistant.Services;

public sealed class SafePreviewDraftExporter : IDraftExporter
{
    public Task<DraftExportResult> ExportPreviewAsync(
        DraftExportRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ProjectName))
        {
            messages.Add("项目名称不能为空。");
        }

        if (request.MediaPaths.Count == 0)
        {
            messages.Add("当前项目没有素材。");
        }

        if (messages.Count > 0)
        {
            return Task.FromResult(new DraftExportResult(false, null, messages));
        }

        // 真实剪映格式尚未接入。这里只返回应用工作目录中的预览目标，绝不触碰用户草稿。
        var safeName = string.Concat(request.ProjectName.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        var previewPath = Path.Combine(request.OutputDirectory, $"{safeName}-preview");

        return Task.FromResult(new DraftExportResult(
            true,
            previewPath,
            ["预览检查通过；未写入任何剪映草稿文件。"]));
    }
}
