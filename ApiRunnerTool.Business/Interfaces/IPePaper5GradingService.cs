using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IPePaper5GradingService
    {
        Task<PeGradingResult> GradeStudentAsync(string studentName, string folderPath, int activePort, string projectStatus);
        Task<PeBatchGradingSummary> GradeAllRunningAsync(System.Collections.Generic.IReadOnlyList<(string studentName, string folderPath, int port, string status)> students);
        string BuildExportCsv(PeBatchGradingSummary summary);
    }
}
