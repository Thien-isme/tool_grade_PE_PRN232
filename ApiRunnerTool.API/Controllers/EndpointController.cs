using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.API.Controllers
{
    [ApiController]
    [Route("api/endpoints")]
    public class EndpointController : ControllerBase
    {
        private readonly IProjectRunnerService _runnerService;
        private readonly ISwaggerScannerService _swaggerScanner;
        private readonly IApiExecutorService _apiExecutor;
        private readonly IRunHistoryRepository _historyRepo;
        private readonly ILogStreamService _logStream;

        public EndpointController(
            IProjectRunnerService runnerService,
            ISwaggerScannerService swaggerScanner,
            IApiExecutorService apiExecutor,
            IRunHistoryRepository historyRepo,
            ILogStreamService logStream)
        {
            _runnerService = runnerService;
            _swaggerScanner = swaggerScanner;
            _apiExecutor = apiExecutor;
            _historyRepo = historyRepo;
            _logStream = logStream;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> ScanEndpoints()
        {
            var status = _runnerService.GetStatus();
            if (status.Status != "Running")
            {
                return BadRequest(new { message = "Dự án hiện tại chưa chạy. Vui lòng chạy dự án trước khi quét." });
            }

            var endpoints = await _swaggerScanner.ScanAsync(status.ActivePort);
            return Ok(endpoints);
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestEndpoints([FromBody] List<EndpointInfo> endpoints)
        {
            var status = _runnerService.GetStatus();
            if (status.Status != "Running")
            {
                return BadRequest(new { message = "Dự án hiện tại chưa chạy." });
            }

            if (endpoints == null || endpoints.Count == 0)
            {
                return BadRequest(new { message = "Không tìm thấy danh sách endpoint để kiểm tra." });
            }

            await _logStream.WriteLogAsync($"=== BẮT ĐẦU CHẠY THỬ NGHIỆM TỰ ĐỘNG ({endpoints.Count} Endpoints) ===");

            var history = new RunHistoryEntity
            {
                ProjectPath = status.FolderPath,
                ProjectName = status.ProjectName,
                Port = status.ActivePort,
                StartedAt = DateTime.UtcNow
            };

            foreach (var ep in endpoints)
            {
                var response = await _apiExecutor.ExecuteAsync(status.ActivePort, ep);
                history.Results.Add(response);
            }

            history.StoppedAt = DateTime.UtcNow;
            await _historyRepo.SaveAsync(history);

            await _logStream.WriteLogAsync($"=== HOÀN THÀNH CHẠY THỬ NGHIỆM TỰ ĐỘNG. LỊCH SỬ ĐÃ ĐƯỢC LƯU! ===");
            return Ok(history);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var list = await _historyRepo.GetAllAsync();
            return Ok(list);
        }

        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteHistory(Guid id)
        {
            await _historyRepo.DeleteAsync(id);
            return Ok(new { success = true });
        }
    }
}
