using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoSales.cs
//
// PURPOSE:
// Development-only mock data for Slice 3b: promotion codes and
// offer links in every state, and discounts on a few demo
// subscriptions (D-S20, D-S21).
//
// RESPONSIBILITIES:
// - Create five promotions: active, waiting for approval, draft,
//   paused and ended
// - Create four offer links: draft, waiting for approval, issued
//   and revoked
// - Give one demo subscription a staff discount and another a
//   discount waiting for the sales manager
//
// IMPORTANT NOTES:
// - Uses the domain the way the use cases do, so every rule
//   holds; history rows are written, outbox messages are not
//   (no emails from demo data)
// - Sales limits are left empty on purpose: until the sales
//   manager sets them, everything needs approval
// - Four eyes: approved items need a second staff member; until
//   one exists only drafts and waiting requests are added, and the
//   approved ones follow at a later start
// - The issued demo offer's link is not known; use "Get a new
//   link" on its page
// - Safe to run again: each part is added once, keyed on the
//   promotion codes LOYAL10 and WELCOME1M
// =========================================================
public sealed class PlatformDemoSales
{
    private const string Undecided = "LOYAL10";

    private const string Decided = "WELCOME1M";

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformPromotionRepository _promotions;

    private readonly IPlatformOfferRepository _offers;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformTenantQueries _tenants;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _time;

    public PlatformDemoSales(
        IPlatformPlanRepository plans,
        IPlatformPromotionRepository promotions,
        IPlatformOfferRepository offers,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformTenantQueries tenants,
        IPlatformSecretGenerator secrets,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _promotions = promotions ?? throw new ArgumentNullException(nameof(promotions));
        _offers = offers ?? throw new ArgumentNullException(nameof(offers));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _time = time ?? throw new ArgumentNullException(nameof(time));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformStaffOptionItem> staff = await _tenants.ListActiveStaffAsync(cancellationToken);
        PlatformPlan? basic = await _plans.GetByCodeAsync("BASIC", cancellationToken);
        PlatformPlan? growth = await _plans.GetByCodeAsync("GROWTH", cancellationToken);

        if (staff.Count == 0 || basic is null || growth is null)
        {
            return "Demo promotions skipped: seed the demo plans first.";
        }

        Guid seller = staff[0].PlatformUserId;
        DateTimeOffset now = _time.GetUtcNow();
        List<string> added = new();

        if (!await _promotions.CodeExistsAsync(Undecided, cancellationToken))
        {
            AddUndecided(growth, seller, now);
            await SaveAsync("undecided promotions and offers", cancellationToken);
            int discounts = await AddDiscountsAsync(seller, now, cancellationToken);
            added.Add($"2 draft or waiting promotions, 2 draft or waiting offer links and {discounts} discount(s)");
        }

        if (!await _promotions.CodeExistsAsync(Decided, cancellationToken))
        {
            if (staff.Count < 2)
            {
                added.Add("approved promotions and offers wait for a second staff member (four eyes) and are added at a later start");
            }
            else
            {
                AddDecided(basic, growth, seller, staff[1].PlatformUserId, now);
                await SaveAsync("decided promotions and offers", cancellationToken);
                added.Add("3 approved promotions (active, paused, ended) and 2 decided offer links (issued, revoked)");
            }
        }

        return added.Count == 0 ? "Demo promotions and offers already exist." : "Demo sales: " + string.Join("; ", added) + ".";
    }

