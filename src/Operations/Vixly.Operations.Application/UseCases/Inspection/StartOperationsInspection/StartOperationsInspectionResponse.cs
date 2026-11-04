using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.StartOperationsInspection;

// =========================================================
// FILE: StartOperationsInspectionResponse.cs
//
// PURPOSE:
// The inspector opens the box: the inspection is in progress (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsInspectionResponse
{
    private StartOperationsInspectionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StartOperationsInspectionResponse Success()
    {
        return new StartOperationsInspectionResponse { Succeeded = true };
    }

    public static StartOperationsInspectionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsInspectionResponse { Error = error };
    }
}
