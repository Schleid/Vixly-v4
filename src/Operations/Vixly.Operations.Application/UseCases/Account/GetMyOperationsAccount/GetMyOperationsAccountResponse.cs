using Vixly.Operations.Application.Staff;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.GetMyOperationsAccount;

// =========================================================
// FILE: GetMyOperationsAccountResponse.cs
//
// PURPOSE:
// Reads my account: second factor, recovery codes left and open sessions (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsAccountResponse
{
    private GetMyOperationsAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsStaffMember? Me { get; private init; }

    public int RecoveryCodesLeft { get; private init; }

    public IReadOnlyList<OperationsSessionSummary> Sessions { get; private init; } = Array.Empty<OperationsSessionSummary>();

    public bool StepUpFresh { get; private init; }

    public static GetMyOperationsAccountResponse Success(OperationsStaffMember? me, int recoveryCodesLeft, IReadOnlyList<OperationsSessionSummary> sessions, bool stepUpFresh)
    {
        return new GetMyOperationsAccountResponse { Succeeded = true, Me = me, RecoveryCodesLeft = recoveryCodesLeft, Sessions = sessions, StepUpFresh = stepUpFresh };
    }

    public static GetMyOperationsAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsAccountResponse { Error = error };
    }
}