    // Drafts and requests waiting for a decision: no second person needed.
    private void AddUndecided(PlatformPlan growth, Guid seller, DateTimeOffset now)
    {
        Promotion(Undecided, "Loyal customer: 10% with no end", Terms(PlatformDiscountKind.Percent, 10m, PlatformDiscountDuration.NoEnd, null, now), now, null, Array.Empty<Guid>(), null, false, true, null, seller, now.AddDays(-1));

        PlatformPromotion spring = Promotion("SPRING20", "Spring: 20% off for 3 months", Terms(PlatformDiscountKind.Percent, 20m, PlatformDiscountDuration.Periods, 3, now), now.AddDays(5), now.AddDays(95), new[] { growth.Id }, PlatformSubscriptionTerm.Monthly, true, false, 200, seller, now.AddDays(-2));
        spring.Submit(seller, now.AddDays(-1));

        Offer("Northwind Freight", "procurement@northwind.example", "Pilot with three depots; agreed during the April visit.", growth, PlatformSubscriptionTerm.Annual, 25, Terms(PlatformDiscountKind.Percent, 15m, PlatformDiscountDuration.Periods, 2, now), 0m, Array.Empty<(string, decimal)>(), now.AddDays(20), seller, now.AddDays(-1));

        PlatformOffer harbour = Offer("Harbour Logistics", null, "Large fleet; matches a competitor's price for the first year.", growth, PlatformSubscriptionTerm.Annual, 60, Terms(PlatformDiscountKind.Percent, 30m, PlatformDiscountDuration.Periods, 1, now), null, new[] { ("On-site training day", 900m) }, now.AddDays(25), seller, now.AddDays(-2));
        harbour.Submit(true, null, now.AddDays(-1));
    }

    // Approved items: four eyes, so a second staff member decides.
    private void AddDecided(PlatformPlan basic, PlatformPlan growth, Guid seller, Guid approver, DateTimeOffset now)
    {
        PlatformPromotion welcome = Promotion(Decided, "First month free", Terms(PlatformDiscountKind.Percent, 100m, PlatformDiscountDuration.Periods, 1, now), now.AddDays(-60), null, Array.Empty<Guid>(), PlatformSubscriptionTerm.Monthly, true, true, null, seller, now.AddDays(-61));
        welcome.SetName(LanguageCode.Create("fr"), "Premier mois offert", now.AddDays(-61));
        welcome.SetName(LanguageCode.Create("es"), "Primer mes gratis", now.AddDays(-61));
        welcome.Submit(seller, now.AddDays(-61));
        welcome.Approve(approver, null, now.AddDays(-60));

        PlatformPromotion partner = Promotion("PARTNER50", "Partner: 50 off the first year", Terms(PlatformDiscountKind.FixedAmount, 50m, PlatformDiscountDuration.Periods, 1, now), now.AddDays(-30), null, new[] { basic.Id, growth.Id }, PlatformSubscriptionTerm.Annual, true, true, 50, seller, now.AddDays(-31));
        partner.Submit(seller, now.AddDays(-31));
        partner.Approve(approver, null, now.AddDays(-30));
        partner.Pause(now.AddDays(-3));

        PlatformPromotion summer = Promotion("SUMMER15", "Summer: 15% for 2 months", Terms(PlatformDiscountKind.Percent, 15m, PlatformDiscountDuration.Periods, 2, now), now.AddDays(-120), now.AddDays(-30), Array.Empty<Guid>(), null, true, false, null, seller, now.AddDays(-121));
        summer.Submit(seller, now.AddDays(-121));
        summer.Approve(approver, null, now.AddDays(-120));
        summer.End(now.AddDays(-30));

        PlatformOffer contoso = Offer("Contoso Cold Chain", "ops@contoso.example", "Long-standing V3 customer moving to V4.", basic, PlatformSubscriptionTerm.Monthly, 8, Terms(PlatformDiscountKind.FixedAmount, 20m, PlatformDiscountDuration.Periods, 6, now), 0m, Array.Empty<(string, decimal)>(), now.AddDays(10), seller, now.AddDays(-6));
        contoso.Submit(true, null, now.AddDays(-5));
        contoso.Approve(approver, "Agreed: keeps the V3 price for six months.", _secrets.NewToken().Hash, now.AddDays(-4));

        PlatformOffer fabrikam = Offer("Fabrikam Movers", null, "Quote for a seasonal contract.", basic, PlatformSubscriptionTerm.Monthly, 4, null, null, new[] { ("Data migration", 300m) }, now.AddDays(15), seller, now.AddDays(-9));
        fabrikam.Submit(true, null, now.AddDays(-8));
        fabrikam.Approve(approver, null, _secrets.NewToken().Hash, now.AddDays(-7));
        fabrikam.Revoke(seller, now.AddDays(-2));
    }

