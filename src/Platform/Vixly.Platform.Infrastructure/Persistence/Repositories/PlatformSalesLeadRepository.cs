using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformSalesLeadRepository.cs
//
// PURPOSE:
// Stores, lists and prunes contact requests (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Add and load leads
// - Page the list with the staff name of the last update
// - Delete closed leads past retention
//
// IMPORTANT NOTES:
// - Lists read without tracking
// =========================================================
internal sealed class PlatformSalesLeadRepository : IPlatformSalesLeadRepository
{
    private readonly PlatformDbContext _context;

    public PlatformSalesLeadRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(PlatformSalesLead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);
        _context.SalesLeads.Add(lead);
    }

    public Task<PlatformSalesLead?> GetAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return _context.SalesLeads.FirstOrDefaultAsync(item => item.Id == leadId, cancellationToken);
    }

    public async Task<PlatformSalesLeadItem?> GetItemAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return (await Project(_context.SalesLeads.AsNoTracking().Where(item => item.Id == leadId)).ToListAsync(cancellationToken)).Select(Map).FirstOrDefault();
    }

    public async Task<PagedResult<PlatformSalesLeadItem>> ListAsync(PlatformSalesLeadFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformSalesLead> leads = _context.SalesLeads.AsNoTracking();

        if (filter.Status is { } status)
        {
            leads = leads.Where(item => item.Status == status);
        }

        if (filter.Search is { } search)
        {
            leads = leads.Where(item => item.CompanyName.Contains(search) || item.ContactName.Contains(search) || item.Email.Contains(search));
        }

        int total = await leads.CountAsync(cancellationToken);
        var rows = await Project(leads.OrderByDescending(item => item.CreatedUtc).Skip(page.Skip).Take(page.PageSize)).ToListAsync(cancellationToken);

        return new PagedResult<PlatformSalesLeadItem>(rows.Select(Map).ToArray(), page, total);
    }

    public Task<int> DeleteClosedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        return _context.SalesLeads.Where(item => item.Status == PlatformSalesLeadStatus.Closed && item.StatusChangedUtc < cutoffUtc).ExecuteDeleteAsync(cancellationToken);
    }

    private IQueryable<Row> Project(IQueryable<PlatformSalesLead> leads)
    {
        return
            from lead in leads
            join user in _context.Users.AsNoTracking() on lead.HandledByUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            select new Row(lead, user == null ? null : user.FirstName + " " + user.LastName);
    }

    private static PlatformSalesLeadItem Map(Row row)
    {
        PlatformSalesLead lead = row.Lead;
        return new PlatformSalesLeadItem(lead.Id, lead.CompanyName, lead.ContactName, lead.Email, lead.Phone, lead.CountryCode, lead.Kind, lead.Message,
            lead.Language.Value, lead.SourcePage, lead.Status, lead.CreatedUtc, lead.StatusChangedUtc, lead.StaffNote, row.HandledByName);
    }

    private sealed record Row(PlatformSalesLead Lead, string? HandledByName);
}
