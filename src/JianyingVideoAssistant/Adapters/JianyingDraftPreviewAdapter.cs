using System.Text;
using System.Text.Json;
using JianyingVideoAssistant.Services;

namespace JianyingVideoAssistant.Adapters;

public sealed class JianyingDraftPreviewAdapter(string workingRoot) : IDraftExporter
{
    private readonly string _workingRoot = Path.GetFullPath(
        string.IsNullOrWhiteSpace(workingRoot)
            ? throw new ArgumentException("草稿工作目录不能为空。", nameof(workingRoot))
            : workingRoot);

    public async Task<DraftExportResult> ExportPreviewAsync(
        DraftExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var messages = Validate(request);
        if (messages.Count > 0)
        {
            return new DraftExportResult(false, null, messages);
        }

        Directory.CreateDirectory(_workingRoot);
        var safeName = SanitizeFileName(request.ProjectName);
        var shortName = safeName.Length > 60 ? safeName[..60] : safeName;
        var folderName = $"{shortName}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..(shortName.Length + 25)];
        var previewPath = Path.Combine(_workingRoot, folderName);
        Directory.CreateDirectory(previewPath);

        var manifest = new
        {
            format = "jianying-video-assistant-preview-v1",
            notice = "这是待适配的项目预览，不是真实剪映草稿。",
            projectName = request.ProjectName,
            createdAt = DateTimeOffset.Now,
            media = request.Media.Select(item => new { path = item.FullPath, category = item.Category }),
            script = request.Script.Select((segment, index) => new
            {
                order = index + 1,
                purpose = segment.Purpose,
                text = segment.Text
            }),
            music = request.Music is null
                ? null
                : new { path = request.Music.FullPath, name = request.Music.Name, mood = request.Music.Mood }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var manifestPath = Path.Combine(previewPath, "project-preview.json");
        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, options),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(previewPath, "说明.txt"),
            "此目录由剪映视频助手生成，仅用于核对素材、文案和音乐。\r\n它不会覆盖或修改任何已有剪映草稿。\r\n取得剪映草稿样本并完成格式适配后，才会生成可由剪映直接打开的副本。\r\n",
            Encoding.UTF8,
            cancellationToken);

        return new DraftExportResult(
            true,
            previewPath,
            ["已生成安全项目预览副本；未访问或修改剪映草稿目录。"]);
    }

    private static List<string> Validate(DraftExportRequest request)
    {
        var messages = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ProjectName))
        {
            messages.Add("项目名称不能为空。");
        }

        if (request.Media.Count == 0)
        {
            messages.Add("当前项目没有素材。");
        }

        var missingCount = request.Media.Count(item => !File.Exists(item.FullPath));
        if (missingCount > 0)
        {
            messages.Add($"有 {missingCount} 个素材文件已移动或不存在。");
        }

        if (request.Music is not null && !File.Exists(request.Music.FullPath))
        {
            messages.Add("所选背景音乐文件已移动或不存在。");
        }

        return messages;
    }

    private static string SanitizeFileName(string projectName)
    {
        var sanitized = string.Concat(projectName.Trim().Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return string.IsNullOrWhiteSpace(sanitized) ? "未命名视频项目" : sanitized;
    }
}
