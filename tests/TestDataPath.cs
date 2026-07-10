using System;
using System.IO;

namespace LimitByCraftingSkillMod.Tests
{
    internal static class TestDataPath
    {
        public static string GetRepoFile(string relativePath)
        {
            var directDataFile = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(relativePath));
            if (File.Exists(directDataFile))
            {
                return directDataFile;
            }

            var workspaceRoot = TryGetWorkspaceRoot();
            if (!string.IsNullOrEmpty(workspaceRoot))
            {
                var candidate = Path.Combine(workspaceRoot, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var manifestPath = Environment.GetEnvironmentVariable("RUNFILES_MANIFEST_FILE");
            if (!string.IsNullOrEmpty(manifestPath) && File.Exists(manifestPath))
            {
                var logicalNames = new[]
                {
                    $"_main/{relativePath.Replace('\\', '/')}",
                    $"{Environment.GetEnvironmentVariable("TEST_WORKSPACE")}/{relativePath.Replace('\\', '/')}",
                };

                foreach (var line in File.ReadLines(manifestPath))
                {
                    foreach (var logicalName in logicalNames)
                    {
                        if (line.StartsWith(logicalName + " ", StringComparison.Ordinal))
                        {
                            return line.Substring(logicalName.Length + 1);
                        }
                    }
                }
            }

            throw new FileNotFoundException($"Unable to resolve repo file '{relativePath}'.");
        }

        private static string TryGetWorkspaceRoot()
        {
            var testSrcDir = Environment.GetEnvironmentVariable("TEST_SRCDIR");
            var testWorkspace = Environment.GetEnvironmentVariable("TEST_WORKSPACE");

            if (!string.IsNullOrEmpty(testSrcDir) && !string.IsNullOrEmpty(testWorkspace))
            {
                var workspaceRoot = Path.Combine(testSrcDir, testWorkspace);
                if (Directory.Exists(workspaceRoot))
                {
                    return workspaceRoot;
                }
            }

            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "src")) &&
                    Directory.Exists(Path.Combine(current.FullName, "tests")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
