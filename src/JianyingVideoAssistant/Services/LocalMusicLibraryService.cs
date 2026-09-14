using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public sealed class LocalMusicLibraryService : IMusicLibraryService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".wma", ".flac", ".ogg"
    };

    private static readonly (string Mood, string[] Keywords)[] MoodRules =
    [
        ("轻快", ["轻快", "快乐", "清新", "欢快", "happy", "bright", "fresh"]),
        ("温暖", ["温暖", "治愈", "温馨", "warm", "healing"]),
        ("高级", ["高级", "时尚", "质感", "奢华", "fashion", "luxury"]),
        ("舒缓", ["舒缓", "安静", "放松", "慢", "calm", "soft", "slow"]),
        ("动感", ["动感", "节奏", "运动", "快", "beat", "sport", "fast"])
    ];

    public IReadOnlyList<MusicTrack> ImportFiles(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        return filePaths
            .Where(path => !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path))
                && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(CreateTrack)
            .OrderBy(track => track.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static MusicTrack CreateTrack(string path)
    {
        var info = new FileInfo(path);
        var searchable = Path.ChangeExtension(info.FullName, null).ToLowerInvariant();
        var mood = MoodRules.FirstOrDefault(rule => rule.Keywords.Any(searchable.Contains)).Mood ?? "其他";
        return new MusicTrack(
            info.FullName.ToUpperInvariant(),
            info.FullName,
            info.Name,
            mood,
            $"{FormatFileSize(info.Length)} · {info.Directory?.Name ?? "本地文件"}");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var index = 0;
        while (size >= 1024 && index < units.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return index == 0 ? $"{size:0} {units[index]}" : $"{size:0.#} {units[index]}";
    }
}
