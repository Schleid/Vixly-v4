using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge;

// =========================================================
// FILE: RevokeOperationsStaffBadgeResponse.cs
//
// PURPOSE:
// Revokes a staff member's badge, for example when it is lost (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokeOperationsStaffBadgeResponse
{
    private RevokeOperationsStaffBadgeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Revoked { get; private init; }

    public static RevokeOperationsStaffBadgeResponse Success(bool revoked)
    {
        return new RevokeOperationsStaffBadgeResponse { Succeeded = true, Revoked = revoked };
    }

    public static RevokeOperationsStaffBadgeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokeOperationsStaffBadgeResponse { Error = error };
    }
}
