namespace JianyingVideoAssistant.Models;

public sealed record MusicTrack(
    string Id,
    string FullPath,
    string Name,
    string Mood,
    string Detail);

public static class MusicMoods
{
    public static IReadOnlyList<string> All { get; } = ["全部", "轻快", "温暖", "高级", "舒缓", "动感", "其他"];
}
