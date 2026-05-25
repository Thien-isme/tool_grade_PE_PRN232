using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ApiRunnerTool.Business.Interfaces;
using ApiRunnerTool.Data.Models;

namespace ApiRunnerTool.Business.Services
{
    public class SqlServerDatabaseResetService : IDatabaseResetService
    {
        private readonly ILogStreamService _logStream;

        public SqlServerDatabaseResetService(ILogStreamService logStream)
        {
            _logStream = logStream;
        }

        public async Task ResetAsync(Q1RubricSettings settings)
        {
            if (!File.Exists(settings.SqlScriptPath))
                throw new FileNotFoundException("Khong tim thay file SQL reset database.", settings.SqlScriptPath);

            var script = await File.ReadAllTextAsync(settings.SqlScriptPath);
            var connectionString = ChooseExecutionConnection(settings.ConnectionString, script);

            await _logStream.WriteLogAsync($"[Q1][DB] Dang reset database bang script: {settings.SqlScriptPath}");
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var batch in SplitSqlBatches(script))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                await using var command = connection.CreateCommand();
                command.CommandText = batch;
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync();
            }

            await _logStream.WriteLogAsync("[Q1][DB] Reset database thanh cong.");
        }

        private static string ChooseExecutionConnection(string connectionString, string script)
        {
            if (!Regex.IsMatch(script, @"\b(create|drop)\s+database\b|\buse\s+\[?\w+\]?", RegexOptions.IgnoreCase))
                return connectionString;

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ConnectionString;
        }

        private static IEnumerable<string> SplitSqlBatches(string script)
        {
            return Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }
    }
}
