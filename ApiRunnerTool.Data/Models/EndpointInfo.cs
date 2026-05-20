using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    public class EndpointInfo
    {
        public string Path { get; set; } = string.Empty;           // "/api/products/{id}"
        public string NormalizedPath { get; set; } = string.Empty; // "/api/products/1" (Option C)
        public string Method { get; set; } = "GET";                 // "GET", "POST", "PUT", "DELETE"
        public string Summary { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public List<ParameterInfo> Parameters { get; set; } = new();
        public bool HasPathParams { get; set; }
    }
}
