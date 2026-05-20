using System;
using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    public class ApiResponse
    {
        public string EndpointPath { get; set; } = string.Empty;     // path gốc
        public string ExecutedUrl { get; set; } = string.Empty;      // URL thực tế đã gọi (params = 1)
        public string Method { get; set; } = "GET";                  // "GET", "POST", "PUT", "DELETE"
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;             // raw JSON string or HTML if error
        public long ResponseTimeMs { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }                    // if exception (timeout, connection error, etc.)
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}
