using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoSubscriptions.cs
//
// PURPOSE:
// Development-only mock data: demo plans and a subscription for
// the demo tenants, in every status (D-S16).
//
// RESPONSIBILITIES:
// - Create the plans Basic, Growth and Enterprise (published,
//   public, translated), a retired Legacy plan and a draft on
//   Growth
// - Give demo tenants subscriptions in a realistic mix: trial,
//   pending payment, active, cancelling, downgrade scheduled,
//   suspended with the tenant, ended with the tenant
// - Add one limit override with a reason
//
// IMPORTANT NOTES:
// - Uses the domain the way the use cases do, so every rule
//   holds; history rows are written, outbox messages are not
//   (no emails from demo data)
// - Safe to run again: plans are created once; later runs only
//   give a subscription to demo tenants that have none (for a
//   database seeded before Slice 3)
// - Runs after PlatformDemoTenants, from the command or at
//   Development start-up (PlatformDemoDataWorker)
// =========================================================
public sealed class PlatformDemoSubscriptions
{
    private const string Seeded = "BASIC";

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformLimitOverrideRepository _overrides;

    private readonly IPlatformTenantQueries _tenants;

    private readonly IPlatformTenantRepository _tenantRecords;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _time;

    public PlatformDemoSubscriptions(
        IPlatformPlanRepository plans,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformLimitOverrideRepository overrides,
        IPlatformTenantQueries tenants,
        IPlatformTenantRepository tenantRecords,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _tenantRecords = tenantRecords ?? throw new ArgumentNullException(nameof(tenantRecords));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformStaffOptionItem> staff = await _tenants.ListActiveStaffAsync(cancellationToken);

        if (staff.Count == 0)
        {
            return "Create and sign in the first SuperUser before seeding demo plans.";
        }

        Guid actor = staff[0].PlatformUserId;
        DateTimeOffset now = _time.GetUtcNow();
        DemoPlans? plans = await LoadPlansAsync(cancellationToken);
        bool created = plans is null;

        if (plans is null)
        {
            plans = CreatePlans(actor, now.AddDays(-420));
            await SaveAsync("plans", cancellationToken);
        }

        PagedResult<PlatformTenantListItem> tenants = await _tenants.ListAsync(
            new PlatformTenantListFilter(null, null, PlatformDemoTenants.Tag, null),
            PagedRequest.Create(1, PagedRequest.MaximumPageSize),
            cancellationToken);

        int added = 0;
        int skipped = 0;
        int scenario = 0;

        foreach (PlatformTenantListItem tenant in tenants.Items.OrderBy(item => item.TenantCode, StringComparer.Ordinal))
        {
            if (tenant.Status is PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation)
            {
                continue;
            }

            int current = scenario++;

            // Ended subscriptions for closed tenants are added once, with the plans.
            if ((!created && tenant.Status is PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned)
                || await _subscriptions.GetCurrentAsync(tenant.PlatformTenantId, cancellationToken) is not null)
            {
                continue;
            }

            try
            {
                PlatformTenant? record = await _tenantRecords.GetAsync(tenant.PlatformTenantId, cancellationToken);
                Subscribe(tenant, record, plans, current, created, actor, now);
                await SaveAsync(tenant.DisplayName, cancellationToken);
                added++;
            }
            catch (DomainException)
            {
                _unitOfWork.DiscardChanges();
                skipped++;
            }
        }

        if (plans.Legacy.Status == PlatformPlanStatus.Active)
        {
            plans.Legacy.Retire(now.AddDays(-30));
            await SaveAsync("the retired plan", cancellationToken);
        }

        string start = created ? "Added 4 demo plans" : "Demo plans already exist";
        string end = skipped > 0 ? $"; {skipped} tenant(s) skipped because their data did not allow it" : string.Empty;

        return $"{start}; added {added} demo subscription(s){end}.";
    }

    private async Task<DemoPlans?> LoadPlansAsync(CancellationToken cancellationToken)
    {
        PlatformPlan? basic = await _plans.GetByCodeAsync(Seeded, cancellationToken);

        if (basic is null)
        {
            return null;
        }

        PlatformPlan growth = await _plans.GetByCodeAsync("GROWTH", cancellationToken) ?? basic;
        PlatformPlan enterprise = await _plans.GetByCodeAsync("ENTERPRISE", cancellationToken) ?? growth;
        PlatformPlan legacy = await _plans.GetByCodeAsync("LEGACY-EU", cancellationToken) ?? basic;

        return new DemoPlans(basic, growth, enterprise, legacy);
    }

