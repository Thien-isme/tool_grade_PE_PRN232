using System.Threading.Channels;
using System.Threading.Tasks;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface ILogStreamService
    {
        ChannelReader<string> GetReader();
        Task WriteLogAsync(string message);
        void ClearLogs();
    }
}
