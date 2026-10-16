namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardEvent.cs
//
// PURPOSE:
// A verified provider event in provider-neutral terms.
//
// RESPONSIBILITIES:
// - Carry a verified provider event in provider-neutral terms
//
// IMPORTANT NOTES:
// - Built only after the signature was checked
// - Serialized into the outbox; it holds no card data
// =========================================================
public sealed record PlatformCardEvent(
    string EventId,
    string EventType,
    PlatformCardEventKind Kind,
    string? ObjectId,
    string? CheckoutId,
    string? PaymentId,
    string? RefundId,
    string? PaymentMethodId,
    string? CustomerId,
    string? FailureCode,
    bool? DisputeWon,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset OccurredUtc);
