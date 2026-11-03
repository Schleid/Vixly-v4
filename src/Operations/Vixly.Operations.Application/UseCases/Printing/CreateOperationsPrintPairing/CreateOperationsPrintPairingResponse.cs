using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing;

// =========================================================
// FILE: CreateOperationsPrintPairingResponse.cs
//
// PURPOSE:
// Makes the one-time code a phone running Vixly Print scans to join a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsPrintPairingResponse
{
    private CreateOperationsPrintPairingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Code { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public static CreateOperationsPrintPairingResponse Success(string code, DateTimeOffset expiresUtc)
    {
        return new CreateOperationsPrintPairingResponse { Succeeded = true, Code = code, ExpiresUtc = expiresUtc };
    }

    public static CreateOperationsPrintPairingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsPrintPairingResponse { Error = error };
    }
}
