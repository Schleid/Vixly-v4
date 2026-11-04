using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.StartOperationsShelfCount;

// =========================================================
// FILE: StartOperationsShelfCountResponse.cs
//
// PURPOSE:
// Starts the count of one shelf, by its scanned code (Slice 12D, owner H-7); an open count of that shelf goes on.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsShelfCountResponse
{
    private StartOperationsShelfCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CountId { get; private init; }

    public static StartOperationsShelfCountResponse Success(Guid countId)
    {
        return new StartOperationsShelfCountResponse { Succeeded = true, CountId = countId };
    }

    public static StartOperationsShelfCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsShelfCountResponse { Error = error };
    }
}
