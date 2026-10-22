namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsBillingDocument.cs
//
// PURPOSE:
// A billing PDF (invoice, receipt or credit note) Vixly issued to the company.
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsBillingDocument(string Kind, Guid DocumentId, string Number, string InvoiceNumber, string Language, DateTimeOffset CreatedUtc);
