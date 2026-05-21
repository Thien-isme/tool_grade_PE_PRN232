using System;
using System.IO;
using System.Linq;

namespace ApiRunnerTool.Business.Helpers
{
    public static class StudentProjectFinder
    {
        public static string? FindQ1Csproj(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) return null;

            var all = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .ToList();

            if (all.Count == 0) return null;

            var q1Named = all.Where(IsQ1Path).ToList();
            if (q1Named.Count > 0)
                return PreferApiProject(q1Named) ?? q1Named[0];

            var apiProjects = all.Where(IsLikelyWebApi).ToList();
            if (apiProjects.Count > 0)
                return PreferApiProject(apiProjects) ?? apiProjects[0];

            return all.FirstOrDefault();
        }

        public static string? FindQ2ProjectRoot(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) return null;

            var all = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .ToList();

            var q2 = all.Where(IsQ2Path).Select(p => Path.GetDirectoryName(p)!);
            if (q2.Any()) return q2.First();

            var mvc = all.Where(IsLikelyMvcOrRazor).Select(p => Path.GetDirectoryName(p)!);
            return mvc.FirstOrDefault();
        }

        public static string? FindAppsettingsPath(string projectRoot)
        {
            foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
            {
                var path = Path.Combine(projectRoot, name);
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static bool IsQ1Path(string csprojPath)
        {
            var text = csprojPath.Replace('\\', '/');
            return text.Contains("/Q1_", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Q1_", StringComparison.OrdinalIgnoreCase)
                || text.EndsWith("/Q1.csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsQ2Path(string csprojPath)
        {
            var text = csprojPath.Replace('\\', '/');
            return text.Contains("/Q2_", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Q2_", StringComparison.OrdinalIgnoreCase)
                || text.EndsWith("/Q2.csproj", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyWebApi(string csprojPath)
        {
            var dir = Path.GetDirectoryName(csprojPath)!;
            if (Directory.Exists(Path.Combine(dir, "Controllers"))) return true;
            var program = Path.Combine(dir, "Program.cs");
            if (!File.Exists(program)) return false;
            var content = File.ReadAllText(program);
            return content.Contains("AddControllers", StringComparison.Ordinal)
                || content.Contains("MapControllers", StringComparison.Ordinal)
                || content.Contains("AddSwaggerGen", StringComparison.Ordinal);
        }

        private static bool IsLikelyMvcOrRazor(string csprojPath)
        {
            var dir = Path.GetDirectoryName(csprojPath)!;
            // Chỉ coi là Q2 khi có Views hoặc Pages (tránh nhận nhầm Web API dùng ApiController)
            return Directory.Exists(Path.Combine(dir, "Views"))
                || Directory.Exists(Path.Combine(dir, "Pages"));
        }

        private static string? PreferApiProject(System.Collections.Generic.IEnumerable<string> projects)
        {
            return projects.FirstOrDefault(IsLikelyWebApi) ?? projects.FirstOrDefault();
        }
    }
}
