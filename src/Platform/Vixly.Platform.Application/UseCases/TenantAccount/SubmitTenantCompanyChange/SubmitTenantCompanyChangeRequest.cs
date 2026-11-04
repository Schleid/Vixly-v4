using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount.SubmitTenantCompanyChange;

// =========================================================
// FILE: SubmitTenantCompanyChangeRequest.cs
//
// PURPOSE:
// A tenant owner’s request to change the company’s legal details (B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitTenantCompanyChangeRequest
{
    public PlatformCompanyInput Company { get; init; } = new();

    public string? Reason { get; init; }

    public string? ProofFileName { get; init; }

    public byte[]? ProofContent { get; init; }
}
