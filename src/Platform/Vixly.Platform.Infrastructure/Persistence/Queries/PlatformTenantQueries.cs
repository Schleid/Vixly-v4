using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformTenantQueries.cs
//
// PURPOSE:
// EF projections of tenants and registrations for the console.
//
// RESPONSIBILITIES:
// - Page and filter the tenant directory (P-1)
// - Read one tenant, its history and its notes
// - List active staff and registrations
//
// IMPORTANT NOTES:
// - No tracking; only the columns each view needs
// - Phone search compares digits only, as stored
// =========================================================
internal sealed class PlatformTenantQueries : IPlatformTenantQueries
{
    private readonly PlatformDbContext _context;

    public PlatformTenantQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformTenantListItem>> ListAsync(
        PlatformTenantListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformTenant> tenants = _context.Tenants.AsNoTracking();

        if (filter.Status is PlatformTenantStatus status)
        {
            tenants = tenants.Where(tenant => tenant.Status == status);
        }

        if (filter.Tag is { Length: > 0 } tag)
        {
            tenants = tenants.Where(tenant => tenant.Tags.Any(item => item.Value == tag));
        }

        if (filter.AccountManagerUserId is Guid managerId)
        {
            tenants = tenants.Where(tenant => tenant.AccountManagerUserId == managerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string pattern = "%" + EscapeLike(filter.Search.Trim()) + "%";
            string digits = new(filter.Search.Where(char.IsAsciiDigit).ToArray());
            string phonePattern = digits.Length >= 4 ? "%" + digits + "%" : "\u0001";

            tenants = tenants.Where(tenant =>
                EF.Functions.Like(tenant.Company.LegalName, pattern, "\\")
                || EF.Functions.Like(tenant.Company.TradingName!, pattern, "\\")
                || EF.Functions.Like(tenant.TenantCode, pattern, "\\")
                || EF.Functions.Like(tenant.CurrentSlug, pattern, "\\")
                || EF.Functions.Like((string)(object)tenant.Company.Email, pattern, "\\")
                || EF.Functions.Like((string)(object)tenant.Company.Phone, phonePattern));
        }

        int total = await tenants.CountAsync(cancellationToken);
        List<PlatformTenantListItem> items = await tenants
            .OrderBy(tenant => tenant.Company.TradingName ?? tenant.Company.LegalName)
            .ThenBy(tenant => tenant.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(tenant => new PlatformTenantListItem(
                tenant.Id,
                tenant.TenantCode,
                tenant.Company.TradingName ?? tenant.Company.LegalName,
                tenant.CurrentSlug,
                tenant.Status,
                tenant.Company.Address.CountryCode,
                _context.Users.Where(user => user.Id == tenant.AccountManagerUserId).Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(),
                _context.Users.Any(user => user.Id == tenant.AccountManagerUserId && user.Account.Status == PlatformUserAccountStatus.Active),
                tenant.Tags.OrderBy(item => item.Value).Select(item => item.Value).ToList(),
                tenant.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformTenantListItem>(items, page, total);
    }

    public async Task<PlatformTenantDetail?> GetAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        PlatformTenant? tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == platformTenantId, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        Guid?[] people = new[] { tenant.AccountManagerUserId, tenant.OpenSuspension?.SuspendedByUserId };
        Dictionary<Guid, (string Name, bool Active)> names = await NamesAsync(people, cancellationToken);
        PlatformTenantSuspension? open = tenant.OpenSuspension;

        return new PlatformTenantDetail(
            tenant.Id,
            tenant.TenantCode,
            tenant.CurrentSlug,
            tenant.Status,
            tenant.StatusChangedUtc,
            tenant.ActivatedUtc,
            tenant.Origin,
            tenant.Company.LegalName,
            tenant.Company.TradingName,
            tenant.Company.Email.Value,
            tenant.Company.Phone.Value,
            tenant.Company.Address.Line1,
            tenant.Company.Address.Line2,
            tenant.Company.Address.City,
            tenant.Company.Address.StateProvince,
            tenant.Company.Address.PostalCode,
            tenant.Company.Address.CountryCode,
            tenant.DefaultLanguage.Value,
            tenant.TimeZone,
            tenant.AccountManagerUserId,
            Name(names, tenant.AccountManagerUserId),
            tenant.AccountManagerUserId is Guid manager && names.TryGetValue(manager, out (string Name, bool Active) found) && found.Active,
            tenant.Tags.Select(tag => tag.Value).Order(StringComparer.Ordinal).ToList(),
            tenant.Contacts
                .OrderBy(contact => contact.Role)
                .Select(contact => new PlatformTenantContactItem(contact.Role, contact.Name, contact.Email.Value, contact.Phone?.Value))
                .ToList(),
            open is null ? null : new PlatformTenantSuspensionItem(open.Category, open.Reason, Name(names, open.SuspendedByUserId), open.CreatedUtc),
            tenant.CreatedUtc);
    }

    public async Task<PagedResult<PlatformTenantEventItem>> ListEventsAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformTenantLifecycleEvent> events = _context.TenantLifecycleEvents
            .AsNoTracking()
            .Where(item => item.PlatformTenantId == platformTenantId);
        int total = await events.CountAsync(cancellationToken);
        List<PlatformTenantLifecycleEvent> rows = await events
            .OrderByDescending(item => item.CreatedUtc)
            .ThenByDescending(item => item.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);
        Dictionary<Guid, (string Name, bool Active)> names = await NamesAsync(rows.Select(item => item.ActorUserId), cancellationToken);

        return new PagedResult<PlatformTenantEventItem>(
            rows.Select(item => new PlatformTenantEventItem(
                item.Id,
                item.EventCode,
                item.FromStatus,
                item.ToStatus,
                item.Category,
                item.Reason,
                Name(names, item.ActorUserId),
                item.Arguments,
                item.CreatedUtc)).ToList(),
            page,
            total);
    }

    public async Task<PagedResult<PlatformTenantNoteItem>> ListNotesAsync(
        Guid platformTenantId,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformTenantNote> notes = _context.TenantNotes.AsNoTracking().Where(item => item.PlatformTenantId == platformTenantId);
        int total = await notes.CountAsync(cancellationToken);
        List<PlatformTenantNoteItem> items = await notes
            .OrderByDescending(item => item.CreatedUtc)
            .ThenByDescending(item => item.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(item => new PlatformTenantNoteItem(
                item.Id,
                item.Body,
                _context.Users.Where(user => user.Id == item.AuthorUserId).Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(),
                item.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformTenantNoteItem>(items, page, total);
    }

    public async Task<IReadOnlyList<PlatformStaffOptionItem>> ListActiveStaffAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Account.Status == PlatformUserAccountStatus.Active)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new PlatformStaffOptionItem(user.Id, user.FirstName + " " + user.LastName))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<PlatformTenantRegistrationListItem>> ListRegistrationsAsync(
        PlatformTenantRegistrationStatus? status,
        string? search,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformTenantRegistration> registrations = _context.TenantRegistrations.AsNoTracking();

        if (status is PlatformTenantRegistrationStatus wanted)
        {
            registrations = registrations.Where(item => item.Status == wanted);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = "%" + EscapeLike(search.Trim()) + "%";
            registrations = registrations.Where(item =>
                EF.Functions.Like(item.Company.LegalName, pattern, "\\")
                || EF.Functions.Like(item.OwnerName, pattern, "\\")
                || EF.Functions.Like((string)(object)item.OwnerEmail, pattern, "\\")
                || EF.Functions.Like(item.Slug, pattern, "\\"));
        }

        int total = await registrations.CountAsync(cancellationToken);
        List<PlatformTenantRegistrationListItem> items = await registrations
            .OrderByDescending(item => item.CreatedUtc)
            .ThenByDescending(item => item.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(item => new PlatformTenantRegistrationListItem(
                item.Id,
                item.Status,
                item.Company.LegalName,
                item.OwnerName,
                (string)(object)item.OwnerEmail,
                item.Slug,
                item.CreatedUtc,
                item.EmailVerifiedUtc,
                item.PlatformTenantId))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlatformTenantRegistrationListItem>(items, page, total);
    }

    public async Task<PlatformTenantRegistrationDetail?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        PlatformTenantRegistration? item = await _context.TenantRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == registrationId, cancellationToken);

        if (item is null)
        {
            return null;
        }

        Dictionary<Guid, (string Name, bool Active)> names = await NamesAsync(new[] { item.CancelledByUserId }, cancellationToken);

        return new PlatformTenantRegistrationDetail(
            item.Id,
            item.Status,
            item.Company.LegalName,
            item.Company.TradingName,
            item.Company.Email.Value,
            item.Company.Phone.Value,
            item.Company.Address.CountryCode,
            item.OwnerName,
            item.OwnerEmail.Value,
            item.OwnerPhone.Value,
            item.Language.Value,
            item.TimeZone,
            item.Slug,
            item.TermsVersion,
            item.PrivacyVersion,
            item.ConsentIpAddress,
            item.CreatedUtc,
            item.LastVerificationSentUtc,
            item.EmailVerifiedUtc,
            item.ClosedUtc,
            Name(names, item.CancelledByUserId),
            item.CancellationReason,
            item.PlatformTenantId);
    }

    private async Task<Dictionary<Guid, (string Name, bool Active)>> NamesAsync(
        IEnumerable<Guid?> userIds,
        CancellationToken cancellationToken)
    {
        Guid[] ids = userIds.OfType<Guid>().Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, (string Name, bool Active)>();
        }

        var rows = await _context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, Name = user.FirstName + " " + user.LastName, Active = user.Account.Status == PlatformUserAccountStatus.Active })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => (row.Name, row.Active));
    }

    private static string? Name(Dictionary<Guid, (string Name, bool Active)> names, Guid? userId)
    {
        return userId is Guid id && names.TryGetValue(id, out (string Name, bool Active) found) ? found.Name : null;
    }

    private static string EscapeLike(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
    }
}
