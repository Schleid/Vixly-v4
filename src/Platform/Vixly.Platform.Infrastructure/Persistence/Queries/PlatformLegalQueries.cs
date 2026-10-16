using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformLegalQueries.cs
//
// PURPOSE:
// Read models for the legal screens, public pages and the reminder clock.
//
// RESPONSIBILITIES:
// - Documents and files without bytes, except the download
// - The register works out each tenant's status and deadline in SQL, so paging and the status filter stay in the database
//
// IMPORTANT NOTES:
// - No tracking; only Active and Suspended tenants are asked
// =========================================================
internal sealed class PlatformLegalQueries : IPlatformLegalQueries
{
    private readonly PlatformDbContext _context;

    public PlatformLegalQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PagedResult<PlatformLegalDocumentItem>> ListDocumentsAsync(PlatformLegalDocumentKind? kind, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        IQueryable<PlatformLegalDocument> documents = _context.LegalDocuments.AsNoTracking();

        if (kind is { } wanted)
        {
            documents = documents.Where(item => item.Kind == wanted);
        }

        int total = await documents.CountAsync(cancellationToken);
        List<PlatformLegalDocumentItem> items = await Project(documents.OrderBy(item => item.Kind).ThenByDescending(item => item.CreatedUtc).Skip(page.Skip).Take(page.PageSize)).ToListAsync(cancellationToken);
        return new PagedResult<PlatformLegalDocumentItem>(items, page, total);
    }

