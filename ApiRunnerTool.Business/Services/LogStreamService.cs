using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using ApiRunnerTool.Business.Interfaces;

namespace ApiRunnerTool.Business.Services
{
    public class LogStreamService : ILogStreamService
    {
        private Channel<string> _channel;

        public LogStreamService()
        {
            _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false
            });
        }

        public ChannelReader<string> GetReader() => _channel.Reader;

        public async Task WriteLogAsync(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formatted = $"[{timestamp}] {message}";
            await _channel.Writer.WriteAsync(formatted);
        }

        public void ClearLogs()
        {
            // Re-create channel to flush old logs
            _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false
            });
        }
    }
}
