using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiRunnerTool.API.Helpers;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.API.Controllers
{
    [ApiController]
    [Route("api/batch")]
    public class BatchController : ControllerBase
    {
        private readonly IBatchRunnerService _batchRunner;
        private readonly ISwaggerScannerService _swaggerScanner;
        private readonly IApiExecutorService _apiExecutor;

        public BatchController(
            IBatchRunnerService batchRunner,
            ISwaggerScannerService swaggerScanner,
            IApiExecutorService apiExecutor)
        {
            _batchRunner = batchRunner;
            _swaggerScanner = swaggerScanner;
            _apiExecutor = apiExecutor;
        }

        /// <summary>GET /api/batch/session — get current batch session status</summary>
        [HttpGet("session")]
        public IActionResult GetSession()
        {
            return Ok(_batchRunner.GetSession());
        }

        /// <summary>POST /api/batch/start — discover student subfolders and launch all</summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartBatch([FromBody] StartBatchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ParentFolderPath))
                return BadRequest(new { message = "Duong dan thu muc cha khong duoc de trong." });

            var session = await _batchRunner.StartBatchAsync(request.ParentFolderPath);
            return Ok(session);
        }

        /// <summary>POST /api/batch/stop — stop all running student projects</summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopAll()
        {
            var session = await _batchRunner.StopAllAsync();
            return Ok(session);
        }

        /// <summary>POST /api/batch/stop/{studentName} — stop a single student's project</summary>
        [HttpPost("stop/{studentName}")]
        public async Task<IActionResult> StopStudent(string studentName)
        {
            var session = await _batchRunner.StopStudentAsync(studentName);
            return Ok(session);
        }

        /// <summary>POST /api/batch/scan/{studentName} — scan endpoints for one student</summary>
        [HttpPost("scan/{studentName}")]
        public async Task<IActionResult> ScanStudent(string studentName)
        {
            var session = _batchRunner.GetSession();
            var student = session.Students.Find(s => s.StudentName == studentName);
            if (student == null)
                return NotFound(new { message = $"Khong tim thay hoc sinh: {studentName}" });
            if (student.Status != "Running")
                return BadRequest(new { message = $"Du an cua {studentName} chua chay." });

            var endpoints = await _swaggerScanner.ScanAsync(student.ActivePort);
            return Ok(endpoints);
        }

        /// <summary>POST /api/batch/test/{studentName} — test all endpoints for one student</summary>
        [HttpPost("test/{studentName}")]
        public async Task<IActionResult> TestStudent(string studentName, [FromBody] List<EndpointInfo> endpoints)
        {
            var session = _batchRunner.GetSession();
            var student = session.Students.Find(s => s.StudentName == studentName);
            if (student == null)
                return NotFound(new { message = $"Khong tim thay hoc sinh: {studentName}" });
            if (student.Status != "Running")
                return BadRequest(new { message = $"Du an cua {studentName} chua chay." });

            var results = new List<ApiResponse>();
            foreach (var ep in endpoints)
            {
                var result = await _apiExecutor.ExecuteAsync(student.ActivePort, ep);
                results.Add(result);
            }

            // Build a minimal history-like object to return
            var passed = results.FindAll(r => r.IsSuccess).Count;
            var score = endpoints.Count > 0 ? Math.Round((double)passed / endpoints.Count * 10, 1) : 0.0;

            return Ok(new
            {
                studentName,
                projectName = student.ProjectName,
                port = student.ActivePort,
                results,
                passed,
                failed = results.Count - passed,
                total = results.Count,
                score
            });
        }

        /// <summary>GET /api/batch/browse — open native folder picker (same as project browse)</summary>
        [HttpGet("browse")]
        public IActionResult BrowseFolder()
        {
            try
            {
                var (cancelled, path, error) = NativeFolderPicker.Pick(
                    "Chọn thư mục CHA chứa tất cả bài nộp (mỗi thư mục con = 1 sinh viên)");
                if (cancelled)
                    return Ok(new { cancelled = true, error });
                return Ok(new { path, cancelled = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi mở Folder Picker: {ex.Message}" });
            }
        }
    }

    public class StartBatchRequest
    {
        public string ParentFolderPath { get; set; } = string.Empty;
    }
}