    private async Task<int> AddDiscountsAsync(Guid seller, DateTimeOffset now, CancellationToken cancellationToken)
    {
        PagedResult<PlatformTenantListItem> tenants = await _tenants.ListAsync(
            new PlatformTenantListFilter(null, null, PlatformDemoTenants.Tag, null),
            PagedRequest.Create(1, PagedRequest.MaximumPageSize),
            cancellationToken);
        int added = 0;

        foreach (PlatformTenantListItem tenant in tenants.Items.OrderBy(item => item.TenantCode, StringComparer.Ordinal))
        {
            if (added == 2)
            {
                break;
            }

            PlatformSubscription? subscription = await _subscriptions.GetCurrentAsync(tenant.PlatformTenantId, cancellationToken);

            if (subscription is not { Status: PlatformSubscriptionStatus.Active } || subscription.ActiveDiscount is not null || subscription.PendingDiscount is not null)
            {
                continue;
            }

            try
            {
                PlatformSubscriptionEvent recorded = added == 0
                    ? subscription.RequestDiscount(Terms(PlatformDiscountKind.Percent, 15m, PlatformDiscountDuration.Periods, 3, now), PlatformDiscountSource.Staff, null, "Goodwill after the onboarding delay.", false, seller, now)
                    : subscription.RequestDiscount(Terms(PlatformDiscountKind.Percent, 35m, PlatformDiscountDuration.NoEnd, null, now), PlatformDiscountSource.Staff, null, "Strategic account; asks for a lasting price.", true, seller, now);
                _subscriptions.AddEvents(new[] { recorded });
                await SaveAsync(tenant.DisplayName, cancellationToken);
                added++;
            }
            catch (DomainException)
            {
                _unitOfWork.DiscardChanges();
            }
        }

        return added;
    }

    private PlatformPromotion Promotion(
        string code,
        string name,
        PlatformDiscountTerms terms,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        IReadOnlyCollection<Guid> planIds,
        PlatformSubscriptionTerm? term,
        bool atRegistration,
        bool byStaff,
        int? redemptionLimit,
        Guid actor,
        DateTimeOffset at)
    {
        PlatformPromotion promotion = PlatformPromotion.Create(code, name, terms, validFromUtc, validToUtc, planIds, term, atRegistration, byStaff, redemptionLimit, actor, at);
        _promotions.Add(promotion);

        return promotion;
    }

    private PlatformOffer Offer(
        string company,
        string? email,
        string reason,
        PlatformPlan plan,
        PlatformSubscriptionTerm term,
        int licences,
        PlatformDiscountTerms? discount,
        decimal? setupFee,
        IReadOnlyList<(string Description, decimal Amount)> charges,
        DateTimeOffset expiresUtc,
        Guid actor,
        DateTimeOffset at)
    {
        PlatformOffer offer = PlatformOffer.Prepare(company, email, reason, plan.Id, term, licences, discount, setupFee, charges, false, expiresUtc, 1, actor, at);
        _offers.Add(offer);

        return offer;
    }

    private static PlatformDiscountTerms Terms(PlatformDiscountKind kind, decimal value, PlatformDiscountDuration duration, int? periods, DateTimeOffset now)
    {
        return PlatformDiscountTerms.Create(kind, value, duration, periods, null, now);
    }

    private async Task SaveAsync(string name, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException("Demo sales data could not be saved: " + name + ".");
        }
    }
}
