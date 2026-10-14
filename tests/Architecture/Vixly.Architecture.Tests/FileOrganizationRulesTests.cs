using System.Text.RegularExpressions;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: FileOrganizationRulesTests.cs
//
// PURPOSE:
// Enforces the file header and formatting rules of Global
// Standard 5.1 and 5.4 on every hand-written C# file.
//
// RESPONSIBILITIES:
// - Require the banner header with FILE, PURPOSE,
//   RESPONSIBILITIES and IMPORTANT NOTES, in that order
// - Require the header directly above the type
// - Require UTF-8 with BOM and CRLF line endings
// - Require one top-level type per file, named after the file
//
// IMPORTANT NOTES:
// - Generated files and EF migrations are excluded
// - Razor files get the same check when the first Web project
//   is added
// =========================================================
public sealed class FileOrganizationRulesTests
{
    private static readonly string Banner = "// " + new string('=', 57);

    public static TheoryData<string> AllFiles()
    {
        TheoryData<string> data = new();

        foreach (string path in RepositoryLocator.CSharpFiles())
        {
            data.Add(Path.GetRelativePath(RepositoryLocator.Root, path));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void File_HasTheStandardHeader(string relativePath)
    {
        string[] lines = File.ReadAllText(FullPath(relativePath))
            .Replace("\r\n", "\n")
            .Split('\n');

        int open = Array.FindIndex(lines, line => line == Banner);

        Assert.True(open >= 0, $"{relativePath}: header banner (57 '=') not found.");

        string expectedFile = $"// FILE: {Path.GetFileName(relativePath)}";
        int fileLine = Array.IndexOf(lines, expectedFile, open);
        int purpose = Array.IndexOf(lines, "// PURPOSE:", open);
        int responsibilities = Array.IndexOf(lines, "// RESPONSIBILITIES:", open);
        int notes = Array.IndexOf(lines, "// IMPORTANT NOTES:", open);
        int close = Array.FindIndex(lines, notes < 0 ? open : notes, line => line == Banner);

        Assert.True(fileLine > open, $"{relativePath}: expected '{expectedFile}'.");
        Assert.True(purpose > fileLine, $"{relativePath}: PURPOSE missing or out of order.");
        Assert.True(responsibilities > purpose, $"{relativePath}: RESPONSIBILITIES missing or out of order.");
        Assert.True(notes > responsibilities, $"{relativePath}: IMPORTANT NOTES missing or out of order.");
        Assert.True(close > notes, $"{relativePath}: closing banner missing.");

        string below = lines.Length > close + 1 ? lines[close + 1] : string.Empty;

        Assert.True(
            below.Length > 0 && !below.StartsWith("//", StringComparison.Ordinal),
            $"{relativePath}: the header must sit directly above the type declaration.");
    }

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void File_IsUtf8WithBomAndCrlf(string relativePath)
    {
        byte[] bytes = File.ReadAllBytes(FullPath(relativePath));

        Assert.True(
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            $"{relativePath}: must be UTF-8 with BOM.");

        string text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.False(
            Regex.IsMatch(text, "(?<!\r)\n"),
            $"{relativePath}: must use CRLF line endings.");
    }

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void File_NameMatchesItsSingleTopLevelType(string relativePath)
    {
        string name = Path.GetFileNameWithoutExtension(relativePath);

        // Test classes and rule classes follow the same one-type-per-file rule.
        MatchCollection types = Regex.Matches(
            File.ReadAllText(FullPath(relativePath)),
            @"^(?:public|internal)\s+(?:(?:sealed|static|abstract|readonly|partial)\s+)*(?:class|record|interface|enum|struct)\s+(?:struct\s+)?([A-Za-z0-9_]+)",
            RegexOptions.Multiline);

        Assert.True(types.Count == 1, $"{relativePath}: exactly one top-level type per file (found {types.Count}).");
        Assert.True(
            types[0].Groups[1].Value == name,
            $"{relativePath}: file name must equal the type name ({types[0].Groups[1].Value}).");
    }

    private static string FullPath(string relativePath)
    {
        return Path.Combine(RepositoryLocator.Root, relativePath);
    }
}
