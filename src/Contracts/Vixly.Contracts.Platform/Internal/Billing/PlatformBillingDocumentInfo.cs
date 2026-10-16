namespace Vixly.Contracts.Platform.Internal.Billing;

// =========================================================
// FILE: PlatformBillingDocumentInfo.cs
//
// PURPOSE:
// One of the tenant's billing PDFs (call 22).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Kind: invoice, credit_note, receipt
// - FileUrl is relative to the internal base address
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformBillingDocumentInfo(string Kind, Guid DocumentId, string Number, Guid InvoiceId, string InvoiceNumber, string Language, DateTimeOffset CreatedUtc, string FileUrl);
