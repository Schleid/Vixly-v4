using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Api.Tests;

// =========================================================
// FILE: AllPlanFeatures.cs
//
// PURPOSE:
// Plan features for the API tests (Slice 2).
//
// RESPONSIBILITIES:
// - Answer that every feature is in the plan
//
// IMPORTANT NOTES:
// - Test-only
// =========================================================
internal sealed class AllPlanFeatures : IOperationsPlanFeatures
{
    public Task<OperationsPlanFeatures> GetAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(new OperationsPlanFeatures(true, true));
}
