using System.Windows.Media;

namespace JianyingVideoAssistant.Services;

public sealed class WpfMusicPreviewService : IMusicPreviewService, IDisposable
{
    private readonly MediaPlayer _player = new();

    public string? PlayingPath { get; private set; }

    public Task PlayAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        Stop();
        _player.Open(new Uri(filePath, UriKind.Absolute));
        _player.Volume = 0.65;
        _player.Play();
        PlayingPath = filePath;
        return Task.CompletedTask;
    }

    public void Stop()
    {
        _player.Stop();
        _player.Close();
        PlayingPath = null;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
