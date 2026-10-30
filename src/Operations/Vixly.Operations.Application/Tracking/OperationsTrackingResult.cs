using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Tracking;

// =========================================================
// FILE: OperationsTrackingResult.cs
//
// PURPOSE:
// The answer of OperationsTracker.RecordAsync (Slice 3A).
//
// RESPONSIBILITIES:
// - Say which event stands for the command and whether it is new
//
// IMPORTANT NOTES:
// - A repeat returns the first event with Created = false (ADR-0021 decision 4)
// =========================================================
public sealed record OperationsTrackingResult(OperationsTrackingEvent? Event, bool Created, Error? Error)
{
    public static OperationsTrackingResult New(OperationsTrackingEvent trackingEvent) => new(trackingEvent, true, null);

    public static OperationsTrackingResult Repeat(OperationsTrackingEvent trackingEvent) => new(trackingEvent, false, null);

    public static OperationsTrackingResult Failure(Error error) => new(null, false, error);
}
