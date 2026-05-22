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

            try
            {
                // 1. Khởi chạy dự án tuần tự
                await _batchRunner.LaunchStudentByNameAsync(studentName);

                // Lấy lại thông tin sinh viên sau khi đã được cập nhật port/status
                student = session.Students.Find(s => s.StudentName == studentName)!;

                // 2. Chấm bài
                var folder = !string.IsNullOrEmpty(student.FolderPath) ? student.FolderPath : student.ClonedPath;
                var result = await _peGrading.GradeStudentAsync(
                    student.StudentName, folder, student.ActivePort, student.Status);

                // Lưu kết quả chấm vào cache
                _batchRunner.SavePeGrade(studentName, result);

                return Ok(result);
            }
            finally
            {
                // 3. Tắt dự án ngay sau khi chấm xong
                await _batchRunner.StopStudentAsync(studentName);
            }
        }

        /// <summary>POST /api/batch/pe5/grade-all</summary>
        [HttpPost("grade-all")]
        public async Task<IActionResult> GradeAll()
        {
            var session = _batchRunner.GetSession();
            if (session.Students.Count == 0)
                return BadRequest(new { message = "Chưa có phiên chấm — hãy chọn thư mục và chạy batch trước." });

            var summary = new PeBatchGradingSummary();

            foreach (var s in session.Students.ToList())
            {
                try
                {
                    // 1. Khởi chạy dự án của từng học sinh
                    await _batchRunner.LaunchStudentByNameAsync(s.StudentName);

                    // Lấy thông tin sinh viên đã cập nhật
                    var student = session.Students.Find(x => x.StudentName == s.StudentName)!;

                    // 2. Chấm bài
                    var folder = !string.IsNullOrEmpty(student.FolderPath) ? student.FolderPath : student.ClonedPath;
                    var r = await _peGrading.GradeStudentAsync(
                        student.StudentName, folder, student.ActivePort, student.Status);

                    // Lưu cache kết quả chấm
                    _batchRunner.SavePeGrade(s.StudentName, r);

                    summary.Results.Add(r);
                    summary.GradedCount++;
                }
                catch (Exception ex)
                {
                    summary.FailedCount++;
                    var failedResult = new PeGradingResult
                    {
                        StudentName = s.StudentName,
                        Message = $"Lỗi chấm bài: {ex.Message}"
                    };
                    summary.Results.Add(failedResult);
                    _batchRunner.SavePeGrade(s.StudentName, failedResult);
                }
                finally
                {
                    // 3. Tắt dự án ngay sau khi chấm xong
                    await _batchRunner.StopStudentAsync(s.StudentName);
                }
            }

            return Ok(summary);
        }

        /// <summary>GET /api/batch/pe5/export — CSV từ kết quả grade-all gần nhất hoặc chạy chấm rồi export</summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportCsv()
        {
            var session = _batchRunner.GetSession();
            if (session.Students.Count == 0)
                return BadRequest(new { message = "Không có dữ liệu để xuất." });

            var grades = _batchRunner.GetAllPeGrades();
            var summary = new PeBatchGradingSummary();

            if (grades.Count == 0)
            {
                // Fallback: Nếu chưa chấm tất cả, thực hiện chấm tuần tự
                foreach (var s in session.Students.ToList())
                {
                    try
                    {
                        await _batchRunner.LaunchStudentByNameAsync(s.StudentName);
                        var student = session.Students.Find(x => x.StudentName == s.StudentName)!;
                        var folder = !string.IsNullOrEmpty(student.FolderPath) ? student.FolderPath : student.ClonedPath;
                        var r = await _peGrading.GradeStudentAsync(
                            student.StudentName, folder, student.ActivePort, student.Status);
                        _batchRunner.SavePeGrade(s.StudentName, r);
                        summary.Results.Add(r);
                        summary.GradedCount++;
                    }
                    catch (Exception ex)
                    {
                        summary.FailedCount++;
                        var failedResult = new PeGradingResult { StudentName = s.StudentName, Message = ex.Message };
                        summary.Results.Add(failedResult);
                        _batchRunner.SavePeGrade(s.StudentName, failedResult);
                    }
                    finally
                    {
                        await _batchRunner.StopStudentAsync(s.StudentName);
                    }
                }
            }
            else
            {
                summary.Results.AddRange(grades);
                summary.GradedCount = grades.Count;
            }

            var csv = _peGrading.BuildExportCsv(summary);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"PE_Paper5_Grades_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}
