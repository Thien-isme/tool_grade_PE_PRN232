using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IBatchRunnerService
    {
        BatchSessionStatus GetSession();
        Task<BatchSessionStatus> StartBatchAsync(string parentFolderPath);
        Task<BatchSessionStatus> StopAllAsync();
        Task<BatchSessionStatus> StopStudentAsync(string studentName);
    }
}
