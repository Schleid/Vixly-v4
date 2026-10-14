namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: RepositoryLocator.cs
//
// PURPOSE:
// Finds the repository root and enumerates source files for
// the architecture rules.
//
// RESPONSIBILITIES:
// - Walk up from the test output folder to the repository root
// - List project files and C# files, skipping build output
//
// IMPORTANT NOTES:
// - The root is the first folder containing AGENTS.md and
//   global.json
// - Generated code and EF migrations are excluded because they
//   are not hand-written files
// =========================================================
internal static class RepositoryLocator
{
    private static readonly string[] ExcludedSegments =
    {
        "bin", "obj", "Migrations", ".git", "node_modules"
    };

    public static string Root { get; } = FindRoot();

    public static IReadOnlyList<string> ProjectFiles()
    {
        return Enumerate(Path.Combine(Root, "src"), "*.csproj").ToList();
    }

    public static IReadOnlyList<string> CSharpFiles()
    {
        return Enumerate(Path.Combine(Root, "src"), "*.cs")
            .Concat(Enumerate(Path.Combine(Root, "tests"), "*.cs"))
            .Where(path => !IsGenerated(path))
            .ToList();
    }

    private static IEnumerable<string> Enumerate(string folder, string pattern)
    {
        if (!Directory.Exists(folder))
        {
            return Enumerable.Empty<string>();
        }

        return Directory
            .EnumerateFiles(folder, pattern, SearchOption.AllDirectories)
            .Where(path => !path
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => ExcludedSegments.Contains(segment)));
    }

    private static bool IsGenerated(string path)
    {
        string name = Path.GetFileName(path);

        return name.EndsWith(".g.cs", StringComparison.Ordinal)
            || name.EndsWith(".Designer.cs", StringComparison.Ordinal)
            || name.EndsWith(".AssemblyInfo.cs", StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                && File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Repository root (AGENTS.md and global.json) was not found above the test output folder.");
    }
}
