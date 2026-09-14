using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IContentLibraryService
{
    IReadOnlyList<ContentSnippet> Search(string? query, string? purpose);
}
