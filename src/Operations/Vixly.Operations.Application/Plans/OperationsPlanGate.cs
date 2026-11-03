using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Plans;

// =========================================================
// FILE: OperationsPlanGate.cs
//
// PURPOSE:
// Whether a module is in the current company's plan (Vixly Lite, owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Read the plan features of the company in scope and answer for one module
//
// IMPORTANT NOTES:
// - Without a company in scope (the system, a worker between companies), everything is included
// - Platform not reachable: the features keep the modules on (OperationsPlanFeaturesClient); a counter never stops because of the plan service
// =========================================================
public sealed class OperationsPlanGate
{
    private readonly IOperationsTenantScope _scope;

    private readonly IOperationsPlanFeatures _features;

    public OperationsPlanGate(IOperationsTenantScope scope, IOperationsPlanFeatures features)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<bool> IncludesAsync(string module, CancellationToken cancellationToken) =>
        _scope.TenantId is not { } tenantId || (await _features.GetAsync(tenantId, cancellationToken)).Includes(module);
}
