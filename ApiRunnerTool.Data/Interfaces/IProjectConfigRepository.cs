using System.Collections.Generic;
using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Data.Interfaces
{
    public interface IProjectConfigRepository
    {
        Task AddOrUpdateAsync(ProjectConfigEntity config);
        Task<List<ProjectConfigEntity>> GetRecentProjectsAsync();
        Task DeleteAsync(string folderPath);
    }
}
