namespace JianyingVideoAssistant.Services;

public interface IMusicPreviewService
{
    string? PlayingPath { get; }
    Task PlayAsync(string filePath);
    void Stop();
}
