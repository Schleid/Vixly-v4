using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.CheckOperationsPayment;

// =========================================================
// FILE: CheckOperationsPaymentRequest.cs
//
// PURPOSE:
// Finance's check of a Zelle or transfer (Slice 5F, C-6): received settles it; not received takes it back, the invoice is owed again and the customer is told.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CheckOperationsPaymentRequest
{
    public Guid PaymentId { get; init; }

    public Guid TenderId { get; init; }

    public bool Received { get; init; }

    // The bank's reference when received; why when not received.
    public string? Note { get; init; }

    // The portal address for the customer's email.
    public string PortalSlug { get; init; } = string.Empty;
}
