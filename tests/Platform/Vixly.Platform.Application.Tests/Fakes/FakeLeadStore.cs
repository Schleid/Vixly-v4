using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeLeadStore.cs
//
// PURPOSE:
// In-memory sales leads and the lead mailer for the use-case tests.
//
// RESPONSIBILITIES:
// - Store and list leads
// - Record each lead email
//
// IMPORTANT NOTES:
// - List filters by status and a plain search, newest first, like the database
// =========================================================
internal sealed class FakeLeadStore : IPlatformSalesLeadRepository, IPlatformLeadMailer
{
    public List<PlatformSalesLead> Leads { get; } = new();

    public List<(string Email, Guid LeadId)> Mails { get; } = new();

    public void Add(PlatformSalesLead lead)
    {
        Leads.Add(lead);
    }

    public Task<PlatformSalesLead?> GetAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Leads.FirstOrDefault(lead => lead.Id == leadId));
    }

    public Task<PlatformSalesLeadItem?> GetItemAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Leads.Where(lead => lead.Id == leadId).Select(Item).FirstOrDefault());
    }

    public Task<PagedResult<PlatformSalesLeadItem>> ListAsync(PlatformSalesLeadFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        List<PlatformSalesLead> matches = Leads
            .Where(lead => filter.Status is null || lead.Status == filter.Status)
            .Where(lead => string.IsNullOrWhiteSpace(filter.Search)
                || lead.CompanyName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || lead.Email.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(lead => lead.CreatedUtc)
            .ToList();

        return Task.FromResult(new PagedResult<PlatformSalesLeadItem>(matches.Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).Select(Item).ToList(), page, matches.Count));
    }

    public Task<int> DeleteClosedBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(Leads.RemoveAll(lead => lead.Status == PlatformSalesLeadStatus.Closed && lead.StatusChangedUtc < cutoffUtc));
    }

    public Task SendLeadReceivedAsync(PlatformPermissionHolderItem recipient, PlatformSalesLead lead, CancellationToken cancellationToken)
    {
        Mails.Add((recipient.Email, lead.Id));
        return Task.CompletedTask;
    }

    private static PlatformSalesLeadItem Item(PlatformSalesLead lead)
    {
        return new PlatformSalesLeadItem(lead.Id, lead.CompanyName, lead.ContactName, lead.Email, lead.Phone, lead.CountryCode, lead.Kind, lead.Message, lead.Language.Value,
            lead.SourcePage, lead.Status, lead.CreatedUtc, lead.StatusChangedUtc, lead.StaffNote, null);
    }
}
