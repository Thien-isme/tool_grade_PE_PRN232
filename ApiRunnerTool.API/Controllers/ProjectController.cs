using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiRunnerTool.API.Helpers;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Business.Models;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.API.Controllers
{
    [ApiController]
    [Route("api/project")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectRunnerService _runnerService;
        private readonly IProjectConfigRepository _configRepo;

        public ProjectController(IProjectRunnerService runnerService, IProjectConfigRepository configRepo)
        {
            _runnerService = runnerService;
            _configRepo = configRepo;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(_runnerService.GetStatus());
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartProject([FromBody] StartProjectRequest request)
        {
            if (string.IsNullOrEmpty(request.FolderPath))
            {
                return BadRequest(new { message = "Đường dẫn thư mục không được để trống." });
            }

            var status = await _runnerService.StartAsync(request.FolderPath);
            return Ok(status);
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopProject()
        {
            var status = await _runnerService.StopAsync();
            return Ok(status);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var list = await _configRepo.GetRecentProjectsAsync();
            return Ok(list);
        }

        [HttpGet("browse")]
        public IActionResult BrowseFolder()
        {
            try
            {
                var (cancelled, path, error) = NativeFolderPicker.Pick(
                    "Chọn thư mục chứa dự án API của học sinh");
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

    public class StartProjectRequest
    {
        public string FolderPath { get; set; } = string.Empty;
    }
}
