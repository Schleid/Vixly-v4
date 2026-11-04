using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Security;

namespace Vixly.Operations.Application.UseCases.Plans.GetOperationsShipmentCap;

// =========================================================
// FILE: GetOperationsShipmentCapUseCase.cs
//
// PURPOSE:
// Gives the company's shipments this month against its plan's cap, for the workspace banner (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Shown from 80 percent of the cap; nothing without a cap
// =========================================================
public sealed class GetOperationsShipmentCapUseCase
{
    private readonly OperationsShipmentCap _cap;

    private readonly OperationsOwnerCheck _owner;

    private readonly TimeProvider _timeProvider;

    public GetOperationsShipmentCapUseCase(OperationsShipmentCap cap, OperationsOwnerCheck owner, TimeProvider timeProvider)
    {
        _cap = cap ?? throw new ArgumentNullException(nameof(cap));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsShipmentCapResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShipmentCapRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Only those who manage the company's settings see the plan's numbers; others get nothing, not an error.
        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.SettingsEdit, cancellationToken) is not null)
        {
            return GetOperationsShipmentCapResponse.Success(null);
        }

        OperationsShipmentCapStatus status = await _cap.StatusAsync(context.RequiredTenantId, _timeProvider.GetUtcNow(), cancellationToken);
        return GetOperationsShipmentCapResponse.Success(status.Capped && status.Level > 0 ? status : null);
    }
}
