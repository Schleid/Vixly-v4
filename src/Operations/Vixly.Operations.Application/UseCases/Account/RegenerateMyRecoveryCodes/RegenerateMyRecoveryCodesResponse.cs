using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.RegenerateMyRecoveryCodes;

// =========================================================
// FILE: RegenerateMyRecoveryCodesResponse.cs
//
// PURPOSE:
// Replaces my ten recovery codes; the old ones stop working (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RegenerateMyRecoveryCodesResponse
{
    private RegenerateMyRecoveryCodesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<string> Codes { get; private init; } = Array.Empty<string>();

    public static RegenerateMyRecoveryCodesResponse Success(IReadOnlyList<string> codes)
    {
        return new RegenerateMyRecoveryCodesResponse { Succeeded = true, Codes = codes };
    }

    public static RegenerateMyRecoveryCodesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RegenerateMyRecoveryCodesResponse { Error = error };
    }
}
