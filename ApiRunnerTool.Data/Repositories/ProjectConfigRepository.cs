using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ApiRunnerTool.Data.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Data.Repositories
{
    public class ProjectConfigRepository : IProjectConfigRepository
    {
        private readonly string _filePath;

        public ProjectConfigRepository()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var baseDir = Path.Combine(userProfile, ".apirunner");
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            _filePath = Path.Combine(baseDir, "projects.json");
        }

        private async Task<List<ProjectConfigEntity>> LoadAllAsync()
        {
            if (!File.Exists(_filePath)) return new List<ProjectConfigEntity>();
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<ProjectConfigEntity>>(json) ?? new List<ProjectConfigEntity>();
            }
            catch
            {
                return new List<ProjectConfigEntity>();
            }
        }

        private async Task SaveAllAsync(List<ProjectConfigEntity> list)
        {
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }

        public async Task AddOrUpdateAsync(ProjectConfigEntity config)
        {
            var list = await LoadAllAsync();
            list.RemoveAll(x => x.FolderPath.Equals(config.FolderPath, StringComparison.OrdinalIgnoreCase));
            config.LastUsed = DateTime.UtcNow;
            list.Add(config);
            await SaveAllAsync(list);
        }

        public async Task<List<ProjectConfigEntity>> GetRecentProjectsAsync()
        {
            var list = await LoadAllAsync();
            list.Sort((a, b) => b.LastUsed.CompareTo(a.LastUsed));
            return list;
        }

        public async Task DeleteAsync(string folderPath)
        {
            var list = await LoadAllAsync();
            list.RemoveAll(x => x.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
            await SaveAllAsync(list);
        }
    }
}
