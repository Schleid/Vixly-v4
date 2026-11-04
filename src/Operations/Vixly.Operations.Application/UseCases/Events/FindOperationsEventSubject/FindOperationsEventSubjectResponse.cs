using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.FindOperationsEventSubject;

// =========================================================
// FILE: FindOperationsEventSubjectResponse.cs
//
// PURPOSE:
// Find what a number typed in the events workspace is (Slice 6C): a parcel, a shipment, a bundle or a dispatch.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class FindOperationsEventSubjectResponse
{
    private FindOperationsEventSubjectResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTrackingSubject Kind { get; private init; }

    public Guid Id { get; private init; }

    public static FindOperationsEventSubjectResponse Success(OperationsTrackingSubject kind, Guid id)
    {
        return new FindOperationsEventSubjectResponse { Succeeded = true, Kind = kind, Id = id };
    }

    public static FindOperationsEventSubjectResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new FindOperationsEventSubjectResponse { Error = error };
    }
}
