namespace JianyingVideoAssistant.Models;

public sealed record DraftCheckItem(string Title, string Detail, bool IsBlocking)
{
    public string StatusLabel => IsBlocking ? "需要处理" : "通过";
}

public sealed record DraftMediaItem(string FullPath, string Category);
