using System;
using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    /// <summary>
    /// Represents a single student's project status within a batch exam session.
    /// </summary>
    public class StudentProjectStatus
    {
        public string StudentName { get; set; } = string.Empty;   // Derived from folder name
        public string FolderPath { get; set; } = string.Empty;    // Original project path
        public string ClonedPath { get; set; } = string.Empty;    // Sandboxed clone path
        public string ProjectName { get; set; } = string.Empty;   // .csproj file name
        public string Status { get; set; } = "Pending";           // Pending | Starting | Running | Failed | Stopped
        public int ActivePort { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
    }

    /// <summary>
    /// Represents the overall state of a batch exam session (one parent folder → many students).
    /// </summary>
    public class BatchSessionStatus
    {
        public string SessionFolderPath { get; set; } = string.Empty;
        public string SessionStatus { get; set; } = "Idle";        // Idle | Starting | Running | Stopped
        public DateTime? StartedAt { get; set; }
        public List<StudentProjectStatus> Students { get; set; } = new();
    }
}
