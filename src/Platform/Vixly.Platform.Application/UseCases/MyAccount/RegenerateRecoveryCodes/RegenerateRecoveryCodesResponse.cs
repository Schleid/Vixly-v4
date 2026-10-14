using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.RegenerateRecoveryCodes;

// =========================================================
// FILE: RegenerateRecoveryCodesResponse.cs
//
// PURPOSE:
// Result of RegenerateRecoveryCodes.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - The codes are shown once and never stored in clear
// =========================================================
public sealed class RegenerateRecoveryCodesResponse
{
    private RegenerateRecoveryCodesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<string> Codes { get; private init; } = Array.Empty<string>();

    public static RegenerateRecoveryCodesResponse Success(IReadOnlyList<string> codes)
    {
        return new RegenerateRecoveryCodesResponse { Succeeded = true, Codes = codes };
    }

    public static RegenerateRecoveryCodesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RegenerateRecoveryCodesResponse { Error = error };
    }
}
