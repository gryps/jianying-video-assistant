using System.Text.Json;
using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public sealed class JsonProjectStore(string storagePath) : IProjectStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public ProjectSnapshot? Load()
    {
        if (!File.Exists(storagePath)) return null;
        var snapshot = JsonSerializer.Deserialize<ProjectSnapshot>(File.ReadAllText(storagePath), SerializerOptions);
        return snapshot?.Version == ProjectSnapshot.CurrentVersion ? snapshot : null;
    }

    public void Save(ProjectSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(storagePath)
            ?? throw new InvalidOperationException("项目保存路径无效。");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{storagePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(snapshot, SerializerOptions));
            File.Move(temporaryPath, storagePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public void Clear()
    {
        if (File.Exists(storagePath)) File.Delete(storagePath);
    }
}
