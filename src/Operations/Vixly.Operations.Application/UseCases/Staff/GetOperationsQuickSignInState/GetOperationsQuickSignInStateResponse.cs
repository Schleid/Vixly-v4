using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsQuickSignInState;

// =========================================================
// FILE: GetOperationsQuickSignInStateResponse.cs
//
// PURPOSE:
// Whether a person has a badge in use and a PIN (Slice 12A): for the staff page, or the person's own account.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsQuickSignInStateResponse
{
    private GetOperationsQuickSignInStateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool HasBadge { get; private init; }

    public DateTimeOffset? BadgeIssuedUtc { get; private init; }

    public bool HasPin { get; private init; }

    public bool PinLocked { get; private init; }

    public static GetOperationsQuickSignInStateResponse Success(bool hasBadge, DateTimeOffset? badgeIssuedUtc, bool hasPin, bool pinLocked)
    {
        return new GetOperationsQuickSignInStateResponse { Succeeded = true, HasBadge = hasBadge, BadgeIssuedUtc = badgeIssuedUtc, HasPin = hasPin, PinLocked = pinLocked };
    }

    public static GetOperationsQuickSignInStateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsQuickSignInStateResponse { Error = error };
    }
}
