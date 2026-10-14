using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetMyAccount;

// =========================================================
// FILE: GetMyAccountResponse.cs
//
// PURPOSE:
// Result of GetMyAccount.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyAccountResponse
{
    private GetMyAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformUserDetail? Account { get; private init; }

    public Guid? CurrentSessionId { get; private init; }

    public static GetMyAccountResponse Success(PlatformUserDetail? account, Guid? currentSessionId)
    {
        return new GetMyAccountResponse { Succeeded = true, Account = account, CurrentSessionId = currentSessionId };
    }

    public static GetMyAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyAccountResponse { Error = error };
    }
}
