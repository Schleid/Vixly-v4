using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsCarrier;

// =========================================================
// FILE: SaveOperationsCarrierUseCase.cs
//
// PURPOSE:
// Add a carrier or change one (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.carriers.manage (the company's carriers and their rates, D-8)
// =========================================================
public sealed class SaveOperationsCarrierUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsCarrierUseCase(
        OperationsAccess access,
        IOperationsDispatchRepository dispatches,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsCarrierResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsCarrierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CarriersManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsCarrierResponse.Failure(refused);
        }

        OperationsCarrier? carrier = null;

        if (request.CarrierId is { } id && (carrier = await _dispatches.GetCarrierAsync(id, cancellationToken)) is null)
        {
            return SaveOperationsCarrierResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        // Its kind sets how its rates are counted: fixed once it has rates.
        if (carrier is not null && carrier.Kind != request.Kind && (await _dispatches.ListRatesAsync(carrier.Id, cancellationToken)).Count > 0)
        {
            return SaveOperationsCarrierResponse.Failure(OperationsDispatchErrors.CarrierInvalid);
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && await _dispatches.CarrierNameTakenAsync(request.Name.Trim(), carrier?.Id, cancellationToken))
        {
            return SaveOperationsCarrierResponse.Failure(OperationsDispatchErrors.CarrierTaken);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        object? before = carrier is null ? null : new { name = carrier.Name, kind = carrier.Kind.ToString(), account = carrier.AccountNumber, prefix = carrier.AirlinePrefix, active = carrier.IsActive };

        try
        {
            if (carrier is null)
            {
                carrier = OperationsCarrier.Create(context.RequiredTenantId, request.Name, request.Kind, request.AccountNumber, request.AirlinePrefix, utcNow);
                _dispatches.Add(carrier);
            }
            else
            {
                carrier.Change(request.Name, request.Kind, request.AccountNumber, request.AirlinePrefix, request.Active, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsCarrierResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, before is null ? "ops.carriers.created" : "ops.carriers.changed", nameof(OperationsCarrier), carrier.Id, before: before,
            after: new { name = carrier.Name, kind = carrier.Kind.ToString(), account = carrier.AccountNumber, prefix = carrier.AirlinePrefix, active = carrier.IsActive });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsCarrierResponse.Success(carrier.Id)
            : SaveOperationsCarrierResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
