using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.RemoveOperationsSupplyPrice;

// =========================================================
// FILE: RemoveOperationsSupplyPriceUseCase.cs
//
// PURPOSE:
// Remove a supply's price that has not started yet (Slice 5E, S-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// =========================================================
public sealed class RemoveOperationsSupplyPriceUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsSupplyPriceUseCase(
        OperationsAccess access,
        IOperationsSupplyRepository supplies,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsSupplyPriceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsSupplyPriceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsSupplyPriceResponse.Failure(refused);
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return RemoveOperationsSupplyPriceResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        try
        {
            supply.RemoveScheduledPrice(request.PriceId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RemoveOperationsSupplyPriceResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.supplies.price_removed", nameof(OperationsSupply), supply.Id, before: new { price = request.PriceId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsSupplyPriceResponse.Success()
            : RemoveOperationsSupplyPriceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
