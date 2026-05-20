using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class ApiExecutorService : IApiExecutorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogStreamService _logStream;

        public ApiExecutorService(IHttpClientFactory httpClientFactory, ILogStreamService logStream)
        {
            _httpClientFactory = httpClientFactory;
            _logStream = logStream;
        }

        public async Task<ApiResponse> ExecuteAsync(int port, EndpointInfo endpoint)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var method = (endpoint.Method ?? "GET").ToUpper();

            // 1. Chuẩn hoá path: thay path parameters bằng "1"
            var path = endpoint.Path;
            foreach (var p in endpoint.Parameters.Where(x => x.In.Equals("path", StringComparison.OrdinalIgnoreCase)))
            {
                path = path.Replace($"{{{p.Name}}}", "1");
            }

            // 2. Thêm query parameters: tất cả query parameters bắt buộc (hoặc tất cả cho chắc) được đặt bằng "1"
            var queryParams = new List<string>();
            foreach (var p in endpoint.Parameters.Where(x => x.In.Equals("query", StringComparison.OrdinalIgnoreCase)))
            {
                if (p.Required || true) // Bắt buộc hoặc tuỳ chọn đều cấp "1" theo Option C
                {
                    queryParams.Add($"{p.Name}=1");
                }
            }

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var relativeUrl = path + queryString;
            var fullUrl = $"http://localhost:{port}{relativeUrl}";

            var result = new ApiResponse
            {
                EndpointPath = endpoint.Path,
                ExecutedUrl = fullUrl,
                Method = method,
                ExecutedAt = DateTime.UtcNow
            };

            await _logStream.WriteLogAsync($"[TESTING] Đang gọi {method}: {fullUrl}...");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                HttpResponseMessage response;
                if (method == "POST")
                {
                    var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    response = await client.PostAsync(fullUrl, content);
                }
                else if (method == "PUT")
                {
                    var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    response = await client.PutAsync(fullUrl, content);
                }
                else if (method == "DELETE")
                {
                    response = await client.DeleteAsync(fullUrl);
                }
                else
                {
                    response = await client.GetAsync(fullUrl);
                }

                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.IsSuccess = response.IsSuccessStatusCode;

                // Đọc headers
                foreach (var header in response.Headers)
                {
                    result.Headers[header.Key] = string.Join(", ", header.Value);
                }
                foreach (var header in response.Content.Headers)
                {
                    result.Headers[header.Key] = string.Join(", ", header.Value);
                }

                // Đọc body
                var body = await response.Content.ReadAsStringAsync();
                if (body.Length > 20000)
                {
                    result.Body = body.Substring(0, 20000) + "\n... [Nội dung quá dài, tự động thu gọn]";
                }
                else
                {
                    result.Body = body;
                }

                await _logStream.WriteLogAsync($"[TESTED] Hoàn thành {method}: {fullUrl} -> Status: {result.StatusCode} ({result.ResponseTimeMs}ms)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.StatusCode = 0;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.IsSuccess = false;
                result.ErrorMessage = ex.GetBaseException().Message;
                result.Body = string.Empty;

                await _logStream.WriteLogAsync($"[TEST_FAILED] Lỗi khi gọi {method} {fullUrl}: {result.ErrorMessage}");
            }

            return result;
        }
    }
}
