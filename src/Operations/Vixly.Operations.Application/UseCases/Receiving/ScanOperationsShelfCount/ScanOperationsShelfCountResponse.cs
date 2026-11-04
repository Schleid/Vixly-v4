using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ScanOperationsShelfCount;

// =========================================================
// FILE: ScanOperationsShelfCountResponse.cs
//
// PURPOSE:
// Records one code scanned on the shelf being counted (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScanOperationsShelfCountResponse
{
    private ScanOperationsShelfCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Added { get; private init; }

    public static ScanOperationsShelfCountResponse Success(bool added)
    {
        return new ScanOperationsShelfCountResponse { Succeeded = true, Added = added };
    }

    public static ScanOperationsShelfCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScanOperationsShelfCountResponse { Error = error };
    }
}
