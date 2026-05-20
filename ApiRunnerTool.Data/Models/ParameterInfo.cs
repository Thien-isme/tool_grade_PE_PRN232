namespace ApiRunnerTool.Data.Models
{
    public class ParameterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string In { get; set; } = string.Empty; // "path" | "query"
        public string Type { get; set; } = string.Empty; // "integer" | "string" | etc.
        public bool Required { get; set; }
        public string DefaultValue { get; set; } = "1";
    }
}
