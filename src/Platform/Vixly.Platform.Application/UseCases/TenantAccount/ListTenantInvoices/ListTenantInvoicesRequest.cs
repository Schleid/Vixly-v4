namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantInvoices;

// =========================================================
// FILE: ListTenantInvoicesRequest.cs
//
// PURPOSE:
// The tenant's invoices, newest first (call 22).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListTenantInvoicesRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
