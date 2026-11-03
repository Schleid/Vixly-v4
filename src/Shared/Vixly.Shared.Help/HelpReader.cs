
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpReader.cs
//
// PURPOSE:
// Filters articles using any permission and all conditions.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public sealed class HelpReader
{
    private readonly HashSet<string> permissions;
    private readonly HashSet<string> conditions;
    private readonly bool signedIn;
    private readonly bool superUser;

    public HelpReader(IEnumerable<string> permissions, IEnumerable<string> conditions, bool signedIn = true, bool superUser = false)
    {
        this.permissions = new(permissions ?? throw new ArgumentNullException(nameof(permissions)), StringComparer.Ordinal);
        this.conditions = new(conditions ?? throw new ArgumentNullException(nameof(conditions)), StringComparer.Ordinal);
        this.signedIn = signedIn;
        this.superUser = superUser;
    }

    public bool CanRead(HelpArticle article)
    {
        ArgumentNullException.ThrowIfNull(article);
        HelpFrontMatter metadata = article.Metadata;
        return (signedIn || metadata.SignedOut)
            && (superUser || metadata.Permissions.Count == 0 || metadata.Permissions.Any(permissions.Contains))
            && metadata.Conditions.All(conditions.Contains);
    }
}
