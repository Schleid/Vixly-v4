using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ChangeOperationsInspectionShare;

// =========================================================
// FILE: ChangeOperationsInspectionShareResponse.cs
//
// PURPOSE:
// Changes how many parcels in a hundred are opened at random (Slice 3C, D-R4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsInspectionShareResponse
{
    private ChangeOperationsInspectionShareResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsInspectionShareResponse Success()
    {
        return new ChangeOperationsInspectionShareResponse { Succeeded = true };
    }

    public static ChangeOperationsInspectionShareResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsInspectionShareResponse { Error = error };
    }
}
