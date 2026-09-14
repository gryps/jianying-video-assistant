namespace JianyingVideoAssistant.Models;

public sealed record ContentSnippet(
    string Id,
    string Purpose,
    string Text,
    IReadOnlyList<string> Keywords)
{
    public string KeywordLabel => string.Join(" · ", Keywords);
}

public sealed record ProjectScriptSegment(
    string SourceId,
    string Purpose,
    string Text);
