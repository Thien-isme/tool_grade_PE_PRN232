using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IDatabaseResetService
    {
        Task ResetAsync(Q1RubricSettings settings);
    }
}
