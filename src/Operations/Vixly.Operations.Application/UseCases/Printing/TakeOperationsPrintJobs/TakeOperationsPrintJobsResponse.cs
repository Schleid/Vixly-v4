using Vixly.Operations.Application.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.TakeOperationsPrintJobs;

// =========================================================
// FILE: TakeOperationsPrintJobsResponse.cs
//
// PURPOSE:
// Hands waiting labels to a dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TakeOperationsPrintJobsResponse
{
    private TakeOperationsPrintJobsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsPrintJobView> Jobs { get; private init; } = Array.Empty<OperationsPrintJobView>();

    public static TakeOperationsPrintJobsResponse Success(IReadOnlyList<OperationsPrintJobView> jobs)
    {
        return new TakeOperationsPrintJobsResponse { Succeeded = true, Jobs = jobs };
    }

    public static TakeOperationsPrintJobsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TakeOperationsPrintJobsResponse { Error = error };
    }
}
