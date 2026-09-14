namespace JianyingVideoAssistant.Models;

public sealed record DraftCheckItem(string Title, string Detail, bool IsBlocking)
{
    public string StatusLabel => IsBlocking ? "需要处理" : "通过";
    public string StatusColor => IsBlocking ? "#FFCC66" : "#4FD1A1";
}

public sealed record DraftMediaItem(string FullPath, string Category);
