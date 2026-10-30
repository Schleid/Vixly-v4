using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.VoidOperationsEvent;

// =========================================================
// FILE: VoidOperationsEventResponse.cs
//
// PURPOSE:
// Void an event recorded by hand, with a reason (Slice 6C, E-7): its copies on parcels go with it.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VoidOperationsEventResponse
{
    private VoidOperationsEventResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTrackingSubject Kind { get; private init; }

    public Guid SubjectId { get; private init; }

    public static VoidOperationsEventResponse Success(OperationsTrackingSubject kind, Guid subjectId)
    {
        return new VoidOperationsEventResponse { Succeeded = true, Kind = kind, SubjectId = subjectId };
    }

    public static VoidOperationsEventResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VoidOperationsEventResponse { Error = error };
    }
}
