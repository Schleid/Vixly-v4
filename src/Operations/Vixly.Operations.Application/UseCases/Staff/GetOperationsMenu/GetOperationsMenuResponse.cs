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

    public static GetOperationsMenuResponse Success(IReadOnlySet<string> permissions, bool isOwner, bool isAdministrator)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return new GetOperationsMenuResponse { Succeeded = true, Permissions = permissions, IsOwner = isOwner, IsAdministrator = isAdministrator };
    }

    public static GetOperationsMenuResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsMenuResponse { Error = error };
    }
}
