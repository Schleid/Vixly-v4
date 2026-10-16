using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantAccount;

// =========================================================
// FILE: UpdateTenantAccountResponse.cs
//
// PURPOSE:
// The tenant changes its billing and technical contacts, language and time zone (call 16).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateTenantAccountResponse
{
    private UpdateTenantAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateTenantAccountResponse Success()
    {
        return new UpdateTenantAccountResponse { Succeeded = true };
    }

    public static UpdateTenantAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateTenantAccountResponse { Error = error };
    }
}
