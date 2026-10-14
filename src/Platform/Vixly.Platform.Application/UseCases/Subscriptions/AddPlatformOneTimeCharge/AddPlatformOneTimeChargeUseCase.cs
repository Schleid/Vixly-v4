using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.AddPlatformOneTimeCharge;

// =========================================================
// FILE: AddPlatformOneTimeChargeUseCase.cs
//
// PURPOSE:
// Adds a one-time charge, billed on the next invoice (D-S23).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - In the plan's currency; before tax
// =========================================================
public sealed class AddPlatformOneTimeChargeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public AddPlatformOneTimeChargeUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AddPlatformOneTimeChargeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AddPlatformOneTimeChargeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsCharges, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return AddPlatformOneTimeChargeResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.AddCharge(request.Description, request.Amount, plan.CurrencyCode, request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return AddPlatformOneTimeChargeResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.SubscriptionsCharges, "platform.subscriptions.charge_added");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AddPlatformOneTimeChargeResponse.Success()
            : AddPlatformOneTimeChargeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
