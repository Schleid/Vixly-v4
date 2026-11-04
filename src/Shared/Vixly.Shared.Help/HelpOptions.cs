
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpOptions.cs
//
// PURPOSE:
// Supplies application-owned validation catalogues and link addresses.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public sealed class HelpOptions
{
    public IReadOnlySet<string> Sections { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Conditions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Routes { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Tokens { get; init; } = new HashSet<string>();
    public bool TenantBranded { get; init; }
    public bool Customer { get; init; }
    public string MediaPath { get; init; } = "/help/media/";
    public Func<string, string, string> ArticleAddress { get; init; } =
        (language, slug) => $"/{language}/help/{slug}";
}
