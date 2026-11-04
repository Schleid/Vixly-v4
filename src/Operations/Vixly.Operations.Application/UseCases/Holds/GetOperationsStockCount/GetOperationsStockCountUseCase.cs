using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsStockCount;

// =========================================================
// FILE: GetOperationsStockCountUseCase.cs
//
// PURPOSE:
// One count of the undeclared parcels area, with what each parcel was found to be (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the count's warehouse; another warehouse's count answers "not found"
// =========================================================
public sealed class GetOperationsStockCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsStockCountRepository _counts;

    public GetOperationsStockCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsStockCountRepository counts)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
    }

    public async Task<GetOperationsStockCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsStockCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsStockCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _counts.GetAsync(request.CountId, cancellationToken) is not { } count
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsAssign, count.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return GetOperationsStockCountResponse.Failure(OperationsHoldErrors.CountNotFound);
        }

        bool manager = access.IsOwner || access.IsAdministrator;
        return GetOperationsStockCountResponse.Success(count, warehouse, count.IsOpen && count.Lines.All(line => line.Result != OperationsCountResult.Expected), manager);
    }
}
