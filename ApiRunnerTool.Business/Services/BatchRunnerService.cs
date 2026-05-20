using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    /// <summary>
    /// Manages a batch exam session: scans a parent folder, launches each student project
    /// on an isolated port, and tracks all of them simultaneously.
    /// </summary>
    public class BatchRunnerService : IBatchRunnerService, IDisposable
    {
        private readonly ILogStreamService _logStream;

        // Map studentName -> (process, clonedPath, cts)
        private readonly ConcurrentDictionary<string, (Process Process, string ClonedPath, CancellationTokenSource Cts)> _runningProjects = new();

        private BatchSessionStatus _session = new();

        public BatchRunnerService(ILogStreamService logStream)
        {
            _logStream = logStream;
        }

        public BatchSessionStatus GetSession() => _session;

        /// <summary>
        /// Scans parentFolderPath for subdirectories containing a .csproj, then launches them all.
        /// </summary>
        public async Task<BatchSessionStatus> StartBatchAsync(string parentFolderPath)
        {
            // Stop any existing session first
            await StopAllAsync();

            _logStream.ClearLogs();
            await _logStream.WriteLogAsync($"[BATCH] Bat dau phien cham bai tu: {parentFolderPath}");

            if (!Directory.Exists(parentFolderPath))
            {
                _session = new BatchSessionStatus
                {
                    SessionFolderPath = parentFolderPath,
                    SessionStatus = "Stopped"
                };
                await _logStream.WriteLogAsync($"[ERROR] Thu muc khong ton tai: {parentFolderPath}");
                return _session;
            }

            // Discover student sub-folders (each sub-folder = one student)
            var subDirs = Directory.GetDirectories(parentFolderPath);
            var studentFolders = new List<(string studentName, string folderPath, string csprojPath)>();

            foreach (var dir in subDirs)
            {
                var csprojFiles = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories);
                if (csprojFiles.Length > 0)
                {
                    var studentName = Path.GetFileName(dir); // folder name = student identifier
                    studentFolders.Add((studentName, dir, csprojFiles[0]));
                }
            }

            if (studentFolders.Count == 0)
            {
                await _logStream.WriteLogAsync("[ERROR] Khong tim thay thu muc con nao chua file .csproj.");
                _session = new BatchSessionStatus
                {
                    SessionFolderPath = parentFolderPath,
                    SessionStatus = "Stopped"
                };
                return _session;
            }

            await _logStream.WriteLogAsync($"[BATCH] Tim thay {studentFolders.Count} du an hoc sinh.");

            _session = new BatchSessionStatus
            {
                SessionFolderPath = parentFolderPath,
                SessionStatus = "Starting",
                StartedAt = DateTime.UtcNow,
                Students = studentFolders.Select(sf => new StudentProjectStatus
                {
                    StudentName = sf.studentName,
                    FolderPath = sf.folderPath,
                    ProjectName = Path.GetFileNameWithoutExtension(sf.csprojPath),
                    Status = "Pending"
                }).ToList()
            };

            // Launch each student project in parallel
            var tasks = studentFolders.Select(sf => LaunchStudentAsync(sf.studentName, sf.folderPath, sf.csprojPath));
            await Task.WhenAll(tasks);

            var runningCount = _session.Students.Count(s => s.Status == "Running");
            _session.SessionStatus = runningCount > 0 ? "Running" : "Stopped";
            await _logStream.WriteLogAsync($"[BATCH] Phien cham bai san sang: {runningCount}/{studentFolders.Count} du an dang chay.");

            return _session;
        }

        private async Task LaunchStudentAsync(string studentName, string folderPath, string csprojPath)
        {
            var studentStatus = _session.Students.FirstOrDefault(s => s.StudentName == studentName);
            if (studentStatus == null) return;

            studentStatus.Status = "Starting";
            await _logStream.WriteLogAsync($"[{studentName}] Dang khoi dong du an...");

            try
            {
                // Clone to isolated sandbox
                var workspaceDir = Directory.GetCurrentDirectory();
                var cloneBaseDir = Path.Combine(workspaceDir, "cloned_runs");
                Directory.CreateDirectory(cloneBaseDir);

                var guid = Guid.NewGuid().ToString("N");
                var clonedFolderPath = Path.Combine(cloneBaseDir, $"{studentName}_{guid}");
                CopyDirectory(folderPath, clonedFolderPath);

                studentStatus.ClonedPath = clonedFolderPath;

                // Find csproj in clone
                var clonedCsprojFiles = Directory.GetFiles(clonedFolderPath, "*.csproj", SearchOption.AllDirectories);
                if (clonedCsprojFiles.Length == 0)
                {
                    studentStatus.Status = "Failed";
                    studentStatus.Error = "Khong tim thay .csproj trong clone.";
                    return;
                }

                var clonedCsprojPath = clonedCsprojFiles[0];
                var clonedProjectFolder = Path.GetDirectoryName(clonedCsprojPath)!;

                // Assign free port
                int port = GetFreePort();
                studentStatus.ActivePort = port;

                // Write launchSettings.json to set port
                var propertiesDir = Path.Combine(clonedProjectFolder, "Properties");
                Directory.CreateDirectory(propertiesDir);
                var launchSettingsPath = Path.Combine(propertiesDir, "launchSettings.json");
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
                            environmentVariables = new { ASPNETCORE_ENVIRONMENT = "Development" }
                        }
                    }
                };
                File.WriteAllText(launchSettingsPath, JsonSerializer.Serialize(launchSettingsObj, new JsonSerializerOptions { WriteIndented = true }));

                // Launch process
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
                startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://localhost:{port}";
                startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

                var cts = new CancellationTokenSource();
                var process = new Process { StartInfo = startInfo };
                process.Start();

                _ = Task.Run(() => ReadStreamAsync(process.StandardOutput, studentName, false, cts.Token));
                _ = Task.Run(() => ReadStreamAsync(process.StandardError, studentName, true, cts.Token));

                _runningProjects[studentName] = (process, clonedFolderPath, cts);

                bool isUp = await WaitForPortAsync(port, 90);

                if (isUp && !process.HasExited)
                {
                    studentStatus.Status = "Running";
                    studentStatus.StartedAt = DateTime.UtcNow;
                    studentStatus.Message = $"Dang chay tren cong {port}";
                    await _logStream.WriteLogAsync($"[{studentName}] [OK] Du an dang chay tai http://localhost:{port}");
                }
                else
                {
                    studentStatus.Status = "Failed";
                    studentStatus.Error = "Du an khoi dong that bai hoac tu tat.";
                    await _logStream.WriteLogAsync($"[{studentName}] [FAILED] Khong the ket noi toi cong {port}.");
                    process.Kill(true);
                    _runningProjects.TryRemove(studentName, out _);
                }
            }
            catch (Exception ex)
            {
                studentStatus.Status = "Failed";
                studentStatus.Error = ex.Message;
                await _logStream.WriteLogAsync($"[{studentName}] [ERROR] {ex.Message}");
            }
        }

        public async Task<BatchSessionStatus> StopAllAsync()
        {
            await _logStream.WriteLogAsync("[BATCH] Dang dung tat ca du an hoc sinh...");

            foreach (var kv in _runningProjects)
            {
                try
                {
                    kv.Value.Cts.Cancel();
                    if (!kv.Value.Process.HasExited)
                        kv.Value.Process.Kill(entireProcessTree: true);
                    kv.Value.Process.Dispose();

                    // Cleanup cloned directory
                    if (Directory.Exists(kv.Value.ClonedPath))
                        DeleteDirectoryWithRetries(kv.Value.ClonedPath, 3);
                }
                catch { }
            }

            _runningProjects.Clear();

            _session.SessionStatus = "Stopped";
            _session.Students.ForEach(s => { if (s.Status == "Running") s.Status = "Stopped"; });

            await _logStream.WriteLogAsync("[BATCH] Da dung tat ca du an.");
            return _session;
        }

        public async Task<BatchSessionStatus> StopStudentAsync(string studentName)
        {
            if (_runningProjects.TryRemove(studentName, out var entry))
            {
                try
                {
                    entry.Cts.Cancel();
                    if (!entry.Process.HasExited)
                        entry.Process.Kill(entireProcessTree: true);
                    entry.Process.Dispose();

                    if (Directory.Exists(entry.ClonedPath))
                        DeleteDirectoryWithRetries(entry.ClonedPath, 3);
                }
                catch { }

                var s = _session.Students.FirstOrDefault(x => x.StudentName == studentName);
                if (s != null) s.Status = "Stopped";

                await _logStream.WriteLogAsync($"[{studentName}] Da dung.");
            }

            var runningCount = _session.Students.Count(s => s.Status == "Running");
            _session.SessionStatus = runningCount == 0 ? "Stopped" : "Running";
            return _session;
        }

        private async Task ReadStreamAsync(StreamReader reader, string studentName, bool isError, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(token);
                    if (line == null) break;
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var prefix = isError ? "[STDERR]" : "";
                        await _logStream.WriteLogAsync($"[{studentName}]{prefix} {line}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        private static async Task<bool> WaitForPortAsync(int port, int timeoutSeconds)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    using var tcp = new TcpClient();
                    var connectTask = tcp.ConnectAsync("127.0.0.1", port);
                    var done = await Task.WhenAny(connectTask, Task.Delay(500, cts.Token));
                    if (done == connectTask)
                    {
                        await connectTask;
                        return true;
                    }
                }
                catch { }

                try { await Task.Delay(500, cts.Token); }
                catch (OperationCanceledException) { break; }
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

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(subDir);
                if (name is "bin" or "obj" or ".vs" or ".git") continue;
                CopyDirectory(subDir, Path.Combine(destDir, name));
            }
        }

        private static void DeleteDirectoryWithRetries(string path, int retries)
        {
            for (int i = 0; i < retries; i++)
            {
                try { if (Directory.Exists(path)) Directory.Delete(path, true); return; }
                catch { Thread.Sleep(500); }
            }
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        public void Dispose()
        {
            foreach (var kv in _runningProjects)
            {
                try { kv.Value.Cts.Cancel(); kv.Value.Process.Kill(true); kv.Value.Process.Dispose(); } catch { }
                try { if (Directory.Exists(kv.Value.ClonedPath)) Directory.Delete(kv.Value.ClonedPath, true); } catch { }
            }
        }
    }
}
