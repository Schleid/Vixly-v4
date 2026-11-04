using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakePlanFeatures.cs
//
// PURPOSE:
// Stands in for the plan's branding features (W-4).
//
// RESPONSIBILITIES:
// - Answer the features a test sets, per tenant
//
// IMPORTANT NOTES:
// - Tests only; every feature by default
// =========================================================
internal sealed class FakePlanFeatures : IOperationsPlanFeatures
{
    public Dictionary<Guid, OperationsPlanFeatures> Tenants { get; } = new();

    public Task<OperationsPlanFeatures> GetAsync(Guid tenantId, CancellationToken cancellationToken) =>
        Task.FromResult(Tenants.TryGetValue(tenantId, out OperationsPlanFeatures? features) ? features : OperationsPlanFeatures.All);
}
