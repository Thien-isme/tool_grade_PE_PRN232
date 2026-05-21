using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Helpers;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class PePaper5GradingService : IPePaper5GradingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogStreamService _logStream;

        private static readonly string[] StudentItemFields = { "studentId", "studentName", "email", "gpa" };

        public PePaper5GradingService(IHttpClientFactory httpClientFactory, ILogStreamService logStream)
        {
            _httpClientFactory = httpClientFactory;
            _logStream = logStream;
        }

        public async Task<PeGradingResult> GradeStudentAsync(string studentName, string folderPath, int activePort, string projectStatus)
        {
            var result = new PeGradingResult { StudentName = studentName };
            var searchRoot = Directory.Exists(folderPath) ? folderPath : string.Empty;

            if (string.IsNullOrEmpty(searchRoot))
            {
                result.Message = "Không tìm thấy thư mục bài nộp.";
                return result;
            }

            var q1Csproj = StudentProjectFinder.FindQ1Csproj(searchRoot);
            result.Q1ProjectPath = q1Csproj != null ? Path.GetDirectoryName(q1Csproj) : null;
            result.Q2ProjectPath = StudentProjectFinder.FindQ2ProjectRoot(searchRoot);

            // ── Q1 (5 điểm) ──
            result.Q1Criteria.Add(await CheckMyCnnAsync(result.Q1ProjectPath));
            if (projectStatus == "Running" && activePort > 0)
            {
                result.Q1Criteria.AddRange(await RunQ1ApiTestsAsync(activePort, studentName));
            }
            else
            {
                var skip = new GradingCriterionResult
                {
                    Id = "Q1-RUN",
                    Description = "Q1 API đang chạy (cần để test endpoint)",
                    MaxPoints = 4.5,
                    EarnedPoints = 0,
                    Passed = false,
                    Detail = $"Trạng thái dự án: {projectStatus}. Hãy chạy batch trước khi chấm Q1."
                };
                result.Q1Criteria.Add(skip);
            }

            result.Q1Score = Math.Round(result.Q1Criteria.Sum(c => c.EarnedPoints), 2);
            if (result.Q1Score > 5) result.Q1Score = 5;

            // ── Q2 (5 điểm) — static ──
            result.Q2Criteria.AddRange(RunQ2StaticChecks(searchRoot, result.Q2ProjectPath));
            result.Q2Score = Math.Round(result.Q2Criteria.Sum(c => c.EarnedPoints), 2);
            if (result.Q2Score > 5) result.Q2Score = 5;

            result.TotalScore = Math.Round(result.Q1Score + result.Q2Score, 2);
            await _logStream.WriteLogAsync($"[PE5][{studentName}] Q1={result.Q1Score}/5 Q2={result.Q2Score}/5 Tổng={result.TotalScore}/10");
            return result;
        }

        public async Task<PeBatchGradingSummary> GradeAllRunningAsync(
            IReadOnlyList<(string studentName, string folderPath, int port, string status)> students)
        {
            var summary = new PeBatchGradingSummary();
            foreach (var s in students)
            {
                try
                {
                    var r = await GradeStudentAsync(s.studentName, s.folderPath, s.port, s.status);
                    summary.Results.Add(r);
                    summary.GradedCount++;
                }
                catch (Exception ex)
                {
                    summary.FailedCount++;
                    summary.Results.Add(new PeGradingResult
                    {
                        StudentName = s.studentName,
                        Message = ex.Message
                    });
                }
            }
            return summary;
        }

        public string BuildExportCsv(PeBatchGradingSummary summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("StudentName,Q1,Q2,Total,Q1Max,Q2Max,Notes");
            foreach (var r in summary.Results.OrderBy(x => x.StudentName))
            {
                var notes = string.Join("; ",
                    r.Q1Criteria.Concat(r.Q2Criteria).Where(c => !c.Passed).Select(c => $"{c.Id}:{c.Detail}"));
                sb.AppendLine($"{EscapeCsv(r.StudentName)},{r.Q1Score},{r.Q2Score},{r.TotalScore},5,5,{EscapeCsv(notes)}");
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static async Task<GradingCriterionResult> CheckMyCnnAsync(string? q1ProjectRoot)
        {
            const double pts = 0.5;
            var c = new GradingCriterionResult
            {
                Id = "Q1-PRE",
                Description = "appsettings có ConnectionStrings:MyCnn",
                MaxPoints = pts
            };
            if (string.IsNullOrEmpty(q1ProjectRoot))
            {
                c.Detail = "Không tìm thấy project Q1 (.csproj API).";
                return c;
            }

            var settingsPath = StudentProjectFinder.FindAppsettingsPath(q1ProjectRoot);
            if (settingsPath == null)
            {
                c.Detail = "Không có appsettings.json trong project Q1.";
                return c;
            }

            try
            {
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs)
                    && cs.TryGetProperty("MyCnn", out var myCnn)
                    && myCnn.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(myCnn.GetString()))
                {
                    c.Passed = true;
                    c.EarnedPoints = pts;
                    c.Detail = "Đúng format ConnectionStrings:MyCnn.";
                }
                else
                {
                    c.Detail = "Thiếu ConnectionStrings:MyCnn — theo đề sẽ 0 điểm phần liên quan DB.";
                }
            }
            catch (Exception ex)
            {
                c.Detail = $"Lỗi đọc appsettings: {ex.Message}";
            }

            return c;
        }

        private async Task<List<GradingCriterionResult>> RunQ1ApiTestsAsync(int port, string studentName)
        {
            var list = new List<GradingCriterionResult>();
            list.Add(await TestHttpAsync(port, "Q1-B1", "GET /api/students", 1.0,
                "GET", "/api/students", 200, expectArray: true, itemFields: StudentItemFields));
            list.Add(await TestHttpAsync(port, "Q1-C1", "GET /api/student-performance (hợp lệ)", 1.0,
                "GET", "/api/student-performance?minGpa=8&page=1&pageSize=10&studentName=", 200,
                rootFields: new[] { "data", "totalStudents", "totalPages", "currentPage", "pageSize" },
                nestedArrayField: "data", itemFields: StudentItemFields));
            list.Add(await TestHttpAsync(port, "Q1-C3", "GET student-performance page=-1 → 400", 0.5,
                "GET", "/api/student-performance?page=-1&pageSize=10&studentName=", 400,
                bodyContains: "Invalid pagination parameters"));
            list.Add(await TestHttpAsync(port, "Q1-D1", "PUT grade hợp lệ (enrollment 8)", 1.0,
                "PUT", "/api/enrollments/8/grade", 200,
                body: "{\"grade\":5}", bodyFields: new[] { "enrollmentId", "studentId", "grade" }));
            list.Add(await TestHttpAsync(port, "Q1-D2", "PUT grade=15 → 400", 0.5,
                "PUT", "/api/enrollments/8/grade", 400,
                body: "{\"grade\":15}", bodyContains: "Grade must be between 0 and 10"));
            list.Add(await TestHttpAsync(port, "Q1-E2", "DELETE enrollment 9 đã có điểm → 400", 0.5,
                "DELETE", "/api/enrollments/9", 400,
                bodyContains: "Cannot cancel an enrollment that has already been graded"));
            return list;
        }

        private async Task<GradingCriterionResult> TestHttpAsync(
            int port,
            string id,
            string description,
            double maxPoints,
            string method,
            string path,
            int expectStatus,
            string? body = null,
            string? bodyContains = null,
            bool expectArray = false,
            string[]? rootFields = null,
            string[]? bodyFields = null,
            string? nestedArrayField = null,
            string[]? itemFields = null)
        {
            var c = new GradingCriterionResult
            {
                Id = id,
                Description = description,
                MaxPoints = maxPoints
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var url = $"http://localhost:{port}{path}";

            try
            {
                HttpResponseMessage response;
                if (method == "GET")
                    response = await client.GetAsync(url);
                else if (method == "PUT")
                {
                    var content = new StringContent(body ?? "{}", Encoding.UTF8, "application/json");
                    response = await client.PutAsync(url, content);
                }
                else if (method == "DELETE")
                    response = await client.DeleteAsync(url);
                else
                {
                    c.Detail = $"Method không hỗ trợ: {method}";
                    return c;
                }

                var status = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync();

                if (status != expectStatus)
                {
                    c.Detail = $"Status {status}, kỳ vọng {expectStatus}. Body: {Truncate(responseBody, 200)}";
                    return c;
                }

                if (!string.IsNullOrEmpty(bodyContains)
                    && !responseBody.Contains(bodyContains, StringComparison.OrdinalIgnoreCase))
                {
                    c.Detail = $"Thiếu message '{bodyContains}'. Body: {Truncate(responseBody, 200)}";
                    return c;
                }

                if (!string.IsNullOrWhiteSpace(responseBody) && responseBody.TrimStart().StartsWith('{') || responseBody.TrimStart().StartsWith('['))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        var root = doc.RootElement;

                        if (expectArray && root.ValueKind != JsonValueKind.Array)
                        {
                            c.Detail = "Response không phải mảng JSON.";
                            return c;
                        }

                        if (rootFields != null)
                        {
                            foreach (var f in rootFields)
                            {
                                if (!HasJsonProperty(root, f))
                                {
                                    c.Detail = $"Thiếu field '{f}' ở root.";
                                    return c;
                                }
                            }
                        }

                        if (bodyFields != null)
                        {
                            foreach (var f in bodyFields)
                            {
                                if (!HasJsonProperty(root, f))
                                {
                                    c.Detail = $"Thiếu field '{f}' trong body.";
                                    return c;
                                }
                            }
                        }

                        JsonElement itemsElement = root;
                        if (expectArray)
                            itemsElement = root;
                        else if (!string.IsNullOrEmpty(nestedArrayField)
                            && root.TryGetProperty(nestedArrayField, out var nested)
                            && nested.ValueKind == JsonValueKind.Array)
                            itemsElement = nested;
                        else if (root.ValueKind == JsonValueKind.Array)
                            itemsElement = root;

                        if (itemFields != null && itemFields.Length > 0)
                        {
                            if (itemsElement.ValueKind == JsonValueKind.Array && itemsElement.GetArrayLength() > 0)
                            {
                                var first = itemsElement[0];
                                foreach (var f in itemFields)
                                {
                                    if (!HasJsonProperty(first, f))
                                    {
                                        c.Detail = $"Phần tử đầu thiếu field '{f}'.";
                                        return c;
                                    }
                                }
                            }
                            else if (itemsElement.ValueKind != JsonValueKind.Array && itemFields == bodyFields)
                            {
                                // single object — already checked bodyFields
                            }
                            else if (expectArray || nestedArrayField != null)
                            {
                                c.Detail = "Mảng dữ liệu rỗng — không kiểm được schema item.";
                                return c;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        if (bodyFields != null || expectArray || rootFields != null)
                        {
                            c.Detail = "Body không parse được JSON.";
                            return c;
                        }
                    }
                }
                else if (expectStatus == 204 && string.IsNullOrEmpty(responseBody))
                {
                    // OK
                }
                else if (bodyFields != null || expectArray)
                {
                    c.Detail = "Không có JSON body để kiểm tra.";
                    return c;
                }

                c.Passed = true;
                c.EarnedPoints = maxPoints;
                c.Detail = $"OK — {method} {path} → {status}";
            }
            catch (Exception ex)
            {
                c.Detail = $"Lỗi gọi API: {ex.GetBaseException().Message}";
            }

            return c;
        }

        private static bool HasJsonProperty(JsonElement el, string name)
        {
            if (el.ValueKind != JsonValueKind.Object) return false;
            foreach (var prop in el.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static List<GradingCriterionResult> RunQ2StaticChecks(string rootFolder, string? q2Root)
        {
            var list = new List<GradingCriterionResult>();

            list.Add(new GradingCriterionResult
            {
                Id = "Q2-PROJ",
                Description = "Có project MVC/Razor (Q2)",
                MaxPoints = 0.5,
                Passed = !string.IsNullOrEmpty(q2Root),
                EarnedPoints = string.IsNullOrEmpty(q2Root) ? 0 : 0.5,
                Detail = string.IsNullOrEmpty(q2Root)
                    ? "Không tìm thấy project Q2 (Views/Pages hoặc tên Q2_*)."
                    : $"Project: {q2Root}"
            });

            if (string.IsNullOrEmpty(q2Root))
            {
                foreach (var stub in new[] {
                    ("Q2-URL", "GivenAPIBaseUrl trong appsettings", 1.0),
                    ("Q2-HTTP", "Sử dụng HttpClient", 1.0),
                    ("Q2-API", "Gọi schedules/search và courses", 1.5),
                    ("Q2-HTML", "Input/output có thuộc tính id", 1.0)
                })
                {
                    list.Add(new GradingCriterionResult
                    {
                        Id = stub.Item1,
                        Description = stub.Item2,
                        MaxPoints = stub.Item3,
                        Detail = "Bỏ qua — không có project Q2."
                    });
                }
                return list;
            }

            var allText = CollectSourceText(q2Root);

            // GivenAPIBaseUrl
            var urlCrit = new GradingCriterionResult
            {
                Id = "Q2-URL",
                Description = "GivenAPIBaseUrl = http://localhost:5100",
                MaxPoints = 1.0
            };
            var settings = StudentProjectFinder.FindAppsettingsPath(q2Root);
            if (settings != null)
            {
                try
                {
                    var json = File.ReadAllText(settings);
                    if (json.Contains("GivenAPIBaseUrl", StringComparison.Ordinal)
                        && json.Contains("http://localhost:5100", StringComparison.OrdinalIgnoreCase))
                    {
                        urlCrit.Passed = true;
                        urlCrit.EarnedPoints = 1.0;
                        urlCrit.Detail = "Đúng cấu hình GivenAPIBaseUrl.";
                    }
                    else
                    {
                        urlCrit.Detail = "Thiếu hoặc sai GivenAPIBaseUrl (yêu cầu http://localhost:5100).";
                    }
                }
                catch (Exception ex) { urlCrit.Detail = ex.Message; }
            }
            else urlCrit.Detail = "Không có appsettings.json.";
            list.Add(urlCrit);

            // HttpClient
            var httpCrit = new GradingCriterionResult
            {
                Id = "Q2-HTTP",
                Description = "Sử dụng HttpClient",
                MaxPoints = 1.0
            };
            if (Regex.IsMatch(allText, @"\bHttpClient\b"))
            {
                httpCrit.Passed = true;
                httpCrit.EarnedPoints = 1.0;
                httpCrit.Detail = "Tìm thấy HttpClient trong mã nguồn.";
            }
            else httpCrit.Detail = "Không thấy HttpClient trong file .cs.";
            list.Add(httpCrit);

            // API paths
            var apiCrit = new GradingCriterionResult
            {
                Id = "Q2-API",
                Description = "Gọi /api/schedules/search và /api/courses",
                MaxPoints = 1.5
            };
            var hasSchedules = allText.Contains("schedules/search", StringComparison.OrdinalIgnoreCase)
                || allText.Contains("schedules", StringComparison.OrdinalIgnoreCase);
            var hasCourses = allText.Contains("/api/courses", StringComparison.OrdinalIgnoreCase)
                || allText.Contains("api/courses", StringComparison.OrdinalIgnoreCase);
            if (hasSchedules && hasCourses)
            {
                apiCrit.Passed = true;
                apiCrit.EarnedPoints = 1.5;
                apiCrit.Detail = "Có tham chiếu schedules và courses.";
            }
            else
            {
                apiCrit.EarnedPoints = (hasSchedules ? 0.75 : 0) + (hasCourses ? 0.75 : 0);
                apiCrit.Passed = apiCrit.EarnedPoints >= 1.5;
                apiCrit.Detail = $"schedules:{hasSchedules}, courses:{hasCourses}";
            }
            list.Add(apiCrit);

            // HTML id
            var htmlCrit = new GradingCriterionResult
            {
                Id = "Q2-HTML",
                Description = "Thẻ input/output có id",
                MaxPoints = 1.0
            };
            var (total, withId) = CountHtmlIds(q2Root);
            if (total == 0)
            {
                htmlCrit.Detail = "Không tìm thấy input/select/textarea trong Views/Pages/wwwroot.";
            }
            else
            {
                var ratio = (double)withId / total;
                htmlCrit.EarnedPoints = Math.Round(ratio * 1.0, 2);
                htmlCrit.Passed = ratio >= 0.8;
                htmlCrit.Detail = $"{withId}/{total} control có id (≥80% để đạt).";
            }
            list.Add(htmlCrit);

            return list;
        }

        private static string CollectSourceText(string projectRoot)
        {
            var sb = new StringBuilder();
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not ".cs" and not ".cshtml" and not ".html" and not ".razor") continue;
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                    continue;
                try { sb.AppendLine(File.ReadAllText(file)); } catch { }
            }
            return sb.ToString();
        }

        private static (int total, int withId) CountHtmlIds(string projectRoot)
        {
            int total = 0, withId = 0;
            var tagPattern = new Regex(@"<(input|select|textarea)\b[^>]*>", RegexOptions.IgnoreCase);
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not ".cshtml" and not ".html" and not ".razor") continue;
                if (file.Contains("bin") || file.Contains("obj")) continue;
                string text;
                try { text = File.ReadAllText(file); } catch { continue; }

                foreach (Match m in tagPattern.Matches(text))
                {
                    total++;
                    if (Regex.IsMatch(m.Value, @"\bid\s*=", RegexOptions.IgnoreCase))
                        withId++;
                }
            }
            return (total, withId);
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s[..max] + "...";
    }
}
