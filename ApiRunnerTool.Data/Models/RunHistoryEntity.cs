using System;
using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    public class RunHistoryEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProjectPath { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StoppedAt { get; set; }
        public List<ApiResponse> Results { get; set; } = new();
    }
}
