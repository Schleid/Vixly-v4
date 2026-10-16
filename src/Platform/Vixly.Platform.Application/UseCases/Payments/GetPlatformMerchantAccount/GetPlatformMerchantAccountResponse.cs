using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformMerchantAccount;

// =========================================================
// FILE: GetPlatformMerchantAccountResponse.cs
//
// PURPOSE:
// Reads the merchant account and how the card provider is configured on this host (D-P8, D-P9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformMerchantAccountResponse
{
    private GetPlatformMerchantAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformMerchantAccountItem? Account { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetPlatformMerchantAccountResponse Success(PlatformMerchantAccountItem? account, bool canEdit)
    {
        return new GetPlatformMerchantAccountResponse { Succeeded = true, Account = account, CanEdit = canEdit };
    }

    public static GetPlatformMerchantAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformMerchantAccountResponse { Error = error };
    }
}
