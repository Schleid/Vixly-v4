using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.StartOperationsInspection;

// =========================================================
// FILE: StartOperationsInspectionUseCase.cs
//
// PURPOSE:
// The inspector opens the box: the inspection is in progress (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.inspection.perform for the parcel's warehouse
// =========================================================
public sealed class StartOperationsInspectionUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsInspectionSteps _steps;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StartOperationsInspectionUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        OperationsInspectionSteps steps,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StartOperationsInspectionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsInspectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return StartOperationsInspectionResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _inspections.GetInspectionAsync(request.InspectionId, cancellationToken) is not { } inspection
            || await _scope.CoveredAsync(access, OperationsPermissions.InspectionPerform, inspection.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return StartOperationsInspectionResponse.Failure(OperationsInspectionErrors.NotFound);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return StartOperationsInspectionResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        // The same inspector pressing twice is one start; another inspector who started it first keeps it.
        if (inspection.Status == OperationsInspectionStatus.InProgress && inspection.StartedByUserId == access.User.Id)
        {
            return StartOperationsInspectionResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            inspection.Start(access.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return StartOperationsInspectionResponse.Failure(exception.Error);
        }

        if (await _steps.RecordAsync(inspection.ParcelId, access, warehouse, utcNow, cancellationToken,
            ("ins:" + request.CommandKey + ":start", OperationsTrackingCodes.InspectionStarted, null, null, null)) is { } refused)
        {
            return StartOperationsInspectionResponse.Failure(refused);
        }

        // Two inspectors starting the same parcel at once: the row version keeps the first.
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return StartOperationsInspectionResponse.Success();
        }

        // The same click twice: the other one started it for this inspector (ADR-0019).
        _unitOfWork.DiscardChanges();
        return await _inspections.GetInspectionAsync(request.InspectionId, cancellationToken) is { Status: OperationsInspectionStatus.InProgress } raced && raced.StartedByUserId == access.User.Id
            ? StartOperationsInspectionResponse.Success()
            : StartOperationsInspectionResponse.Failure(OperationsInspectionErrors.NotWaiting);
    }
}
