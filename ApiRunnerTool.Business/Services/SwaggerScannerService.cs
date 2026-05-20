using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class SwaggerScannerService : ISwaggerScannerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogStreamService _logStream;

        public SwaggerScannerService(IHttpClientFactory httpClientFactory, ILogStreamService logStream)
        {
            _httpClientFactory = httpClientFactory;
            _logStream = logStream;
        }

        public async Task<List<EndpointInfo>> ScanAsync(int port)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Các URL Swagger phổ biến của ASP.NET Core
            var swaggerUrls = new[]
            {
                $"http://localhost:{port}/swagger/v1/swagger.json",
                $"http://localhost:{port}/swagger/v2/swagger.json",
                $"http://localhost:{port}/swagger/swagger.json",
                $"http://localhost:{port}/openapi.json"
            };

            string json = "";
            string successUrl = "";

            foreach (var url in swaggerUrls)
            {
                try
                {
                    await _logStream.WriteLogAsync($"Đang tìm file cấu hình Swagger tại: {url}...");
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        json = await response.Content.ReadAsStringAsync();
                        successUrl = url;
                        await _logStream.WriteLogAsync($"[SUCCESS] Đã tìm thấy Swagger JSON tại: {url}");
                        break;
                    }
                }
                catch
                {
                    // Thử tiếp các URL khác
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                await _logStream.WriteLogAsync("[ERROR] Không tìm thấy file Swagger JSON nào hoạt động. Hãy kiểm tra xem dự án của học sinh có cài Swagger/OpenAPI không!");
                return new List<EndpointInfo>();
            }

            var list = new List<EndpointInfo>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("paths", out var pathsEl))
                {
                    await _logStream.WriteLogAsync("[WARN] File Swagger JSON không chứa thuộc tính 'paths'.");
                    return list;
                }

                var methods = new[] { "get", "post", "put", "delete" };
                foreach (var pathProperty in pathsEl.EnumerateObject())
                {
                    var path = pathProperty.Name; // VD: "/api/Students/{id}"
                    var pathItem = pathProperty.Value;

                    foreach (var m in methods)
                    {
                        if (pathItem.TryGetProperty(m, out var methodEl))
                        {
                            var endpoint = new EndpointInfo
                            {
                                Path = path,
                                Method = m.ToUpper(),
                                Summary = methodEl.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                                OperationId = methodEl.TryGetProperty("operationId", out var op) ? op.GetString() ?? "" : "",
                                Tag = methodEl.TryGetProperty("tags", out var t) && t.GetArrayLength() > 0 ? t[0].GetString() ?? "General" : "General"
                            };

                            // Đọc danh sách parameters
                            if (methodEl.TryGetProperty("parameters", out var paramsEl))
                            {
                                foreach (var paramItem in paramsEl.EnumerateArray())
                                {
                                    var name = paramItem.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                                    var @in = paramItem.TryGetProperty("in", out var i) ? i.GetString() ?? "" : "";
                                    var required = paramItem.TryGetProperty("required", out var r) && r.GetBoolean();
                                    
                                    var type = "string";
                                    if (paramItem.TryGetProperty("schema", out var schemaEl))
                                    {
                                        type = schemaEl.TryGetProperty("type", out var ty) ? ty.GetString() ?? "string" : "string";
                                    }

                                    var defaultValue = type.Contains("int") || type.Contains("num") ? "1" : "1";

                                    endpoint.Parameters.Add(new ParameterInfo
                                    {
                                        Name = name,
                                        In = @in,
                                        Required = required,
                                        Type = type,
                                        DefaultValue = defaultValue
                                    });
                                }
                            }

                            // Chuẩn hoá đường dẫn theo Option C: thay các tham số path ({id}) bằng giá trị "1"
                            var normPath = path;
                            foreach (var param in endpoint.Parameters)
                            {
                                if (param.In.Equals("path", StringComparison.OrdinalIgnoreCase))
                                {
                                    endpoint.HasPathParams = true;
                                    normPath = normPath.Replace($"{{{param.Name}}}", "1");
                                }
                            }
                            endpoint.NormalizedPath = normPath;

                            list.Add(endpoint);
                        }
                    }
                }

                await _logStream.WriteLogAsync($"[SUCCESS] Đã phân tích thành công {list.Count} endpoint API (GET, POST, PUT, DELETE)!");
            }
            catch (Exception ex)
            {
                await _logStream.WriteLogAsync($"[ERROR] Lỗi khi phân tích Swagger JSON: {ex.Message}");
            }

            return list;
        }
    }
}
