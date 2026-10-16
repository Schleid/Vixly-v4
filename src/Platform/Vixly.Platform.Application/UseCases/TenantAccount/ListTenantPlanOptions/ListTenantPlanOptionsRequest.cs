namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantPlanOptions;

// =========================================================
// FILE: ListTenantPlanOptionsRequest.cs
//
// PURPOSE:
// Plans the tenant may move to by itself: public plans on sale and its current plan (call 20).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListTenantPlanOptionsRequest
{
    public string? Language { get; init; }
}
