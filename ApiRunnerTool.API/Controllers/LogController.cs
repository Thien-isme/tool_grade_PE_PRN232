using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ApiRunnerTool.Business.Interfaces;

namespace ApiRunnerTool.API.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogController : ControllerBase
    {
        private readonly ILogStreamService _logStream;

        public LogController(ILogStreamService logStream)
        {
            _logStream = logStream;
        }

        [HttpGet("stream")]
        public async Task StreamLogs()
        {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            var reader = _logStream.GetReader();
            var responseStream = Response.Body;

            // Đầu tiên gửi một tin nhắn rỗng để thiết lập kết nối
            await responseStream.WriteAsync(Encoding.UTF8.GetBytes("data: [CONNECTED] Kết nối luồng log thành công...\n\n"));
            await responseStream.FlushAsync();

            try
            {
                while (await reader.WaitToReadAsync(HttpContext.RequestAborted))
                {
                    while (reader.TryRead(out var logLine))
                    {
                        var data = $"data: {logLine}\n\n";
                        await responseStream.WriteAsync(Encoding.UTF8.GetBytes(data), HttpContext.RequestAborted);
                        await responseStream.FlushAsync();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Client đóng tab hoặc huỷ kết nối
            }
        }
    }
}
