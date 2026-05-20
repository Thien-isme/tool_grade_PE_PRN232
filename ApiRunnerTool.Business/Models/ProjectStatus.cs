using System;

namespace ApiRunnerTool.Business.Models
{
    public class ProjectStatus
    {
        public string FolderPath { get; set; } = string.Empty;
        public string ClonedPath { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Status { get; set; } = "Stopped"; // "Stopped" | "Starting" | "Running" | "Failed"
        public int ActivePort { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
    }
}
