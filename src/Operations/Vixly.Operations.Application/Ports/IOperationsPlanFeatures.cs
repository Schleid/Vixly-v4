namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPlanFeatures.cs
//
// PURPOSE:
// Reads the branding features of a tenant's plan from Platform (W-4).
//
// RESPONSIBILITIES:
// - Answer from a short cache; never fail the public site
//
// IMPORTANT NOTES:
// - When Platform cannot answer, the site shows the tenant's own choices (fail open: cosmetic only)
// =========================================================
public interface IOperationsPlanFeatures
{
    Task<OperationsPlanFeatures> GetAsync(Guid tenantId, CancellationToken cancellationToken);
}
