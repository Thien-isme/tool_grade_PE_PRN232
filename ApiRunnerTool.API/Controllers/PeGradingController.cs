using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.API.Controllers
{
    [ApiController]
    [Route("api/batch/pe5")]
    public class PeGradingController : ControllerBase
    {
        private readonly IBatchRunnerService _batchRunner;
        private readonly IPePaper5GradingService _peGrading;

        public PeGradingController(IBatchRunnerService batchRunner, IPePaper5GradingService peGrading)
        {
            _batchRunner = batchRunner;
            _peGrading = peGrading;
        }

        /// <summary>POST /api/batch/pe5/grade/{studentName}</summary>
        [HttpPost("grade/{studentName}")]
        public async Task<IActionResult> GradeStudent(string studentName)
        {
            var session = _batchRunner.GetSession();
            var student = session.Students.Find(s => s.StudentName == studentName);
            if (student == null)
                return NotFound(new { message = $"Không tìm thấy học sinh: {studentName}" });

            var folder = !string.IsNullOrEmpty(student.FolderPath) ? student.FolderPath : student.ClonedPath;
            var result = await _peGrading.GradeStudentAsync(
                student.StudentName, folder, student.ActivePort, student.Status);
            return Ok(result);
        }

        /// <summary>POST /api/batch/pe5/grade-all</summary>
        [HttpPost("grade-all")]
        public async Task<IActionResult> GradeAll()
        {
            var session = _batchRunner.GetSession();
            if (session.Students.Count == 0)
                return BadRequest(new { message = "Chưa có phiên chấm — hãy chọn thư mục và chạy batch trước." });

            var inputs = session.Students
                .Select(s => (
                    s.StudentName,
                    folderPath: !string.IsNullOrEmpty(s.FolderPath) ? s.FolderPath : s.ClonedPath,
                    port: s.ActivePort,
                    status: s.Status))
                .ToList();

            var summary = await _peGrading.GradeAllRunningAsync(inputs);
            return Ok(summary);
        }

        /// <summary>GET /api/batch/pe5/export — CSV từ kết quả grade-all gần nhất (client gửi body) hoặc grade-all rồi export</summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportCsv()
        {
            var session = _batchRunner.GetSession();
            if (session.Students.Count == 0)
                return BadRequest(new { message = "Không có dữ liệu để xuất." });

            var inputs = session.Students
                .Select(s => (
                    s.StudentName,
                    folderPath: !string.IsNullOrEmpty(s.FolderPath) ? s.FolderPath : s.ClonedPath,
                    port: s.ActivePort,
                    status: s.Status))
                .ToList();

            var summary = await _peGrading.GradeAllRunningAsync(inputs);
            var csv = _peGrading.BuildExportCsv(summary);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"PE_Paper5_Grades_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}
