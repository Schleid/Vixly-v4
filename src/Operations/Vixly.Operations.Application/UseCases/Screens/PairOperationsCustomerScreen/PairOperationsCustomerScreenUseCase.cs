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

namespace Vixly.Operations.Application.UseCases.Screens.PairOperationsCustomerScreen;

// =========================================================
// FILE: PairOperationsCustomerScreenUseCase.cs
//
// PURPOSE:
// Adds a customer screen to a warehouse, or gives an existing one a new code, and returns the code to type on the screen (Slice 5F step 3, P-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve or ops.payments.take at that warehouse
// - The code is shown once to the clerk, typed on the screen within 15 minutes; only its hash is kept
// - A new code stops the screen paired before at once
// =========================================================
public sealed class PairOperationsCustomerScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PairOperationsCustomerScreenUseCase(
        OperationsScreenDesk desk,
        IOperationsSecretGenerator secrets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PairOperationsCustomerScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PairOperationsCustomerScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return PairOperationsCustomerScreenResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSecret code = _secrets.NewRecoveryCode();
        OperationsCustomerScreen screen;

        if (request.ScreenId is { } screenId)
        {
            if (await _desk.Screens.GetAsync(screenId, cancellationToken) is not { } existing || warehouses.All(warehouse => warehouse.Id != existing.WarehouseId))
            {
                return PairOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
            }

            screen = existing;
            screen.NewCode(code.Hash, access!.User.Id, utcNow);
        }
        else
        {
            if (warehouses.All(warehouse => warehouse.Id != request.WarehouseId))
            {
                return PairOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
            }

            try
            {
                screen = OperationsCustomerScreen.Add(context.RequiredTenantId, request.WarehouseId, request.Name, code.Hash, access!.User.Id, utcNow);
            }
            catch (DomainException exception)
            {
                return PairOperationsCustomerScreenResponse.Failure(exception.Error);
            }

            _desk.Screens.Add(screen);
        }

        _audit.Record(access.User.Id, "ops.screens.code_given", nameof(OperationsCustomerScreen), screen.Id, after: new { screen.Name, warehouse = screen.WarehouseId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PairOperationsCustomerScreenResponse.Success(screen.Id, code.Value, screen.PairingExpiresUtc!.Value)
            : PairOperationsCustomerScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
