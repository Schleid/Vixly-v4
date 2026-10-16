using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Persistence.Queries;

// =========================================================
// FILE: PlatformZelleQueries.cs
//
// PURPOSE:
// Read models for the Zelle details and reports.
//
// RESPONSIBILITIES:
// - Project the details without the image
// - Page the reports with tenant, invoice balance and staff names
//
// IMPORTANT NOTES:
// - No tracking; newest reports first
// =========================================================
internal sealed class PlatformZelleQueries : IPlatformZelleQueries
{
    private readonly PlatformDbContext _context;

    public PlatformZelleQueries(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<PlatformZelleAccountItem?> GetAccountAsync(CancellationToken cancellationToken)
    {
        return _context.ZelleAccounts.AsNoTracking()
            .Where(item => item.Key == PlatformPaymentsLimits.SingletonKey)
            .Select(item => new PlatformZelleAccountItem(item.Enabled, item.RecipientName, item.Email, item.Tag, item.Phone, item.QrCodeContentType != null, item.QrCodeVersion, item.LastModifiedUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlatformZelleImage?> GetQrCodeAsync(CancellationToken cancellationToken)
    {
        var row = await _context.ZelleAccounts.AsNoTracking()
            .Where(item => item.Key == PlatformPaymentsLimits.SingletonKey && item.QrCode != null && item.QrCodeContentType != null)
            .Select(item => new { item.QrCode, item.QrCodeContentType, item.QrCodeVersion, item.Enabled })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new PlatformZelleImage(row.QrCode!, row.QrCodeContentType!, row.QrCodeVersion, row.Enabled);
    }

    public async Task<PagedResult<PlatformZelleClaimItem>> ListClaimsAsync(PlatformZelleClaimFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(page);

        IQueryable<PlatformZelleClaim> claims = _context.ZelleClaims.AsNoTracking();

        if (filter.Status is { } status)
        {
            claims = claims.Where(item => item.Status == status);
        }

        if (filter.PlatformTenantId is Guid tenantId)
        {
            claims = claims.Where(item => item.PlatformTenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search;
            claims = claims.Where(item => item.InvoiceNumber.Contains(search) || item.SenderName.Contains(search) || item.Reference.Contains(search));
        }

        int total = await claims.CountAsync(cancellationToken);
        List<PlatformZelleClaimItem> rows = await Project(claims.OrderByDescending(item => item.CreatedUtc).Skip(page.Skip).Take(page.PageSize)).ToListAsync(cancellationToken);
        return new PagedResult<PlatformZelleClaimItem>(rows, page, total);
    }

    public Task<PlatformZelleClaimItem?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        return Project(_context.ZelleClaims.AsNoTracking().Where(item => item.Id == claimId)).FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<PlatformZelleClaimItem> Project(IQueryable<PlatformZelleClaim> claims)
    {
        return
            from claim in claims
            join tenant in _context.Tenants.AsNoTracking() on claim.PlatformTenantId equals tenant.Id
            join invoice in _context.Invoices.AsNoTracking() on claim.PlatformInvoiceId equals invoice.Id
            join user in _context.Users.AsNoTracking() on claim.DecidedByUserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            select new PlatformZelleClaimItem(
                claim.Id,
                claim.PlatformTenantId,
                tenant.Company.TradingName ?? tenant.Company.LegalName,
                claim.PlatformInvoiceId,
                claim.InvoiceNumber,
                claim.CurrencyCode,
                claim.Amount,
                invoice.Balance,
                claim.SentOn,
                claim.SenderName,
                claim.Reference,
                claim.Source,
                claim.Status,
                claim.CreatedUtc,
                claim.ReceivedAmount,
                claim.DecidedUtc,
                user == null ? null : user.FirstName + " " + user.LastName,
                claim.RejectReason,
                claim.StaffNote);
    }
}
