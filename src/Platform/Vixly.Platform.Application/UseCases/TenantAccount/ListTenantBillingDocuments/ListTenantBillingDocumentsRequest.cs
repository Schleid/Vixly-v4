namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantBillingDocuments;

// =========================================================
// FILE: ListTenantBillingDocumentsRequest.cs
//
// PURPOSE:
// The tenant's invoice, credit note and receipt PDFs, newest first (call 22).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListTenantBillingDocumentsRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
