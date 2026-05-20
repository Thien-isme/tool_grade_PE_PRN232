using System;

namespace ApiRunnerTool.Data.Models
{
    public class ProjectConfigEntity
    {
        public string FolderPath { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }
}
