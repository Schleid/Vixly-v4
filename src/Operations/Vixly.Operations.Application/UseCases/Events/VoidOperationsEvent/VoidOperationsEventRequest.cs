namespace Vixly.Operations.Application.UseCases.Events.VoidOperationsEvent;

// =========================================================
// FILE: VoidOperationsEventRequest.cs
//
// PURPOSE:
// Void an event recorded by hand, with a reason (Slice 6C, E-7): its copies on parcels go with it.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VoidOperationsEventRequest
{
    public Guid EventId { get; init; }

    public string? Reason { get; init; }
}
