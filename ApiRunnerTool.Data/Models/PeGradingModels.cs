using System.Collections.Generic;

namespace ApiRunnerTool.Data.Models
{
    public class PeGradingResult
    {
        public string StudentName { get; set; } = string.Empty;
        public double Q1Score { get; set; }
        public double Q2Score { get; set; }
        public double TotalScore { get; set; }
        public double Q1Max { get; set; } = 5;
        public double Q2Max { get; set; } = 5;
        public string? Q1ProjectPath { get; set; }
        public string? Q2ProjectPath { get; set; }
        public string? Message { get; set; }
        public List<GradingCriterionResult> Q1Criteria { get; set; } = new();
        public List<GradingCriterionResult> Q2Criteria { get; set; } = new();
    }

    public class GradingCriterionResult
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double MaxPoints { get; set; }
        public double EarnedPoints { get; set; }
        public bool Passed { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public class PeBatchGradingSummary
    {
        public List<PeGradingResult> Results { get; set; } = new();
        public int GradedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
