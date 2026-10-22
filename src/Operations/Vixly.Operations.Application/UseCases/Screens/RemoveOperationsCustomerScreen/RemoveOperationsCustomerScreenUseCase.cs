using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.RemoveOperationsCustomerScreen;

// =========================================================
// FILE: RemoveOperationsCustomerScreenUseCase.cs
//
// PURPOSE:
// Removes a customer screen; it stops working at once (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve or ops.payments.take at the screen’s warehouse
// =========================================================
public sealed class RemoveOperationsCustomerScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public RemoveOperationsCustomerScreenUseCase(
        OperationsScreenDesk desk,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RemoveOperationsCustomerScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsCustomerScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsCustomerScreenResponse.Failure(refused);
        }

        if (await _desk.Screens.GetAsync(request.ScreenId, cancellationToken) is not { } screen || warehouses.All(warehouse => warehouse.Id != screen.WarehouseId))
        {
            return RemoveOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
        }

        _desk.Screens.Remove(screen);
        _audit.Record(access!.User.Id, "ops.screens.removed", nameof(OperationsCustomerScreen), screen.Id, before: new { screen.Name });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsCustomerScreenResponse.Success()
            : RemoveOperationsCustomerScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
