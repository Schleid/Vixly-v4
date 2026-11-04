using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;

// =========================================================
// FILE: GetOperationsMenuResponse.cs
//
// PURPOSE:
// What the signed-in person may do, so the staff site shows only that (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - The permissions the person holds anywhere (any scope), and whether they are the Owner or an administrator
//
// IMPORTANT NOTES:
// - Nobody signed in holds nothing; the menu never grants access, every page and action still checks
// =========================================================
public sealed class GetOperationsMenuResponse
{
    private GetOperationsMenuResponse()
    {
    }

    // Nobody signed in, or no tenant: nothing is shown.
    public static GetOperationsMenuResponse Nothing { get; } = new();

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlySet<string> Permissions { get; private init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool IsOwner { get; private init; }

    public bool IsAdministrator { get; private init; }

    public bool Has(string permission) => Permissions.Contains(permission);

    public bool HasAny(IEnumerable<string> permissions) => permissions.Any(Permissions.Contains);

    // Vixly Lite (owner, 2026-10-30): the staff pages of the modules the plan leaves out.
    public IReadOnlyList<string> HiddenPages { get; private init; } = Array.Empty<string>();

    public bool Hides(string url) => Vixly.Operations.Application.Plans.OperationsPlanModules.Hides(HiddenPages, url);

    // Vixly Lite: without the customer portal, staff enter customers themselves.
    public bool PortalOff { get; private init; }

    // The modules the plan leaves out (OperationsPlanModules codes).
    public IReadOnlySet<string> ModulesOff { get; private init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool Includes(string module) => !ModulesOff.Contains(module);

    // Owner, 2026-11-04: what waits on a page, shown beside its menu entry (keyed by the entry's section, such as "site" or "verify").
    public IReadOnlyDictionary<string, int> Counts { get; private init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    public int CountOf(string section) => Counts.TryGetValue(section, out int count) ? count : 0;

    public GetOperationsMenuResponse WithCounts(IReadOnlyDictionary<string, int> counts) => new()
    {
        Succeeded = Succeeded, Error = Error, Permissions = Permissions, IsOwner = IsOwner, IsAdministrator = IsAdministrator, HiddenPages = HiddenPages, PortalOff = PortalOff,
        ModulesOff = ModulesOff, Counts = counts ?? throw new ArgumentNullException(nameof(counts))
    };

    public static GetOperationsMenuResponse Success(IReadOnlySet<string> permissions, bool isOwner, bool isAdministrator, IReadOnlyList<string>? hiddenPages = null, bool portalOff = false,
        IReadOnlySet<string>? modulesOff = null)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return new GetOperationsMenuResponse
        {
            Succeeded = true, Permissions = permissions, IsOwner = isOwner, IsAdministrator = isAdministrator, HiddenPages = hiddenPages ?? Array.Empty<string>(), PortalOff = portalOff,
            ModulesOff = modulesOff ?? new HashSet<string>(StringComparer.Ordinal)
        };
    }

    public static GetOperationsMenuResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsMenuResponse { Error = error };
    }
}
