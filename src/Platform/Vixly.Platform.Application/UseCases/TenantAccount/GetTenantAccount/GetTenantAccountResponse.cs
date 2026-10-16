using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantAccount;

// =========================================================
// FILE: GetTenantAccountResponse.cs
//
// PURPOSE:
// The tenant reads its own account and how to reach Vixly support (calls 15, 17, 18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantAccountResponse
{
    private GetTenantAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantAccountSummary? Account { get; private init; }

    public static GetTenantAccountResponse Success(PlatformTenantAccountSummary? account)
    {
        return new GetTenantAccountResponse { Succeeded = true, Account = account };
    }

    public static GetTenantAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetTenantAccountResponse { Error = error };
    }
}
