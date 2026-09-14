using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IMusicLibraryService
{
    IReadOnlyList<MusicTrack> ImportFiles(IEnumerable<string> filePaths);
}
