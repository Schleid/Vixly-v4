using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ReportTenantShipmentUsage;

// =========================================================
// FILE: ReportTenantShipmentUsageUseCase.cs
//
// PURPOSE:
// Records a company's shipments of one month and, once the month is closed, charges the shipments above the plan's cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The company is always the one in the token (S-13); a charged month never changes again
// =========================================================
public sealed class ReportTenantShipmentUsageUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformShipmentUsageRepository _usage;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ReportTenantShipmentUsageUseCase(PlatformSubscriptionAdministration administration, IPlatformShipmentUsageRepository usage, IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _usage = usage ?? throw new ArgumentNullException(nameof(usage));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReportTenantShipmentUsageResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ReportTenantShipmentUsageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _administration.UtcNow;
        string month;

        try
        {
            month = PlatformTenantShipmentUsage.MonthOf(request.Month);
        }
        catch (DomainException exception)
        {
            return ReportTenantShipmentUsageResponse.Failure(exception.Error);
        }

        // A month is closed only once it has ended everywhere (the company's time zone may be up to 14 hours ahead of UTC).
        DateTimeOffset monthEnd = new DateTimeOffset(DateTime.ParseExact(month + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), TimeSpan.Zero).AddMonths(1);
        bool isFinal = request.IsFinal && utcNow >= monthEnd.AddHours(-14);
        PlatformTenantShipmentUsage? usage = await _usage.GetAsync(context.PlatformTenantId, month, cancellationToken);

        try
        {
            if (usage is null)
            {
                usage = PlatformTenantShipmentUsage.Start(context.PlatformTenantId, month, utcNow);
                _usage.Add(usage);
            }

            usage.Report(request.Shipments, isFinal, utcNow);

            if (usage.IsFinal && !usage.IsCharged)
            {
                await ChargeAsync(context.PlatformTenantId, usage, utcNow, cancellationToken);
            }
        }
        catch (DomainException exception)
        {
            return ReportTenantShipmentUsageResponse.Failure(exception.Error);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ReportTenantShipmentUsageResponse.Success()
            : ReportTenantShipmentUsageResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }

    // The cap in force when the month closes (an override counts); a company that upgraded before then is charged by its new plan.
    private async Task ChargeAsync(Guid platformTenantId, PlatformTenantShipmentUsage usage, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(platformTenantId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? version = plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);
        IReadOnlyList<PlatformLimitItem> limits = await _administration.LimitsAsync(platformTenantId, version, null, cancellationToken);
        int cap = limits.FirstOrDefault(item => item.Kind == PlatformPlanLimitKind.MaxShipmentsPerMonth)?.Effective ?? 0;
        int extra = cap > 0 ? Math.Max(0, usage.Shipments - cap) : 0;

        if (subscription is null || plan is null || version is null || extra == 0 || version.ExtraShipmentPrice <= 0m)
        {
            usage.MarkSettled(cap, utcNow);
            return;
        }

        PlatformSubscriptionEvent change = subscription.AddShipmentCharge(usage.Month, extra, version.ExtraShipmentPrice, plan.CurrencyCode, utcNow);
        _administration.RecordChange(null, subscription, plan, change, null, "platform.subscriptions.shipments_charged");
        usage.MarkCharged(cap, extra, decimal.Parse(change.Arguments["amount"], System.Globalization.CultureInfo.InvariantCulture), plan.CurrencyCode, utcNow);
    }
}
