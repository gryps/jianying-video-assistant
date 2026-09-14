using JianyingVideoAssistant.Models;

namespace JianyingVideoAssistant.Services;

public interface IProjectStore
{
    ProjectSnapshot? Load();
    void Save(ProjectSnapshot snapshot);
    void Clear();
}
