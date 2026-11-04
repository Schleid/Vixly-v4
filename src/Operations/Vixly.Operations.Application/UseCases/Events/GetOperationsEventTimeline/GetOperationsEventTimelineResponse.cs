using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.GetOperationsEventTimeline;

// =========================================================
// FILE: GetOperationsEventTimelineResponse.cs
//
// PURPOSE:
// One subject's history in the events workspace (Slice 6C): its events newest first, its parcels, and what can be added.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsEventTimelineResponse
{
    private GetOperationsEventTimelineResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsEventSubject Subject { get; private init; } = null!;

    public IReadOnlyList<OperationsTrackingLine> Lines { get; private init; } = Array.Empty<OperationsTrackingLine>();

    public int Pages { get; private init; }

    public IReadOnlyList<OperationsParcel> Parcels { get; private init; } = Array.Empty<OperationsParcel>();

    public bool CanAdd { get; private init; }

    public IReadOnlyList<OperationsEventCause> DelayCauses { get; private init; } = Array.Empty<OperationsEventCause>();

    public IReadOnlyList<OperationsEventCause> ExceptionCauses { get; private init; } = Array.Empty<OperationsEventCause>();

    public string? TimeZone { get; private init; }

    public static GetOperationsEventTimelineResponse Success(OperationsEventSubject subject, IReadOnlyList<OperationsTrackingLine> lines, int pages, IReadOnlyList<OperationsParcel> parcels, bool canAdd, IReadOnlyList<OperationsEventCause> delayCauses, IReadOnlyList<OperationsEventCause> exceptionCauses, string? timeZone)
    {
        return new GetOperationsEventTimelineResponse { Succeeded = true, Subject = subject, Lines = lines, Pages = pages, Parcels = parcels, CanAdd = canAdd, DelayCauses = delayCauses, ExceptionCauses = exceptionCauses, TimeZone = timeZone };
    }

    public static GetOperationsEventTimelineResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsEventTimelineResponse { Error = error };
    }
}
