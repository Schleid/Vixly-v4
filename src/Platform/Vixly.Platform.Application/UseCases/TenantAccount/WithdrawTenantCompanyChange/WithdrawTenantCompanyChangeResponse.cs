using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.WithdrawTenantCompanyChange;

// =========================================================
// FILE: WithdrawTenantCompanyChangeResponse.cs
//
// PURPOSE:
// Answer to a withdrawal (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class WithdrawTenantCompanyChangeResponse
{
    private WithdrawTenantCompanyChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static WithdrawTenantCompanyChangeResponse Success()
    {
        return new WithdrawTenantCompanyChangeResponse { Succeeded = true };
    }

    public static WithdrawTenantCompanyChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new WithdrawTenantCompanyChangeResponse { Error = error };
    }
}
