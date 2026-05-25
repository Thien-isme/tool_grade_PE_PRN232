using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Interfaces
{
    public interface IQ1RubricExcelService
    {
        Q1RubricDefinition Load(string excelPath);
        byte[] BuildTemplate();
    }
}
