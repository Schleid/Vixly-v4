using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsInspectionSteps.cs
//
// PURPOSE:
// Records the tracking events of an inspection or a hold, one after the other (Slice 3C).
//
// RESPONSIBILITIES:
// - Build each event with the staff member, the warehouse and its own idempotency key
// - Stop at the first refusal
//
// IMPORTANT NOTES:
// - Events are written in the caller's unit of work (ADR-0021)
// =========================================================
public sealed class OperationsInspectionSteps
{
    private readonly OperationsTracker _tracker;

    public OperationsInspectionSteps(OperationsTracker tracker)
    {
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    public async Task<Error?> RecordAsync(Guid parcelId, OperationsAccessView access, OperationsWarehouse warehouse, DateTimeOffset utcNow, CancellationToken cancellationToken,
        params (string Key, string Code, string? Argument, string? Value, string? Note)[] steps)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(warehouse);

        foreach ((string key, string code, string? argument, string? value, string? note) in steps)
        {
            Dictionary<string, string> arguments = new(StringComparer.Ordinal);

            if (argument is not null && value is not null)
            {
                arguments[argument] = value;
            }

            OperationsTrackingResult result = await _tracker.RecordAsync(new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcelId, code, arguments, utcNow, key,
                OperationsTrackingActor.Staff, OperationsTrackingSource.Manual)
            {
                ActorUserId = access.User.Id,
                LocationCode = warehouse.Code,
                Note = note
            }, cancellationToken);

            if (result.Error is { } refused)
            {
                return refused;
            }
        }

        return null;
    }
}
