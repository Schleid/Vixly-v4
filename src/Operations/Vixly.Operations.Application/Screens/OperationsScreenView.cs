using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Screens;

// =========================================================
// FILE: OperationsScreenView.cs
//
// PURPOSE:
// What a customer screen shows for what was sent to it (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Carry the invoice and how it can be paid on the screen (card, Zelle with its details and terms), the card's state, or the declaration to sign
//
// IMPORTANT NOTES:
// - Cash and the other methods a person records are not offered on the screen (P-7): the customer tells the clerk
// =========================================================
public sealed record OperationsScreenView
{
    public OperationsInvoice? Invoice { get; init; }

    // Nothing left to pay (paid, or waiting for finance to see a Zelle payment).
    public bool Settled { get; init; }

    public bool CardOffered { get; init; }

    public OperationsCardChargeStatus? CardStatus { get; init; }

    public string? CardPageUrl { get; init; }

    public bool ZelleOffered { get; init; }

    public bool ZelleHasQr { get; init; }

    public string? ZelleTag { get; init; }

    public string? ZelleContact { get; init; }

    public string? ZelleName { get; init; }

    public string? ZelleTerms { get; init; }

    public bool ZelleAccepted { get; init; }

    public OperationsCustomerDeclaration? Declaration { get; init; }
}