    private DemoPlans CreatePlans(Guid actor, DateTimeOffset at)
    {
        PlatformPlan basic = Plan(
            "BASIC",
            ("Basic", "For small teams getting started.", "Essentiel", "Pour les petites équipes qui débutent.", "Básico", "Para equipos pequeños que empiezan."),
            PlatformPlanVersionTerms.Create("USD", 12m, 120m, 0m, 2, 14, 10, 1, new[] { PlatformPlanFeature.DataExport }),
            1,
            actor,
            at);
        PlatformPlan growth = Plan(
            "GROWTH",
            ("Growth", "For growing operations with several sites.", "Croissance", "Pour les activités en croissance sur plusieurs sites.", "Crecimiento", "Para operaciones en crecimiento con varias sedes."),
            PlatformPlanVersionTerms.Create(
                "USD",
                25m,
                250m,
                99m,
                2,
                30,
                50,
                5,
                new[] { PlatformPlanFeature.DataExport, PlatformPlanFeature.AdvancedAnalytics, PlatformPlanFeature.PortalThemeCustomization, PlatformPlanFeature.HomepageContentCustomization }),
            2,
            actor,
            at);
        PlatformPlan enterprise = Plan(
            "ENTERPRISE",
            ("Enterprise", "Every feature, white-label portal and custom domain.", "Entreprise", "Toutes les fonctionnalités, portail en marque blanche et domaine personnalisé.", "Empresa", "Todas las funciones, portal de marca blanca y dominio propio."),
            PlatformPlanVersionTerms.Create("USD", 40m, 400m, 499m, 10, null, 500, 50, Enum.GetValues<PlatformPlanFeature>()),
            3,
            actor,
            at);
        PlatformPlan legacy = Plan(
            "LEGACY-EU",
            ("Legacy Europe", "Former euro plan, kept for existing customers.", "Ancien Europe", "Ancien forfait en euros, conservé pour les clients existants.", "Antiguo Europa", "Antiguo plan en euros, conservado para los clientes existentes."),
            PlatformPlanVersionTerms.Create("EUR", 15m, 0m, 0m, 2, null, 20, 2, new[] { PlatformPlanFeature.DataExport }),
            9,
            actor,
            at);

        growth.StartDraft(at.AddDays(400));
        growth.EditDraft(
            PlatformPlanVersionTerms.Create(
                "USD",
                27m,
                270m,
                99m,
                2,
                30,
                60,
                6,
                new[] { PlatformPlanFeature.DataExport, PlatformPlanFeature.AdvancedAnalytics, PlatformPlanFeature.PortalThemeCustomization, PlatformPlanFeature.HomepageContentCustomization }),
            at.AddDays(400));

        return new DemoPlans(basic, growth, enterprise, legacy);
    }

    private PlatformPlan Plan(
        string code,
        (string En, string EnText, string Fr, string FrText, string Es, string EsText) names,
        PlatformPlanVersionTerms terms,
        int order,
        Guid actor,
        DateTimeOffset at)
    {
        PlatformPlan plan = PlatformPlan.Create(code, names.En, names.EnText, terms.CurrencyCode, actor, at);
        plan.SetTranslation(LanguageCode.Fr, names.Fr, names.FrText, at);
        plan.SetTranslation(LanguageCode.Es, names.Es, names.EsText, at);
        plan.EditDraft(terms, at);
        plan.PublishDraft(actor, at);
        plan.SetCatalogue(order < 9, order, at);
        _plans.Add(plan);

        return plan;
    }

