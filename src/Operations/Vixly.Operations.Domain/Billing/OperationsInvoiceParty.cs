namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsInvoiceParty.cs
//
// PURPOSE:
// Who an invoice is from and to, as at issue (Slice 5F, I-2).
//
// RESPONSIBILITIES:
// - Keep the company's name and tax number and the issuing warehouse
// - Keep the customer's code, name, company, phone and email
//
// IMPORTANT NOTES:
// - The company own name, never "Vixly"
// =========================================================
public sealed record OperationsInvoiceParty(
    string CompanyName,
    string? TaxNumber,
    string WarehouseName,
    string? CustomerCode,
    string CustomerName,
    string? CustomerCompany,
    string CustomerPhone,
    string CustomerEmail);
