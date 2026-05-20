using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
                // Tạo script PowerShell tạm thời để mở Windows Folder Picker
                var tempScriptPath = Path.Combine(Path.GetTempPath(), "apirunner_browse.ps1");
                var scriptContent = @"
Add-Type -AssemblyName System.Windows.Forms
$dialog = New-Object System.Windows.Forms.FolderBrowserDialog
$dialog.Description = 'Chọn thư mục chứa dự án API của học sinh'
$dialog.ShowNewFolderButton = $false
$result = $dialog.ShowDialog((New-Object System.Windows.Forms.Form -Property @{TopMost=$true}))
if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
    Write-Output $dialog.SelectedPath
} else {
    Write-Output 'CANCELLED'
}
";
                System.IO.File.WriteAllText(tempScriptPath, scriptContent);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var readTask = process.StandardOutput.ReadToEndAsync();
                if (process.WaitForExit(45000)) // timeout 45s
                {
                    var output = readTask.Result.Trim();
                    try { System.IO.File.Delete(tempScriptPath); } catch { }

                    if (string.IsNullOrEmpty(output) || output == "CANCELLED")
                    {
                        return Ok(new { cancelled = true });
                    }
                    return Ok(new { path = output, cancelled = false });
                }
                else
                {
                    try { process.Kill(true); } catch { }
                    try { System.IO.File.Delete(tempScriptPath); } catch { }
                    return Ok(new { cancelled = true, error = "Timeout" });
                }
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
