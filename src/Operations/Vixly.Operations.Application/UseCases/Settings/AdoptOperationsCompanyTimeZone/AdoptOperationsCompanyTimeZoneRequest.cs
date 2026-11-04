namespace Vixly.Operations.Application.UseCases.Settings.AdoptOperationsCompanyTimeZone;

// =========================================================
// FILE: AdoptOperationsCompanyTimeZoneRequest.cs
//
// PURPOSE:
// The tenant and the company time zone Platform holds for it.
//
// RESPONSIBILITIES:
// - Carry the input of AdoptOperationsCompanyTimeZoneUseCase
//
// IMPORTANT NOTES:
// - CompanyTimeZone comes from Platform's tenant answer, never from a visitor
// =========================================================
public sealed class AdoptOperationsCompanyTimeZoneRequest
{
    public Guid TenantId { get; init; }

    public string? CompanyTimeZone { get; init; }
}
