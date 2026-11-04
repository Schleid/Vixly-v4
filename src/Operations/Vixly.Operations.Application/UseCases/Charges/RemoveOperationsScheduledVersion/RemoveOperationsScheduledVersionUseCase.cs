using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.RemoveOperationsScheduledVersion;

// =========================================================
// FILE: RemoveOperationsScheduledVersionUseCase.cs
//
// PURPOSE:
// Removes a version that has not started yet: a price list, an extra or the insurance plan (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RemoveOperationsScheduledVersionUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsScheduledVersionUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsScheduledVersionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsScheduledVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsScheduledVersionResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            switch (request.Owner)
            {
                case OperationsVersionOwner.PriceList:
                    OperationsRateCard card = await _charges.GetCardAsync(request.OwnerId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.CardNotFound);

                    // Removed already (a second click): nothing more to do.
                    if (card.Versions.All(version => version.Id != request.VersionId))
                    {
                        return RemoveOperationsScheduledVersionResponse.Success();
                    }

                    card.RemoveScheduled(request.VersionId, utcNow);
                    break;
                case OperationsVersionOwner.Extra:
                    OperationsChargeExtra extra = await _charges.GetExtraAsync(request.OwnerId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.ExtraNotFound);

                    if (extra.Versions.All(version => version.Id != request.VersionId))
                    {
                        return RemoveOperationsScheduledVersionResponse.Success();
                    }

                    extra.RemoveScheduled(request.VersionId, utcNow);
                    break;
                case OperationsVersionOwner.Insurance:
                    OperationsInsurancePlan plan = await _charges.GetInsurancePlanAsync(request.OwnerId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.VersionNotFound);

                    if (plan.Versions.All(version => version.Id != request.VersionId))
                    {
                        return RemoveOperationsScheduledVersionResponse.Success();
                    }

                    plan.RemoveScheduled(request.VersionId, utcNow);
                    break;
                default:
                    return RemoveOperationsScheduledVersionResponse.Failure(OperationsChargeErrors.ChoiceInvalid);
            }
        }
        catch (DomainException exception)
        {
            return RemoveOperationsScheduledVersionResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.charges.version_removed", request.Owner.ToString(), request.OwnerId, after: new { version = request.VersionId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsScheduledVersionResponse.Success()
            : RemoveOperationsScheduledVersionResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
