using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: ControlTenantPreviewData.cs
//
// PURPOSE:
// Supplies controlled tenant pages to dashboard rendering tests.
//
// RESPONSIBILITIES:
// - Return a page, delay completion or deny the read
//
// IMPORTANT NOTES:
// - Test only; no database or authenticated production services
// =========================================================
internal sealed class ControlTenantPreviewData : IPlatformTenantQueries, IPlatformTenantPolicy
{
    public TaskCompletionSource<PagedResult<PlatformTenantListItem>> Result { get; } = new();
    public int Reads { get; private set; }
    public IReadOnlyCollection<string> ReservedSlugs => Array.Empty<string>();
    public string TermsVersion => "test";
    public string PrivacyVersion => "test";
    public string TenantBaseDomain => "example.test";
    public bool RegistrationInviteOnly => true;

    public Task<PagedResult<PlatformTenantListItem>> ListAsync(PlatformTenantListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        Reads++;
        return Result.Task;
    }

    public void Complete(int total, int rows = 0)
    {
        PlatformTenantListItem[] items = Enumerable.Range(1, rows).Select(number => new PlatformTenantListItem(
            Guid.NewGuid(), $"T-{number}", $"Tenant {number}", $"tenant-{number}", PlatformTenantStatus.Active,
            "US", null, true, Array.Empty<string>(), DateTimeOffset.UtcNow)).ToArray();
        Result.SetResult(new PagedResult<PlatformTenantListItem>(items, PagedRequest.Create(1, 20), total));
    }

    public Task<PlatformTenantDetail?> GetAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<PagedResult<PlatformTenantEventItem>> ListEventsAsync(Guid id, PagedRequest page, CancellationToken ct) => throw new NotSupportedException();
    public Task<PagedResult<PlatformTenantNoteItem>> ListNotesAsync(Guid id, PagedRequest page, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<PlatformStaffOptionItem>> ListActiveStaffAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<PagedResult<PlatformTenantRegistrationListItem>> ListRegistrationsAsync(PlatformTenantRegistrationStatus? status, string? search, PagedRequest page, CancellationToken ct) => throw new NotSupportedException();
    public Task<PlatformTenantRegistrationDetail?> GetRegistrationAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
}
