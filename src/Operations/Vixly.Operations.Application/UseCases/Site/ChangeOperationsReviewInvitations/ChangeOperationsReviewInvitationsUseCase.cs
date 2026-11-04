using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsReviewInvitations;

// =========================================================
// FILE: ChangeOperationsReviewInvitationsUseCase.cs
//
// PURPOSE:
// Turns the portal's request for customer reviews on or off (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Those who edit the website decide
// - Only on plans with the customer portal; turning it off keeps the reviews already sent
// =========================================================
public sealed class ChangeOperationsReviewInvitationsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsSettingsRepository _settings;

    private readonly Vixly.Operations.Application.Ports.IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsReviewInvitationsUseCase(
        OperationsOwnerCheck owner,
        IOperationsSettingsRepository settings,
        Vixly.Operations.Application.Ports.IOperationsPlanFeatures features,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsReviewInvitationsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsReviewInvitationsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return ChangeOperationsReviewInvitationsResponse.Failure(refused);
        }

        if (request.Ask && !(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal))
        {
            return ChangeOperationsReviewInvitationsResponse.Failure(OperationsCommonErrors.NotInPlan);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);
        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = saved ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);

        if (settings.AskForReviews == request.Ask)
        {
            return ChangeOperationsReviewInvitationsResponse.Success();
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        settings.ChangeAskForReviews(request.Ask, utcNow);
        _audit.Record(context.UserId, request.Ask ? "ops.site.reviews_asked" : "ops.site.reviews_not_asked", "OperationsTenantSettings", settings.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsReviewInvitationsResponse.Success()
            : ChangeOperationsReviewInvitationsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