    private void Subscribe(PlatformTenantListItem tenant, PlatformTenant? record, DemoPlans plans, int scenario, bool withOverride, Guid actor, DateTimeOffset now)
    {
        List<PlatformSubscriptionEvent> events = new();
        int kind = scenario % 8;
        PlatformPlan plan = kind switch
        {
            0 or 4 => plans.Basic,
            3 or 5 => plans.Enterprise,
            7 when plans.Legacy.CanBeChosen => plans.Legacy,
            _ => plans.Growth
        };
        PlatformSubscriptionTerm term = kind is 3 or 5 ? PlatformSubscriptionTerm.Annual : PlatformSubscriptionTerm.Monthly;
        int licences = Math.Clamp(2 + (scenario % 7), plan.Current!.MinimumLicences, plan.Current.Limit(PlatformPlanLimitKind.MaxLicensedDevices));
        bool trial = kind is 0 or 6 && tenant.Status == PlatformTenantStatus.Active;
        // A closed or suspended tenant subscribed while it was active: the
        // history must start before its status changed, never after.
        bool closedOrSuspended = tenant.Status is PlatformTenantStatus.Suspended or PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned;
        DateTimeOffset statusChanged = record?.StatusChangedUtc ?? now.AddDays(-1);
        DateTimeOffset? suspendedBeforeClosing = tenant.Status is PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned
            ? record?.Suspensions
                .Where(item => item.CreatedUtc < statusChanged && (item.ResolvedUtc is null || item.ResolvedUtc >= statusChanged))
                .Select(item => (DateTimeOffset?)item.CreatedUtc)
                .Min()
            : null;
        DateTimeOffset anchor = suspendedBeforeClosing ?? statusChanged;
        DateTimeOffset started = closedOrSuspended
            ? Later(record?.ActivatedUtc?.AddHours(1), anchor.AddDays(term == PlatformSubscriptionTerm.Annual ? -200 : -20))
            : now.AddDays(term == PlatformSubscriptionTerm.Annual ? -200 + scenario : -(3 + scenario % 20));
        int maxLicences = plan.Current.Limit(PlatformPlanLimitKind.MaxLicensedDevices);

        PlatformSubscriptionStart start = PlatformSubscription.Start(tenant.PlatformTenantId, plan, term, licences, maxLicences, trial, actor, started);
        PlatformSubscription subscription = start.Subscription;
        events.Add(start.Event);
        _subscriptions.Add(subscription);

        if (!trial && kind != 1)
        {
            events.Add(subscription.RecordOfflinePayment("Bank transfer received (demo).", actor, started.AddHours(4)));
        }

        switch (tenant.Status)
        {
            case PlatformTenantStatus.Suspended:
                events.AddRange(Optional(subscription.SuspendWithTenant(statusChanged)));
                break;
            case PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned:
                if (suspendedBeforeClosing is DateTimeOffset suspendedAt)
                {
                    events.AddRange(Optional(subscription.SuspendWithTenant(suspendedAt)));
                }

                events.AddRange(Optional(subscription.EndWithTenant(statusChanged)));
                break;
            default:
                events.AddRange(Scenario(subscription, kind, plans, licences, actor, now));
                break;
        }

        if (scenario == 2 && withOverride)
        {
            _overrides.Add(PlatformTenantLimitOverride.Set(
                tenant.PlatformTenantId,
                PlatformPlanLimitKind.MaxWarehouses,
                8,
                "Seasonal peak: three temporary warehouses agreed with sales (demo).",
                now.AddDays(60),
                actor,
                now.AddDays(-2)));
        }

        _subscriptions.AddEvents(events);
    }

    private static IEnumerable<PlatformSubscriptionEvent> Scenario(
        PlatformSubscription subscription,
        int kind,
        DemoPlans plans,
        int licences,
        Guid actor,
        DateTimeOffset now)
    {
        DateTimeOffset at = now.AddHours(-6);

        switch (kind)
        {
            case 4:
                yield return subscription.RequestCancellation("Customer is moving to an in-house system (demo).", actor, at);
                break;
            case 5:
                int growthMax = plans.Growth.Current!.Limit(PlatformPlanLimitKind.MaxLicensedDevices);
                PlatformSubscriptionQuote quote = subscription.Quote(
                    plans.Enterprise,
                    plans.Growth,
                    PlatformSubscriptionTerm.Annual,
                    Math.Min(licences, growthMax),
                    growthMax,
                    0,
                    at);
                yield return subscription.Apply(quote, plans.Enterprise.Code, actor, at);
                break;
            case 6:
                yield return subscription.ExtendTrial(subscription.TrialEndsUtc!.Value.AddDays(14), "Waiting for their IT sign-off (demo).", actor, at);
                break;
        }
    }

    private static DateTimeOffset Later(DateTimeOffset? first, DateTimeOffset second)
    {
        return first is DateTimeOffset value && value > second ? value : second;
    }

    private static IEnumerable<PlatformSubscriptionEvent> Optional(PlatformSubscriptionEvent? item)
    {
        return item is null ? Array.Empty<PlatformSubscriptionEvent>() : new[] { item };
    }

    private async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Could not save demo subscription data for {name}.");
        }
    }

    private sealed record DemoPlans(PlatformPlan Basic, PlatformPlan Growth, PlatformPlan Enterprise, PlatformPlan Legacy);
}