    public Task<PlatformLegalDocumentItem?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return Project(_context.LegalDocuments.AsNoTracking().Where(item => item.Id == documentId)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLegalDocumentItem>> ListPublishedAsync(CancellationToken cancellationToken)
    {
        return await Project(_context.LegalDocuments.AsNoTracking().Where(item => item.Status == PlatformLegalDocumentStatus.Published).OrderBy(item => item.Kind)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLegalFileItem>> ListFilesAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return await _context.LegalDocumentFiles.AsNoTracking()
            .Where(item => item.PlatformLegalDocumentId == documentId)
            .OrderBy(item => item.Language)
            .Select(item => new PlatformLegalFileItem(item.Language, item.Sha256, item.SizeBytes, item.Summary, item.LastModifiedUtc ?? item.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<PlatformLegalFileContent?> GetFileContentAsync(Guid documentId, string language, CancellationToken cancellationToken)
    {
        return (from file in _context.LegalDocumentFiles.AsNoTracking()
                join document in _context.LegalDocuments.AsNoTracking() on file.PlatformLegalDocumentId equals document.Id
                where file.PlatformLegalDocumentId == documentId && file.Language == language
                select new PlatformLegalFileContent(file.Content, file.Sha256, file.Language, document.Kind, document.Version, document.Status)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<PlatformLegalRegisterItem>> RegisterAsync(PlatformLegalDocument required, PlatformLegalRegisterFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        DateTimeOffset published = required.PublishedUtc ?? required.CreatedUtc;
        DateTimeOffset deadline = required.DeadlineUtc ?? required.EffectiveUtc ?? published;
        IQueryable<PlatformTenantTermsAcceptance> accepted =
            from acceptance in _context.TenantTermsAcceptances
            join document in _context.LegalDocuments on acceptance.PlatformLegalDocumentId equals document.Id
            where document.PublishedUtc >= published
            select acceptance;

        var rows =
            from tenant in _context.Tenants.AsNoTracking()
            where tenant.Status == PlatformTenantStatus.Active || tenant.Status == PlatformTenantStatus.Suspended
            select new
            {
                tenant.Id,
                Name = tenant.Company.TradingName ?? tenant.Company.LegalName,
                tenant.TenantCode,
                tenant.Status,
                Deadline = tenant.ActivatedUtc != null && tenant.ActivatedUtc > published && tenant.ActivatedUtc.Value.AddDays(PlatformLegalLimits.NewTenantDays) > deadline
                    ? tenant.ActivatedUtc.Value.AddDays(PlatformLegalLimits.NewTenantDays)
                    : deadline,
                AcceptedUtc = accepted.Where(item => item.PlatformTenantId == tenant.Id).Max(item => (DateTimeOffset?)item.AcceptedUtc),
                AcceptedVersion = accepted.Where(item => item.PlatformTenantId == tenant.Id).OrderByDescending(item => item.AcceptedUtc).Select(item => item.Version).FirstOrDefault(),
                LastReminder = _context.TermsReminders.Where(item => item.PlatformTenantId == tenant.Id).Max(item => (DateTimeOffset?)item.CreatedUtc)
            };

        rows = filter.Status switch
        {
            PlatformLegalStatus.Accepted => rows.Where(row => row.AcceptedUtc != null),
            PlatformLegalStatus.Due => rows.Where(row => row.AcceptedUtc == null && row.Deadline > utcNow),
            PlatformLegalStatus.Overdue => rows.Where(row => row.AcceptedUtc == null && row.Deadline <= utcNow),
            _ => rows
        };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            rows = rows.Where(row => row.Name.Contains(search) || row.TenantCode.Contains(search));
        }

        int total = await rows.CountAsync(cancellationToken);
        var list = await rows.OrderBy(row => row.AcceptedUtc != null).ThenBy(row => row.Deadline).ThenBy(row => row.Name).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<PlatformLegalRegisterItem>(
            list.Select(row => new PlatformLegalRegisterItem(row.Id, row.Name, row.TenantCode, row.Status, PlatformLegalRules.Status(true, row.AcceptedUtc is not null, row.Deadline, utcNow),
                row.Deadline, row.AcceptedUtc, row.AcceptedVersion, row.LastReminder)).ToArray(),
            page,
            total);
    }

    public async Task<IReadOnlyList<PlatformTermsAcceptanceItem>> ListAcceptancesAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await (from acceptance in _context.TenantTermsAcceptances.AsNoTracking()
                      join user in _context.Users.AsNoTracking() on acceptance.RecordedByUserId equals user.Id into users
                      from user in users.DefaultIfEmpty()
                      where acceptance.PlatformTenantId == platformTenantId
                      orderby acceptance.AcceptedUtc descending
                      select new PlatformTermsAcceptanceItem(acceptance.Id, acceptance.Version, acceptance.Language, acceptance.Source, acceptance.AcceptedUtc, acceptance.ActorName, acceptance.ActorRole,
                          acceptance.ClientIp, acceptance.UserAgent, acceptance.Sha256, user == null ? null : user.FirstName + " " + user.LastName, acceptance.Reason, acceptance.EvidenceContentType != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformTermsReminderItem>> ListRemindersAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return await (from reminder in _context.TermsReminders.AsNoTracking()
                      join document in _context.LegalDocuments.AsNoTracking() on reminder.PlatformLegalDocumentId equals document.Id
                      join user in _context.Users.AsNoTracking() on reminder.SentByUserId equals user.Id into users
                      from user in users.DefaultIfEmpty()
                      where reminder.PlatformTenantId == platformTenantId
                      orderby reminder.CreatedUtc descending
                      select new PlatformTermsReminderItem(reminder.Step, document.Version, reminder.Language, reminder.CreatedUtc, user == null ? null : user.FirstName + " " + user.LastName, reminder.Reason))
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlatformLegalTenantRow>> ListTenantsToRemindAsync(Guid documentId, DateTimeOffset? acceptedSinceUtc, int take, CancellationToken cancellationToken)
    {
        IQueryable<PlatformTenant> tenants = _context.Tenants.AsNoTracking().Where(tenant => tenant.Status == PlatformTenantStatus.Active || tenant.Status == PlatformTenantStatus.Suspended);

        if (acceptedSinceUtc is { } since)
        {
            tenants = tenants.Where(tenant => !(from acceptance in _context.TenantTermsAcceptances
                                                join document in _context.LegalDocuments on acceptance.PlatformLegalDocumentId equals document.Id
                                                where acceptance.PlatformTenantId == tenant.Id && document.PublishedUtc >= since
                                                select acceptance.Id).Any());
        }
        else
        {
            tenants = tenants.Where(tenant => !_context.TermsReminders.Any(reminder => reminder.PlatformTenantId == tenant.Id && reminder.PlatformLegalDocumentId == documentId));
        }

        var rows = await tenants.OrderBy(tenant => tenant.Id).Take(take).Select(tenant => new { tenant.Id, tenant.ActivatedUtc, tenant.DefaultLanguage }).ToListAsync(cancellationToken);
        return rows.Select(row => new PlatformLegalTenantRow(row.Id, row.ActivatedUtc, row.DefaultLanguage.Value)).ToArray();
    }

    private IQueryable<PlatformLegalDocumentItem> Project(IQueryable<PlatformLegalDocument> documents)
    {
        return from document in documents
               join user in _context.Users.AsNoTracking() on document.PublishedByUserId equals user.Id into users
               from user in users.DefaultIfEmpty()
               select new PlatformLegalDocumentItem(document.Id, document.Kind, document.Version, document.Status, document.AcceptanceRequired, document.EffectiveUtc, document.DeadlineUtc,
                   document.PublishedUtc, user == null ? null : user.FirstName + " " + user.LastName, document.CreatedUtc);
    }
}
