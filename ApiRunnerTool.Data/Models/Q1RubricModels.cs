using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    public class Q1RubricDefinition
    {
        public Q1RubricSettings Settings { get; set; } = new();
        public List<Q1ApiTestCase> TestCases { get; set; } = new();
    }

    public class Q1RubricSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SqlScriptPath { get; set; } = string.Empty;
    }

    public class Q1ApiTestCase
    {
        public int Order { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Points { get; set; }
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int ExpectedStatus { get; set; }
        public string ExpectedJson { get; set; } = string.Empty;
        public string CompareMode { get; set; } = "ExactJson";
        public string ArrayCompareMode { get; set; } = "ExactOrder";
        public bool Enabled { get; set; } = true;
    }

    public class PeGradeRequest
    {
        public string RubricExcelPath { get; set; } = string.Empty;
    }
}
