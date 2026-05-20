using System.Threading.Tasks;
using ApiRunnerTool.Business.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IProjectRunnerService
    {
        ProjectStatus GetStatus();
        Task<ProjectStatus> StartAsync(string folderPath);
        Task<ProjectStatus> StopAsync();
    }
}
