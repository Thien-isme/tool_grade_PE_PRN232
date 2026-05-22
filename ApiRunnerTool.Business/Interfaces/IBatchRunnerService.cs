using System.Collections.Generic;
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
        Task<bool> LaunchStudentByNameAsync(string studentName);
        void SavePeGrade(string studentName, PeGradingResult result);
        PeGradingResult? GetPeGrade(string studentName);
        List<PeGradingResult> GetAllPeGrades();
        void ClearPeGrades();
    }
}
