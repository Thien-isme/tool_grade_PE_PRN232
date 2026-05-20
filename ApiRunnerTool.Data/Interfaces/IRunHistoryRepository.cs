using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Data.Interfaces
{
    public interface IRunHistoryRepository
    {
        Task SaveAsync(RunHistoryEntity history);
        Task<List<RunHistoryEntity>> GetAllAsync();
        Task<RunHistoryEntity?> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}
