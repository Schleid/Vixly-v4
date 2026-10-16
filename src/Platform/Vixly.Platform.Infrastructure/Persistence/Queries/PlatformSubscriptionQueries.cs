using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformSubscriptionQueries.cs
//
// PURPOSE:
// Read-only projections of plans and subscriptions.
//
// RESPONSIBILITIES:
// - Page plans with their current prices and subscriber counts
// - Page subscriptions with tenant and plan names
// - Page a tenant subscription history
// - Name staff
//
// IMPORTANT NOTES:
// - No tracking; nothing here changes data
// - Names are read in the reader language with an English fallback
// =========================================================
internal sealed class PlatformSubscriptionQueries : IPlatformSubscriptionQueries
{
    private readonly PlatformDbContext _context;

    public PlatformSubscriptionQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformPlanListItem>> ListPlansAsync(
        PlatformPlanListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        LanguageCode language = LanguageCode.TryCreate(filter.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;
        IQueryable<PlatformPlan> plans = _context.Plans.AsNoTracking();

        if (filter.Status is PlatformPlanStatus status)
        {
            plans = plans.Where(plan => plan.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            plans = plans.Where(plan =>
                EF.Functions.Like(plan.Code, pattern, "\\")
                || plan.Translations.Any(item => EF.Functions.Like(item.Name, pattern, "\\")));
        }

        int total = await plans.CountAsync(cancellationToken);
        var rows = await plans
            .OrderBy(plan => plan.DisplayOrder)
            .ThenBy(plan => plan.Code)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(plan => new
            {
                plan.Id,
                plan.Code,
                Name = plan.Translations.Where(item => item.Language == language).Select(item => item.Name).FirstOrDefault()
                    ?? plan.Translations.Where(item => item.Language == LanguageCode.Default).Select(item => item.Name).FirstOrDefault(),
                plan.Status,
                plan.IsPublic,
                plan.DisplayOrder,
                Current = plan.Versions
                    .Where(version => version.Status == PlatformPlanVersionStatus.Published)
                    .Select(version => new { version.Number, version.CurrencyCode, version.MonthlyPricePerLicence, version.AnnualPricePerLicence })
                    .FirstOrDefault(),
                DraftCurrency = plan.Versions.Where(version => version.Status == PlatformPlanVersionStatus.Draft).Select(version => version.CurrencyCode).FirstOrDefault(),
                HasDraft = plan.Versions.Any(version => version.Status == PlatformPlanVersionStatus.Draft),
                Subscribers = _context.Subscriptions.Count(subscription => subscription.PlatformPlanId == plan.Id && subscription.Status != PlatformSubscriptionStatus.Ended)
            })
            .ToListAsync(cancellationToken);

        List<PlatformPlanListItem> items = rows
            .Select(row => new PlatformPlanListItem(
                row.Id,
                row.Code,
                row.Name ?? row.Code,
                row.Status,
                row.IsPublic,
                row.DisplayOrder,
                row.Current?.Number,
                row.HasDraft,
                row.Current?.CurrencyCode ?? row.DraftCurrency ?? string.Empty,
                row.Current?.MonthlyPricePerLicence,
                row.Current?.AnnualPricePerLicence,
                row.Subscribers))
            .ToList();

        return new PagedResult<PlatformPlanListItem>(items, page, total);
    }

    public async Task<PagedResult<PlatformSubscriptionListItem>> ListSubscriptionsAsync(
        PlatformSubscriptionListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        LanguageCode language = LanguageCode.TryCreate(filter.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;
        IQueryable<PlatformSubscription> subscriptions = _context.Subscriptions.AsNoTracking();

        if (filter.Status is PlatformSubscriptionStatus status)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Status == status);
        }
        else if (!filter.IncludeEnded)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Status != PlatformSubscriptionStatus.Ended);
        }

