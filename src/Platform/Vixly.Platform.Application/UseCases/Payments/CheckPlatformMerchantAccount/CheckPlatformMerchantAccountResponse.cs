using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.CheckPlatformMerchantAccount;

// =========================================================
// FILE: CheckPlatformMerchantAccountResponse.cs
//
// PURPOSE:
// Asks the card provider whether the configured keys work, and records the answer (D-P9).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckPlatformMerchantAccountResponse
{
    private CheckPlatformMerchantAccountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Connected { get; private init; }

    public string? ProblemCode { get; private init; }

    public static CheckPlatformMerchantAccountResponse Success(bool connected, string? problemCode)
    {
        return new CheckPlatformMerchantAccountResponse { Succeeded = true, Connected = connected, ProblemCode = problemCode };
    }

    public static CheckPlatformMerchantAccountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckPlatformMerchantAccountResponse { Error = error };
    }
}
