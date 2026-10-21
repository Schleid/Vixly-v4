using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.RemoveOperationsScheduledVersion;

// =========================================================
// FILE: RemoveOperationsScheduledVersionResponse.cs
//
// PURPOSE:
// Removes a version that has not started yet: a price list, an extra or the insurance plan (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsScheduledVersionResponse
{
    private RemoveOperationsScheduledVersionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsScheduledVersionResponse Success()
    {
        return new RemoveOperationsScheduledVersionResponse { Succeeded = true };
    }

    public static RemoveOperationsScheduledVersionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsScheduledVersionResponse { Error = error };
    }
}
