using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeZelleStore.cs
//
// PURPOSE:
// In-memory Zelle details, reports, read models and mailer for the Zelle tests.
//
// RESPONSIBILITIES:
// - Keep the account and the reports; answer the repository and query ports
// - Record every Zelle email instead of sending it
//
// IMPORTANT NOTES:
// - Read models leave tenant names and balances blank; the database tests cover them
// =========================================================
internal sealed class FakeZelleStore : IPlatformZelleRepository, IPlatformZelleQueries, IPlatformZelleMailer
{
    public PlatformZelleAccount? Account { get; set; }

    public List<PlatformZelleClaim> Claims { get; } = new();

    public List<(string To, PlatformZelleMail Mail)> Mails { get; } = new();

    Task<PlatformZelleAccount?> IPlatformZelleRepository.GetAccountAsync(CancellationToken cancellationToken) => Task.FromResult(Account);

    public void AddAccount(PlatformZelleAccount account) => Account = account;

    public Task<PlatformZelleClaim?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken) => Task.FromResult(Claims.FirstOrDefault(item => item.Id == claimId));

    public Task<PlatformZelleClaim?> FindWaitingAsync(Guid invoiceId, CancellationToken cancellationToken) => Task.FromResult(Claims.FirstOrDefault(item => item.PlatformInvoiceId == invoiceId && item.IsWaiting));

    public Task<int> CountForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) => Task.FromResult(Claims.Count(item => item.PlatformInvoiceId == invoiceId));

    public Task<IReadOnlyList<Guid>> ListWaitingInvoiceIdsAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Claims.Where(item => item.PlatformTenantId == platformTenantId && item.IsWaiting).Select(item => item.PlatformInvoiceId).ToArray());
    }

    public void AddClaim(PlatformZelleClaim claim) => Claims.Add(claim);

    Task<PlatformZelleAccountItem?> IPlatformZelleQueries.GetAccountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Account is { } account
            ? new PlatformZelleAccountItem(account.Enabled, account.RecipientName, account.Email, account.Tag, account.Phone, account.HasQrCode, account.QrCodeVersion, account.LastModifiedUtc)
            : null);
    }

    public Task<PlatformZelleImage?> GetQrCodeAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Account is { QrCode: { } content, QrCodeContentType: { } type } account ? new PlatformZelleImage(content, type, account.QrCodeVersion, account.Enabled) : null);
    }

    public Task<PagedResult<PlatformZelleClaimItem>> ListClaimsAsync(PlatformZelleClaimFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        PlatformZelleClaimItem[] items = Claims
            .Where(item => filter.Status is null || item.Status == filter.Status)
            .Where(item => filter.PlatformTenantId is null || item.PlatformTenantId == filter.PlatformTenantId)
            .OrderByDescending(item => item.CreatedUtc)
            .Select(Item)
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformZelleClaimItem>(items.Skip(page.Skip).Take(page.PageSize).ToArray(), page, items.Length));
    }

    Task<PlatformZelleClaimItem?> IPlatformZelleQueries.GetClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Claims.FirstOrDefault(item => item.Id == claimId) is { } claim ? Item(claim) : null);
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformZelleMail mail, CancellationToken cancellationToken)
    {
        Mails.Add((recipient.Email.Value, mail));
        return Task.CompletedTask;
    }

    private static PlatformZelleClaimItem Item(PlatformZelleClaim claim)
    {
        return new PlatformZelleClaimItem(claim.Id, claim.PlatformTenantId, string.Empty, claim.PlatformInvoiceId, claim.InvoiceNumber, claim.CurrencyCode, claim.Amount, 0m, claim.SentOn,
            claim.SenderName, claim.Reference, claim.Source, claim.Status, claim.CreatedUtc, claim.ReceivedAmount, claim.DecidedUtc, null, claim.RejectReason, claim.StaffNote);
    }
}
