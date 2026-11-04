using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Events.AddOperationsEvent;

// =========================================================
// FILE: AddOperationsEventRequest.cs
//
// PURPOSE:
// Record a note, a delay or an exception by hand (Slice 6C, E-1 to E-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddOperationsEventRequest
{
    public OperationsTrackingSubject Kind { get; init; }

    public Guid Id { get; init; }

    public OperationsEventKind EventKind { get; init; }

    // A delay's or an exception's cause: a standard code or the company's own.
    public string? Cause { get; init; }

    // The short line the customer sees beside the cause.
    public string? Message { get; init; }

    // Internal: the note itself, or staff context.
    public string? Note { get; init; }

    // When it happened, where the company works; none: now.
    public DateTime? At { get; init; }

    public Guid? RequestKey { get; init; }

    public string PortalSlug { get; init; } = string.Empty;
}
