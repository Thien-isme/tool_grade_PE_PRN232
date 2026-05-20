using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Data.Repositories
{
    public class RunHistoryRepository : IRunHistoryRepository
    {
        private readonly string _storageDir;

        public RunHistoryRepository()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _storageDir = Path.Combine(userProfile, ".apirunner", "history");
            if (!Directory.Exists(_storageDir))
            {
                Directory.CreateDirectory(_storageDir);
            }
        }

        public async Task SaveAsync(RunHistoryEntity history)
        {
            var filePath = Path.Combine(_storageDir, $"{history.Id}.json");
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<RunHistoryEntity>> GetAllAsync()
        {
            var list = new List<RunHistoryEntity>();
            var files = Directory.GetFiles(_storageDir, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var entity = JsonSerializer.Deserialize<RunHistoryEntity>(json);
                    if (entity != null)
                    {
                        list.Add(entity);
                    }
                }
                catch
                {
                    // Ignore corrupted files
                }
            }
            list.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));
            return list;
        }

        public async Task<RunHistoryEntity?> GetByIdAsync(Guid id)
        {
            var filePath = Path.Combine(_storageDir, $"{id}.json");
            if (!File.Exists(filePath)) return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<RunHistoryEntity>(json);
            }
            catch
            {
                return null;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            var filePath = Path.Combine(_storageDir, $"{id}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
