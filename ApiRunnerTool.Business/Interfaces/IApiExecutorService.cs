using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IApiExecutorService
    {
        Task<ApiResponse> ExecuteAsync(int port, EndpointInfo endpoint);
    }
}
