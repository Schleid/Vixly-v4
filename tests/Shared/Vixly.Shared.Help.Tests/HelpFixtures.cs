using Vixly.Shared.Help;

namespace Vixly.Shared.Help.Tests;

// =========================================================
// FILE: HelpFixtures.cs
//
// PURPOSE:
// Defines or verifies the shared help foundation.
//
// RESPONSIBILITIES:
// - Support the help contract of ADR-0023.
//
// IMPORTANT NOTES:
// - No plane implementation dependencies.
// =========================================================
public static class HelpFixtures
{
    public static HelpOptions Options => new()
    {
        Sections = new HashSet<string> { "start" },
        Permissions = new HashSet<string> { "read", "edit" },
        Conditions = new HashSet<string> { "cards", "credit" },
        Routes = new HashSet<string> { "/home" },
        Tokens = new HashSet<string> { "brandName", "portalAddress" }
    };

    public static string Markdown(string slug = "welcome", string body = "## Steps\n1. Open the café.") =>
        $"---\nslug: {slug}\ntitle: Café welcome\nsummary: Learn to start.\nsection: start\norder: 1\npermissions: []\nconditions: []\nroutes: []\nrelated: []\nsources:\n  - src/example.cs\nupdated: 2026-10-22\nreview: ok\n---\n{body}";

    public static HelpDocument[] Documents(string markdown) =>
        new[] { "en", "fr", "es" }.Select(language => new HelpDocument(language, language + "/welcome.md", markdown)).ToArray();

    public static HelpReader Reader => new([], []);
}
