using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Plans;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Plans.PublishPlatformPlanVersion;

// =========================================================
// FILE: PublishPlatformPlanVersionUseCase.cs
//
// PURPOSE:
// Approves and publishes a submitted plan version: the CFO step (D-S2, D-S13, D-S26).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up: prices reach every new subscriber
// - Only a submitted version, and never by the person who submitted it (four eyes)
// - Existing subscribers keep their version for the rest of their period (owner, D-S2); a price rise is announced to them (D-S13)
// =========================================================
public sealed class PublishPlatformPlanVersionUseCase
{
    private readonly PlatformPlanAdministration _administration;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PublishPlatformPlanVersionUseCase(
        PlatformPlanAdministration administration,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PublishPlatformPlanVersionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PublishPlatformPlanVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPlanTarget target = await _administration.LoadPlanAsync(context, PlatformPermissions.PlansPublish, request.PlatformPlanId, requireStepUp: true, cancellationToken);

        if (target.Plan is not PlatformPlan plan)
        {
            return PublishPlatformPlanVersionResponse.Failure(target.Error!);
        }

        PlatformPlanVersion? previous;
        PlatformPlanVersion published;

        try
        {
            previous = plan.Current;
            published = plan.ApproveDraft(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return PublishPlatformPlanVersionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, plan, PlatformPermissions.PlansPublish, "platform.plans.version_published", before: new { version = previous?.Number }, after: new { version = published.Number });

        _outbox.Write(
            PlatformMessageTypes.PlanVersionPublished,
            plan.Id,
            new PlatformPlanVersionPublished(plan.Id, plan.Code, published.Id, published.Number, previous?.Id, _administration.UtcNow));

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PublishPlatformPlanVersionResponse.Success()
            : PublishPlatformPlanVersionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
