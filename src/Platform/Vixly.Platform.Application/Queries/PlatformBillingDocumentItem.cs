using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformBillingDocumentItem.cs
//
// PURPOSE:
// One billing PDF in a list.
//
// RESPONSIBILITIES:
// - Carry what a document list shows
//
// IMPORTANT NOTES:
// - The content is never loaded in a list
// =========================================================
public sealed record PlatformBillingDocumentItem(PlatformBillingDocumentKind Kind, Guid OwnerId, string Number, Guid PlatformInvoiceId, string InvoiceNumber, string Language, DateTimeOffset CreatedUtc);