        if (filter.DiscountedOnly)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Discounts.Any(discount => discount.Status == PlatformDiscountStatus.Active));
        }

        if (filter.PlatformPlanId is Guid planId)
        {
            subscriptions = subscriptions.Where(subscription => subscription.PlatformPlanId == planId);
        }

        if (filter.RenewsBeforeUtc is DateTimeOffset before)
        {
            subscriptions = subscriptions.Where(subscription =>
                (subscription.PeriodEndUtc != null && subscription.PeriodEndUtc <= before)
                || (subscription.TrialEndsUtc != null && subscription.Status == PlatformSubscriptionStatus.Trialing && subscription.TrialEndsUtc <= before));
        }

        var joined =
            from subscription in subscriptions
            join tenant in _context.Tenants.AsNoTracking() on subscription.PlatformTenantId equals tenant.Id
            join plan in _context.Plans.AsNoTracking() on subscription.PlatformPlanId equals plan.Id
            join version in _context.Set<PlatformPlanVersion>().AsNoTracking() on subscription.PlanVersionId equals version.Id
            select new { subscription, tenant, plan, version };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            joined = joined.Where(row =>
                EF.Functions.Like(row.tenant.Company.LegalName, pattern, "\\")
                || EF.Functions.Like(row.tenant.Company.TradingName!, pattern, "\\")
                || EF.Functions.Like(row.tenant.TenantCode, pattern, "\\")
                || EF.Functions.Like(row.plan.Code, pattern, "\\"));
        }

        int total = await joined.CountAsync(cancellationToken);
        var rows = await joined
            .OrderBy(row => row.subscription.PeriodEndUtc ?? row.subscription.TrialEndsUtc)
            .ThenBy(row => row.subscription.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(row => new
            {
                row.subscription.Id,
                row.subscription.PlatformTenantId,
                row.tenant.TenantCode,
                TenantName = row.tenant.Company.TradingName ?? row.tenant.Company.LegalName,
                PlanCode = row.plan.Code,
                PlanName = row.plan.Translations.Where(item => item.Language == language).Select(item => item.Name).FirstOrDefault()
                    ?? row.plan.Translations.Where(item => item.Language == LanguageCode.Default).Select(item => item.Name).FirstOrDefault(),
                row.version.Number,
                row.subscription.Status,
                row.subscription.Term,
                row.subscription.Licences,
                Unit = row.subscription.Term == PlatformSubscriptionTerm.Annual ? row.version.AnnualPricePerLicence : row.version.MonthlyPricePerLicence,
                row.version.CurrencyCode,
                row.subscription.PeriodEndUtc,
                row.subscription.TrialEndsUtc,
                row.subscription.CancelAtPeriodEnd,
                HasScheduledChange = row.subscription.ScheduledChange != null,
                HasDiscount = row.subscription.Discounts.Any(discount => discount.Status == PlatformDiscountStatus.Active)
            })
            .ToListAsync(cancellationToken);

        List<PlatformSubscriptionListItem> items = rows
            .Select(row => new PlatformSubscriptionListItem(
                row.Id,
                row.PlatformTenantId,
                row.TenantCode,
                row.TenantName,
                row.PlanCode,
                row.PlanName ?? row.PlanCode,
                row.Number,
                row.Status,
                row.Term,
                row.Licences,
                PlatformPricing.Money(row.Unit * row.Licences),
                row.CurrencyCode,
                row.PeriodEndUtc,
                row.TrialEndsUtc,
                row.CancelAtPeriodEnd,
                row.HasScheduledChange,
                row.HasDiscount))
            .ToList();

        return new PagedResult<PlatformSubscriptionListItem>(items, page, total);
    }

    public async Task<PagedResult<PlatformSubscriptionEventItem>> ListEventsAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformSubscriptionEvent> events = _context.SubscriptionEvents
            .AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId);

        int total = await events.CountAsync(cancellationToken);
        List<PlatformSubscriptionEventItem> items = await events
            .OrderByDescending(item => item.CreatedUtc)
            .ThenByDescending(item => item.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(item => new PlatformSubscriptionEventItem(
                item.Id,
                item.PlatformSubscriptionId,
                item.EventCode,
                item.FromStatus,
                item.ToStatus,
                item.Reason,
                _context.Users.Where(user => user.Id == item.ActorUserId).Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(),
                item.Arguments,
                item.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformSubscriptionEventItem>(items, page, total);
    }

    public Task<int> CountSubscribersAsync(Guid platformPlanId, CancellationToken cancellationToken)
    {
        return _context.Subscriptions.CountAsync(
            subscription => subscription.PlatformPlanId == platformPlanId && subscription.Status != PlatformSubscriptionStatus.Ended,
            cancellationToken);
    }

    public async Task<PagedResult<PlatformPromotionListItem>> ListPromotionsAsync(
        PlatformPromotionListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        string language = LanguageCode.TryCreate(filter.Language, out LanguageCode? parsed) ? parsed.Value : LanguageCode.Default.Value;
        IQueryable<PlatformPromotion> promotions = _context.Promotions.AsNoTracking();

        if (filter.Status is PlatformPromotionStatus status)
        {
            promotions = promotions.Where(promotion => promotion.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            promotions = promotions.Where(promotion =>
                EF.Functions.Like(promotion.Code, pattern, "\\")
                || promotion.Names.Any(name => EF.Functions.Like(name.Name, pattern, "\\")));
        }

        int total = await promotions.CountAsync(cancellationToken);
        List<PlatformPromotionListItem> items = await promotions
            .OrderByDescending(promotion => promotion.CreatedUtc)
            .ThenBy(promotion => promotion.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(promotion => new PlatformPromotionListItem(
                promotion.Id,
                promotion.Code,
                promotion.Names.Where(name => name.Language == language).Select(name => name.Name).FirstOrDefault()
                    ?? promotion.Names.Where(name => name.Language == LanguageCode.Default.Value).Select(name => name.Name).FirstOrDefault()
                    ?? promotion.Code,
                promotion.Status,
                promotion.Terms.Kind,
                promotion.Terms.Value,
                promotion.Terms.Duration,
                promotion.Terms.Periods,
                promotion.Terms.EndsUtc,
                promotion.ValidFromUtc,
                promotion.ValidToUtc,
                promotion.AtRegistration,
                promotion.ByStaff,
                promotion.RedemptionLimit,
                promotion.Redemptions))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformPromotionListItem>(items, page, total);
    }

    public async Task<PagedResult<PlatformOfferListItem>> ListOffersAsync(
        PlatformOfferListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        LanguageCode language = LanguageCode.TryCreate(filter.Language, out LanguageCode? parsed) ? parsed : LanguageCode.Default;
        var offers =
            from offer in _context.Offers.AsNoTracking()
            join plan in _context.Plans.AsNoTracking() on offer.PlatformPlanId equals plan.Id
            join preparer in _context.Users.AsNoTracking() on offer.PreparedByUserId equals preparer.Id
            select new { offer, plan, preparer };

        if (filter.Status is PlatformOfferStatus status)
        {
            offers = offers.Where(row => row.offer.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            offers = offers.Where(row =>
                EF.Functions.Like(row.offer.CompanyName, pattern, "\\")
                || EF.Functions.Like(row.offer.BoundEmail!, pattern, "\\")
                || EF.Functions.Like(row.plan.Code, pattern, "\\"));
        }

        int total = await offers.CountAsync(cancellationToken);
        var rows = await offers
            .OrderByDescending(row => row.offer.CreatedUtc)
            .ThenBy(row => row.offer.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(row => new
            {
                row.offer.Id,
                row.offer.CompanyName,
                row.offer.BoundEmail,
                PlanCode = row.plan.Code,
                PlanName = row.plan.Translations.Where(item => item.Language == language).Select(item => item.Name).FirstOrDefault()
                    ?? row.plan.Translations.Where(item => item.Language == LanguageCode.Default).Select(item => item.Name).FirstOrDefault(),
                row.offer.Term,
                row.offer.Licences,
                Kind = row.offer.Discount == null ? (PlatformDiscountKind?)null : row.offer.Discount.Kind,
                Value = row.offer.Discount == null ? (decimal?)null : row.offer.Discount.Value,
                row.offer.Status,
                row.offer.ExpiresUtc,
                row.offer.UsesAllowed,
                row.offer.Uses,
                Preparer = row.preparer.FirstName + " " + row.preparer.LastName,
                row.offer.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformOfferListItem>(
            rows.Select(row => new PlatformOfferListItem(
                row.Id,
                row.CompanyName,
                row.BoundEmail,
                row.PlanCode,
                row.PlanName ?? row.PlanCode,
                row.Term,
                row.Licences,
                row.Kind,
                row.Value,
                row.Status,
                row.ExpiresUtc,
                row.UsesAllowed,
                row.Uses,
                row.Preparer,
                row.CreatedUtc)).ToList(),
            page,
            total);
    }

    public async Task<IReadOnlyList<PlatformApprovalItem>> ListApprovalsAsync(
        IReadOnlyCollection<PlatformApprovalKind> kinds,
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(kinds);

        string code = LanguageCode.TryCreate(language, out LanguageCode? parsed) ? parsed.Value : LanguageCode.Default.Value;
        List<PlatformApprovalItem> items = new();

        if (kinds.Contains(PlatformApprovalKind.Discount))
        {
            var discounts = await (
                    from subscription in _context.Subscriptions.AsNoTracking()
                    from discount in subscription.Discounts
                    join tenant in _context.Tenants.AsNoTracking() on subscription.PlatformTenantId equals tenant.Id
                    where discount.Status == PlatformDiscountStatus.PendingApproval
                    select new
                    {
                        discount.Id,
                        subscription.PlatformTenantId,
                        Name = tenant.Company.TradingName ?? tenant.Company.LegalName,
                        discount.Terms.Kind,
                        discount.Terms.Value,
                        discount.GrantedByUserId,
                        discount.CreatedUtc,
                        discount.Reason
                    })
                .ToListAsync(cancellationToken);
            items.AddRange(discounts.Select(row => new PlatformApprovalItem(
                PlatformApprovalKind.Discount,
                row.Id,
                row.PlatformTenantId,
                row.Name,
                row.Kind == PlatformDiscountKind.Percent ? row.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " %" : row.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                row.GrantedByUserId,
                null,
                row.CreatedUtc,
                row.Reason)));
        }

        if (kinds.Contains(PlatformApprovalKind.Offer))
        {
            var offers = await _context.Offers.AsNoTracking()
                .Where(offer => offer.Status == PlatformOfferStatus.PendingApproval)
                .Select(offer => new { offer.Id, offer.CompanyName, offer.PreparedByUserId, offer.SubmittedUtc, offer.CreatedUtc, offer.Reason })
                .ToListAsync(cancellationToken);
            items.AddRange(offers.Select(row => new PlatformApprovalItem(
                PlatformApprovalKind.Offer, row.Id, null, row.CompanyName, null, row.PreparedByUserId, null, row.SubmittedUtc ?? row.CreatedUtc, row.Reason)));
        }

        if (kinds.Contains(PlatformApprovalKind.Promotion))
        {
            var promotions = await _context.Promotions.AsNoTracking()
                .Where(promotion => promotion.Status == PlatformPromotionStatus.PendingApproval)
                .Select(promotion => new
                {
                    promotion.Id,
                    promotion.Code,
                    Name = promotion.Names.Where(name => name.Language == code).Select(name => name.Name).FirstOrDefault()
                        ?? promotion.Names.Where(name => name.Language == LanguageCode.Default.Value).Select(name => name.Name).FirstOrDefault(),
                    promotion.SubmittedByUserId,
                    Requested = promotion.LastModifiedUtc ?? promotion.CreatedUtc
                })
                .ToListAsync(cancellationToken);
            items.AddRange(promotions.Select(row => new PlatformApprovalItem(
                PlatformApprovalKind.Promotion, row.Id, null, row.Code, row.Name, row.SubmittedByUserId, null, row.Requested, null)));
        }

        if (kinds.Contains(PlatformApprovalKind.PlanVersion))
        {
            var versions = await (
                    from version in _context.Set<PlatformPlanVersion>().AsNoTracking()
                    join plan in _context.Plans.AsNoTracking() on version.PlatformPlanId equals plan.Id
                    where version.Status == PlatformPlanVersionStatus.PendingApproval
                    select new { plan.Id, plan.Code, version.Number, version.SubmittedByUserId, version.SubmittedUtc, version.CreatedUtc })
                .ToListAsync(cancellationToken);
            items.AddRange(versions.Select(row => new PlatformApprovalItem(
                PlatformApprovalKind.PlanVersion,
                row.Id,
                null,
                row.Code,
                "v" + row.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.SubmittedByUserId,
                null,
                row.SubmittedUtc ?? row.CreatedUtc,
                null)));
        }

        if (kinds.Contains(PlatformApprovalKind.CreditNote))
        {
            var credits = await (
                    from invoice in _context.Invoices.AsNoTracking()
                    from credit in invoice.Credits
                    where credit.Status == Vixly.Platform.Domain.Billing.PlatformCreditNoteStatus.Requested
                    select new { invoice.Id, invoice.PlatformTenantId, invoice.Number, credit.Amount, credit.IsVoid, invoice.CurrencyCode, credit.RequestedByUserId, credit.CreatedUtc, credit.Reason })
                .ToListAsync(cancellationToken);
            items.AddRange(credits.Select(row => new PlatformApprovalItem(
                PlatformApprovalKind.CreditNote,
                row.Id,
                row.PlatformTenantId,
                row.Number,
                (row.IsVoid ? "void " : string.Empty) + row.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + row.CurrencyCode,
                row.RequestedByUserId,
                null,
                row.CreatedUtc,
                row.Reason)));
        }

        IReadOnlyDictionary<Guid, string> names = await StaffNamesAsync(
            items.Where(item => item.RequestedByUserId is not null).Select(item => item.RequestedByUserId!.Value),
            cancellationToken);

        return items
            .Select(item => item with { RequestedByName = item.RequestedByUserId is Guid id && names.TryGetValue(id, out string? name) ? name : null })
            .OrderBy(item => item.RequestedUtc)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> StaffNamesAsync(IEnumerable<Guid> platformUserIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(platformUserIds);

        Guid[] ids = platformUserIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FirstName + " " + user.LastName, cancellationToken);
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
