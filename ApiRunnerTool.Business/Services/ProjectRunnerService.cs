using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Business.Models;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class ProjectRunnerService : IProjectRunnerService, IDisposable
    {
        private readonly ILogStreamService _logStream;
        private readonly IProjectConfigRepository _projectConfigRepo;
        private ProjectStatus _currentStatus = new();
        private Process? _process;
        private CancellationTokenSource? _logCts;

        public ProjectRunnerService(ILogStreamService logStream, IProjectConfigRepository projectConfigRepo)
        {
            _logStream = logStream;
            _projectConfigRepo = projectConfigRepo;
        }

        public ProjectStatus GetStatus() => _currentStatus;

        public async Task<ProjectStatus> StartAsync(string folderPath)
        {
            if (_currentStatus.Status == "Running" || _currentStatus.Status == "Starting")
            {
                await StopAsync();
            }

            _logStream.ClearLogs();
            await _logStream.WriteLogAsync($"Yêu cầu chạy dự án tại thư mục: {folderPath}");

            if (!Directory.Exists(folderPath))
            {
                _currentStatus = new ProjectStatus
                {
                    FolderPath = folderPath,
                    Status = "Failed",
                    Error = "Thư mục không tồn tại."
                };
                await _logStream.WriteLogAsync($"[ERROR] Thư mục không tồn tại: {folderPath}");
                return _currentStatus;
            }

            // Tìm file .csproj
            var csprojFiles = Directory.GetFiles(folderPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                _currentStatus = new ProjectStatus
                {
                    FolderPath = folderPath,
                    Status = "Failed",
                    Error = "Không tìm thấy file .csproj trong thư mục."
                };
                await _logStream.WriteLogAsync($"[ERROR] Không tìm thấy file .csproj");
                return _currentStatus;
            }

            var csprojPath = csprojFiles[0];
            var projectName = Path.GetFileNameWithoutExtension(csprojPath);
            await _logStream.WriteLogAsync($"Đã phát hiện dự án: {projectName} (.csproj: {Path.GetFileName(csprojPath)})");

            // Tạo thư mục tạm để Clone bên trong workspace
            var guid = Guid.NewGuid().ToString("N");
            var workspaceDir = Directory.GetCurrentDirectory(); // g:\Ky_8_FPT\PRN232\project_PRN232\ApiRunnerTool
            var cloneBaseDir = Path.Combine(workspaceDir, "cloned_runs");
            if (!Directory.Exists(cloneBaseDir))
            {
                Directory.CreateDirectory(cloneBaseDir);
            }
            var clonedFolderPath = Path.Combine(cloneBaseDir, $"{projectName}_{guid}");

            await _logStream.WriteLogAsync($"Đang clone dự án để bảo vệ bài gốc của học sinh...");
            await _logStream.WriteLogAsync($"Thư mục clone: {clonedFolderPath}");

            try
            {
                CopyDirectory(folderPath, clonedFolderPath);
                await _logStream.WriteLogAsync($"Clone dự án thành công (đã bỏ qua bin/obj/.vs)!");
            }
            catch (Exception ex)
            {
                _currentStatus = new ProjectStatus
                {
                    FolderPath = folderPath,
                    Status = "Failed",
                    Error = $"Lỗi clone thư mục: {ex.Message}"
                };
                await _logStream.WriteLogAsync($"[ERROR] Lỗi clone: {ex.Message}");
                return _currentStatus;
            }

            // Tìm file csproj trong cloned folder
            var clonedCsprojFiles = Directory.GetFiles(clonedFolderPath, "*.csproj", SearchOption.AllDirectories);
            if (clonedCsprojFiles.Length == 0)
            {
                _currentStatus = new ProjectStatus
                {
                    FolderPath = folderPath,
                    ClonedPath = clonedFolderPath,
                    Status = "Failed",
                    Error = "Không tìm thấy file .csproj trong thư mục đã clone."
                };
                await _logStream.WriteLogAsync($"[ERROR] Không tìm thấy csproj trong clone");
                return _currentStatus;
            }
            var clonedCsprojPath = clonedCsprojFiles[0];
            var clonedProjectFolder = Path.GetDirectoryName(clonedCsprojPath)!;

            // Tìm cổng rảnh
            int port = GetFreePort();
            await _logStream.WriteLogAsync($"Đã cấp cổng chạy an toàn cho dự án: {port}");

            // Cập nhật launchSettings.json của cloned project để dùng cổng này
            var propertiesDir = Path.Combine(clonedProjectFolder, "Properties");
            var launchSettingsPath = Path.Combine(propertiesDir, "launchSettings.json");
            try
            {
                if (!Directory.Exists(propertiesDir))
                {
                    Directory.CreateDirectory(propertiesDir);
                }

                // Cấu hình launchSettings mới
                var launchSettingsObj = new
                {
                    profiles = new
                    {
                        http = new
                        {
                            commandName = "Project",
                            dotnetRunMessages = true,
                            launchBrowser = false,
                            applicationUrl = $"http://localhost:{port}",
                            environmentVariables = new
                            {
                                ASPNETCORE_ENVIRONMENT = "Development"
                            }
                        }
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(launchSettingsPath, JsonSerializer.Serialize(launchSettingsObj, options));
                await _logStream.WriteLogAsync($"Đã ghi đè cấu hình cổng mạng trong clone launchSettings.json");
            }
            catch (Exception ex)
            {
                await _logStream.WriteLogAsync($"[WARN] Không thể cập nhật launchSettings.json: {ex.Message}. Sẽ dùng override qua CommandLine/Environment.");
            }

            // Lưu cấu hình vào danh sách vừa dùng
            await _projectConfigRepo.AddOrUpdateAsync(new ProjectConfigEntity
            {
                FolderPath = folderPath,
                ProjectName = projectName
            });

            // Tiến hành build và chạy dự án cloned
            _currentStatus = new ProjectStatus
            {
                FolderPath = folderPath,
                ClonedPath = clonedFolderPath,
                ProjectName = projectName,
                Status = "Starting",
                ActivePort = port,
                StartedAt = DateTime.UtcNow,
                Message = "Đang khởi động dự án..."
            };

            _logCts = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{clonedCsprojPath}\" --no-launch-profile",
                WorkingDirectory = clonedProjectFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Override URL để đảm bảo 100% chạy trên cổng rảnh
            startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://localhost:{port}";
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

            try
            {
                _process = new Process { StartInfo = startInfo };
                _process.Start();

                // Đọc luồng Output & Error bất đồng bộ
                _ = Task.Run(() => ReadStreamAsync(_process.StandardOutput, false, _logCts.Token));
                _ = Task.Run(() => ReadStreamAsync(_process.StandardError, true, _logCts.Token));

                // Đợi kiểm tra cổng mạng của dự án có lắng nghe thành công hay không
                await _logStream.WriteLogAsync("Đang kết nối tới dự án...");
                bool isRunning = await WaitForPortAsync(port, 15); // timeout 15s

                if (isRunning && !_process.HasExited)
                {
                    _currentStatus.Status = "Running";
                    _currentStatus.Message = $"Dự án đang chạy trên cổng {port}";
                    await _logStream.WriteLogAsync($"[SUCCESS] Dự án '{projectName}' đang chạy cực kỳ an toàn tại địa chỉ: http://localhost:{port}");
                }
                else
                {
                    _currentStatus.Status = "Failed";
                    _currentStatus.Error = "Dự án khởi động quá lâu hoặc tự tắt ngay khi chạy.";
                    await _logStream.WriteLogAsync($"[ERROR] Không thể kết nối tới cổng {port}.");
                    await StopAsync();
                }
            }
            catch (Exception ex)
            {
                _currentStatus.Status = "Failed";
                _currentStatus.Error = ex.Message;
                await _logStream.WriteLogAsync($"[ERROR] Lỗi khởi động tiến trình: {ex.Message}");
                await StopAsync();
            }

            return _currentStatus;
        }

        public async Task<ProjectStatus> StopAsync()
        {
            if (_process != null)
            {
                try
                {
                    await _logStream.WriteLogAsync("Đang tắt dự án đang test...");
                    _logCts?.Cancel();

                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                        await _logStream.WriteLogAsync("Đã tắt tiến trình dự án học sinh và dọn dẹp các tiến trình con liên quan.");
                    }
                }
                catch (Exception ex)
                {
                    await _logStream.WriteLogAsync($"[WARN] Lỗi khi tắt tiến trình: {ex.Message}");
                }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }

            // Dọn dẹp thư mục clone tạm thời để tránh tốn dung lượng
            if (!string.IsNullOrEmpty(_currentStatus.ClonedPath) && Directory.Exists(_currentStatus.ClonedPath))
            {
                try
                {
                    await _logStream.WriteLogAsync($"Đang dọn dẹp thư mục clone tạm thời: {_currentStatus.ClonedPath}");
                    DeleteDirectoryWithRetries(_currentStatus.ClonedPath, 3);
                    await _logStream.WriteLogAsync("Đã xoá sạch thư mục tạm của clone thành công!");
                }
                catch (Exception ex)
                {
                    await _logStream.WriteLogAsync($"[WARN] Không thể xoá thư mục clone tạm thời: {ex.Message}");
                }
            }

            _currentStatus = new ProjectStatus
            {
                FolderPath = _currentStatus.FolderPath,
                ProjectName = _currentStatus.ProjectName,
                Status = "Stopped",
                Message = "Dự án đã dừng."
            };

            return _currentStatus;
        }

        private async Task ReadStreamAsync(StreamReader reader, bool isError, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                if (!string.IsNullOrEmpty(line))
                {
                    var prefix = isError ? "[ERROR] " : "";
                    await _logStream.WriteLogAsync($"{prefix}{line}");
                }
            }
        }

        private async Task<bool> WaitForPortAsync(int port, int timeoutSeconds)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    var connectTask = tcpClient.ConnectAsync("127.0.0.1", port);
                    // Chờ kết nối hoặc timeout
                    var completedTask = await Task.WhenAny(connectTask, Task.Delay(500, cts.Token));
                    if (completedTask == connectTask)
                    {
                        await connectTask; // ném exception nếu thất bại
                        return true;
                    }
                }
                catch
                {
                    // Tiếp tục đợi
                }
                await Task.Delay(500, cts.Token);
            }
            return false;
        }

        private static int GetFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destSubDir = Path.Combine(destinationDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        private static void DeleteDirectoryWithRetries(string path, int retries)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                    return;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
            // Lần cuối cố gắng đổi quyền hoặc xoá thô
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public void Dispose()
        {
            _logCts?.Cancel();
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch { }
                _process.Dispose();
            }

            if (!string.IsNullOrEmpty(_currentStatus.ClonedPath) && Directory.Exists(_currentStatus.ClonedPath))
            {
                try
                {
                    Directory.Delete(_currentStatus.ClonedPath, true);
                }
                catch { }
            }
        }
    }
}
