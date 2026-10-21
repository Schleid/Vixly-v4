using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;

namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsPaymentSettings;

// =========================================================
// FILE: SaveOperationsPaymentSettingsRequest.cs
//
// PURPOSE:
// Change the payment methods the company accepts and its Zelle details (Slice 5F, C-1 and P-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsPaymentSettingsRequest
{
    public bool Cash { get; init; }

    public bool Zelle { get; init; }

    public bool Cheque { get; init; }

    public bool BankTransfer { get; init; }

    public string? Other1 { get; init; }

    public string? Other2 { get; init; }

    public string? Other3 { get; init; }

    public string? ZelleTag { get; init; }

    public string? ZelleContact { get; init; }

    public string? ZelleName { get; init; }

    public string? ZelleTermsEn { get; init; }

    public string? ZelleTermsFr { get; init; }

    public string? ZelleTermsEs { get; init; }

    // A new QR picture (PNG or JPEG, at most 1 MB); none keeps the current one.
    public byte[]? ZelleQr { get; init; }

    // True removes the current picture.
    public bool RemoveZelleQr { get; init; }
}
