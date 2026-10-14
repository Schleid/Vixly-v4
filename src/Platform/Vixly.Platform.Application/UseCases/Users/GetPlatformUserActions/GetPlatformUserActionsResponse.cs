using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;

// =========================================================
// FILE: GetPlatformUserActionsResponse.cs
//
// PURPOSE:
// Result of GetPlatformUserActions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - For showing buttons only; never authorization (S-1)
// =========================================================
public sealed class GetPlatformUserActionsResponse
{
    private GetPlatformUserActionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyCollection<PlatformUserAdminAction> Actions { get; private init; } = Array.Empty<PlatformUserAdminAction>();

    public static GetPlatformUserActionsResponse Success(IReadOnlyCollection<PlatformUserAdminAction> actions)
    {
        return new GetPlatformUserActionsResponse { Succeeded = true, Actions = actions };
    }

    public static GetPlatformUserActionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformUserActionsResponse { Error = error };
    }
}
