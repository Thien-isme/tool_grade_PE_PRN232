using System.Collections.Generic;
using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface ISwaggerScannerService
    {
        Task<List<EndpointInfo>> ScanAsync(int port);
    }
}
