using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.StartPlatformSubscription;

// =========================================================
// FILE: StartPlatformSubscriptionUseCase.cs
//
// PURPOSE:
// Starts a subscription for a tenant without one (D-S15).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The tenant data still comes from registration (Slice 2 correction); this only adds the subscription
// - One current subscription per tenant: checked here, guaranteed by a filtered unique index (a lost race reports already_subscribed)
// =========================================================
public sealed class StartPlatformSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public StartPlatformSubscriptionUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformTenantRepository tenants,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<StartPlatformSubscriptionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        StartPlatformSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsManage, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return StartPlatformSubscriptionResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null)
        {
            return StartPlatformSubscriptionResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (!tenant.CanStartSubscription)
        {
            return StartPlatformSubscriptionResponse.Failure(PlatformSubscriptionErrors.TenantCannotSubscribe);
        }

        if (await _administration.Subscriptions.GetCurrentAsync(tenant.Id, cancellationToken) is not null)
        {
            return StartPlatformSubscriptionResponse.Failure(PlatformSubscriptionErrors.AlreadySubscribed);
        }

        PlatformPlan? plan = await _administration.Plans.GetAsync(request.PlatformPlanId, cancellationToken);

        if (plan is null || !plan.CanBeChosen)
        {
            return StartPlatformSubscriptionResponse.Failure(PlatformSubscriptionErrors.PlanNotAvailable);
        }

        int maxLicences = await _administration.MaxLicencesAsync(tenant.Id, plan.Current!, cancellationToken);
        PlatformSubscriptionStart start;

        try
        {
            start = PlatformSubscription.Start(tenant.Id, plan, request.Term, request.Licences, maxLicences, request.StartTrial, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return StartPlatformSubscriptionResponse.Failure(exception.Error);
        }

        _administration.Subscriptions.Add(start.Subscription);
        _administration.RecordChange(context, start.Subscription, plan, start.Event, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.started");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? StartPlatformSubscriptionResponse.Success(start.Subscription.Id)
            : StartPlatformSubscriptionResponse.Failure(PlatformSubscriptionErrors.AlreadySubscribed);
    }
}
