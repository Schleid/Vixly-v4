using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.FinishOperationsInspection;

// =========================================================
// FILE: FinishOperationsInspectionResponse.cs
//
// PURPOSE:
// Records what the inspector found and decided: passed, held for the customer, repacked or refused (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FinishOperationsInspectionResponse
{
    private FinishOperationsInspectionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Result { get; private init; } = string.Empty;

    public static FinishOperationsInspectionResponse Success(string result)
    {
        return new FinishOperationsInspectionResponse { Succeeded = true, Result = result };
    }

    public static FinishOperationsInspectionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FinishOperationsInspectionResponse { Error = error };
    }
}
