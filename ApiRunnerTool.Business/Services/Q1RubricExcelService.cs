using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class Q1RubricExcelService : IQ1RubricExcelService
    {
        private const string SettingsSheet = "Settings";
        private const string TestCasesSheet = "Q1_TestCases";

        public Q1RubricDefinition Load(string excelPath)
        {
            if (string.IsNullOrWhiteSpace(excelPath))
                throw new InvalidOperationException("Chua cau hinh duong dan file rubric Excel.");
            if (!File.Exists(excelPath))
                throw new FileNotFoundException("Khong tim thay file rubric Excel.", excelPath);

            using var workbook = new XLWorkbook(excelPath);
            var settingsSheet = workbook.Worksheets.Worksheet(SettingsSheet)
                ?? throw new InvalidOperationException($"Khong tim thay sheet {SettingsSheet}.");
            var testSheet = workbook.Worksheets.Worksheet(TestCasesSheet)
                ?? throw new InvalidOperationException($"Khong tim thay sheet {TestCasesSheet}.");

            var rubric = new Q1RubricDefinition
            {
                Settings = ReadSettings(settingsSheet)
            };

            if (string.IsNullOrWhiteSpace(rubric.Settings.ConnectionString))
                throw new InvalidOperationException("Settings.ConnectionString khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(rubric.Settings.SqlScriptPath))
                throw new InvalidOperationException("Settings.SqlScriptPath khong duoc de trong.");
            if (!File.Exists(rubric.Settings.SqlScriptPath))
                throw new FileNotFoundException("Khong tim thay file SQL reset database.", rubric.Settings.SqlScriptPath);

            var headerMap = testSheet.Row(1).CellsUsed()
                .ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

            int lastRow = testSheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int row = 2; row <= lastRow; row++)
            {
                var id = ReadString(testSheet, row, headerMap, "Id");
                if (string.IsNullOrWhiteSpace(id)) continue;

                var enabledText = ReadString(testSheet, row, headerMap, "Enabled");
                var testCase = new Q1ApiTestCase
                {
                    Order = ReadInt(testSheet, row, headerMap, "Order"),
                    Id = id,
                    Name = ReadString(testSheet, row, headerMap, "Name"),
                    Points = ReadDouble(testSheet, row, headerMap, "Points"),
                    Method = ReadString(testSheet, row, headerMap, "Method").ToUpperInvariant(),
                    Path = ReadString(testSheet, row, headerMap, "Path"),
                    Query = ReadString(testSheet, row, headerMap, "Query"),
                    Headers = ReadString(testSheet, row, headerMap, "Headers"),
                    Body = ReadString(testSheet, row, headerMap, "Body"),
                    ExpectedStatus = ReadInt(testSheet, row, headerMap, "ExpectedStatus"),
                    ExpectedJson = ReadString(testSheet, row, headerMap, "ExpectedJson"),
                    CompareMode = DefaultIfEmpty(ReadString(testSheet, row, headerMap, "CompareMode"), "ExactJson"),
                    ArrayCompareMode = DefaultIfEmpty(ReadString(testSheet, row, headerMap, "ArrayCompareMode"), "ExactOrder"),
                    Enabled = string.IsNullOrWhiteSpace(enabledText) || enabledText.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                };

                if (testCase.Enabled)
                    Validate(testCase, row);
                rubric.TestCases.Add(testCase);
            }

            rubric.TestCases = rubric.TestCases
                .Where(t => t.Enabled)
                .OrderBy(t => t.Order)
                .ThenBy(t => t.Id)
                .ToList();

            if (rubric.TestCases.Count == 0)
                throw new InvalidOperationException("Rubric khong co testcase Q1 nao dang Enabled.");

            return rubric;
        }

        public byte[] BuildTemplate()
        {
            using var workbook = new XLWorkbook();
            var settings = workbook.Worksheets.Add(SettingsSheet);
            settings.Cell("A1").Value = "Key";
            settings.Cell("B1").Value = "Value";
            settings.Cell("A2").Value = "ConnectionString";
            settings.Cell("B2").Value = "Server=localhost;Database=PE5_Q1_Test;Trusted_Connection=True;TrustServerCertificate=True";
            settings.Cell("A3").Value = "SqlScriptPath";
            settings.Cell("B3").Value = "D:\\PRN232\\Rubrics\\PE5\\reset.sql";
            settings.Columns().AdjustToContents();

            var tests = workbook.Worksheets.Add(TestCasesSheet);
            var headers = new[]
            {
                "Order", "Id", "Name", "Points", "Method", "Path", "Query", "Headers", "Body",
                "ExpectedStatus", "ExpectedJson", "CompareMode", "ArrayCompareMode", "Enabled"
            };
            for (int i = 0; i < headers.Length; i++)
                tests.Cell(1, i + 1).Value = headers[i];

            AddSample(tests, 2, 10, "Q1-B1", "Get all students", 1.0, "GET", "/api/students", "", "", "", 200,
                "[{\"studentId\":1,\"studentName\":\"Nguyen Van A\",\"email\":\"a@fpt.edu.vn\",\"gpa\":8.5}]",
                "ExactJson", "IgnoreOrder");
            AddSample(tests, 3, 20, "Q1-C3", "Invalid pagination", 0.5, "GET", "/api/student-performance",
                "page=-1&pageSize=10&studentName=", "", "", 400,
                "{\"message\":\"Invalid pagination parameters\"}", "ExactJson", "ExactOrder");
            AddSample(tests, 4, 30, "Q1-D1", "Update grade", 1.0, "PUT", "/api/enrollments/8/grade", "", "",
                "{\"grade\":5}", 200, "{\"enrollmentId\":8,\"studentId\":1,\"grade\":5}", "ExactJson", "ExactOrder");

            tests.Columns().AdjustToContents();
            tests.Column(11).Width = 80;
            tests.Column(9).Width = 28;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static Q1RubricSettings ReadSettings(IXLWorksheet sheet)
        {
            var settings = new Q1RubricSettings();
            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int row = 2; row <= lastRow; row++)
            {
                var key = sheet.Cell(row, 1).GetString().Trim();
                var value = sheet.Cell(row, 2).GetString().Trim();
                if (key.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase))
                    settings.ConnectionString = value;
                else if (key.Equals("SqlScriptPath", StringComparison.OrdinalIgnoreCase))
                    settings.SqlScriptPath = value;
            }
            return settings;
        }

        private static void AddSample(IXLWorksheet sheet, int row, int order, string id, string name, double points,
            string method, string path, string query, string headers, string body, int expectedStatus,
            string expectedJson, string compareMode, string arrayCompareMode)
        {
            object[] values =
            {
                order, id, name, points, method, path, query, headers, body, expectedStatus,
                expectedJson, compareMode, arrayCompareMode, "TRUE"
            };

            for (int i = 0; i < values.Length; i++)
                sheet.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i]);
        }

        private static void Validate(Q1ApiTestCase testCase, int row)
        {
            if (testCase.Points <= 0)
                throw new InvalidOperationException($"Dong {row}: Points phai lon hon 0.");
            if (string.IsNullOrWhiteSpace(testCase.Method))
                throw new InvalidOperationException($"Dong {row}: Method khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(testCase.Path) || !testCase.Path.StartsWith('/'))
                throw new InvalidOperationException($"Dong {row}: Path phai bat dau bang '/'.");
            if (testCase.ExpectedStatus <= 0)
                throw new InvalidOperationException($"Dong {row}: ExpectedStatus khong hop le.");
            if (string.IsNullOrWhiteSpace(testCase.ExpectedJson))
                throw new InvalidOperationException($"Dong {row}: ExpectedJson khong duoc de trong.");
        }

        private static string ReadString(IXLWorksheet sheet, int row, System.Collections.Generic.Dictionary<string, int> map, string name)
            => map.TryGetValue(name, out var col) ? sheet.Cell(row, col).GetString().Trim() : string.Empty;

        private static int ReadInt(IXLWorksheet sheet, int row, System.Collections.Generic.Dictionary<string, int> map, string name)
            => int.TryParse(ReadString(sheet, row, map, name), out var value) ? value : 0;

        private static double ReadDouble(IXLWorksheet sheet, int row, System.Collections.Generic.Dictionary<string, int> map, string name)
            => double.TryParse(ReadString(sheet, row, map, name), out var value) ? value : 0;

        private static string DefaultIfEmpty(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
