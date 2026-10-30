using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.FinishOperationsPrintJob;

// =========================================================
// FILE: FinishOperationsPrintJobResponse.cs
//
// PURPOSE:
// Records what the dock print program did with a label (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FinishOperationsPrintJobResponse
{
    private FinishOperationsPrintJobResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid JobId { get; private init; }

    public static FinishOperationsPrintJobResponse Success(Guid jobId)
    {
        return new FinishOperationsPrintJobResponse { Succeeded = true, JobId = jobId };
    }

    public static FinishOperationsPrintJobResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FinishOperationsPrintJobResponse { Error = error };
    }
}
