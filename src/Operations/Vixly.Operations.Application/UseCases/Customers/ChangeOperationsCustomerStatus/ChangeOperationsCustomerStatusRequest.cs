using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;

// =========================================================
// FILE: ChangeOperationsCustomerStatusRequest.cs
//
// PURPOSE:
// Staff suspend, reactivate or close a customer with a reason; the customer's sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCustomerStatusRequest
{
    public Guid CustomerId { get; init; }

    public OperationsCustomerStatus Status { get; init; }

    public string Reason { get; init; } = string.Empty;

    // Owner, 2026-10-31: a suspension may end by itself at the start of this day, in the company's time zone.
    public DateOnly? SuspendedUntil { get; init; }

    // For the email to the customer: the company's portal address.
    public string? PortalSlug { get; init; }
}
