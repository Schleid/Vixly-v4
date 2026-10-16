using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformMerchantAccount;

// =========================================================
// FILE: UpdatePlatformMerchantAccountResponse.cs
//
// PURPOSE:
// Changes the publishable key, accepted currencies and whether card payments are on (D-P8, D-P11).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformMerchantAccountResponse
{
    private UpdatePlatformMerchantAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformMerchantAccountResponse Success()
    {
        return new UpdatePlatformMerchantAccountResponse { Succeeded = true };
    }

    public static UpdatePlatformMerchantAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformMerchantAccountResponse { Error = error };
    }
}
