using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeTenantStore.cs
//
// PURPOSE:
// In-memory tenant, slug, registration and outbox stores for
// application tests.
//
// RESPONSIBILITIES:
// - Implement the tenant ports with lists
// - Record sent mail and bot checks
//
// IMPORTANT NOTES:
// - Saving is FakePlatformStore's job; these lists change at once
// - Queries return what tests need, not full projections
// =========================================================
internal sealed class FakeTenantStore :
    IPlatformTenantRepository,
    IPlatformPortalSlugRepository,
    IPlatformTenantRegistrationRepository,
    IPlatformOutboxRepository,
    IPlatformTenantMailer,
    IPlatformBotCheck,
    IPlatformTenantPolicy,
    IPlatformRandom,
    IPlatformTenantQueries
{
    private byte _next = 1;

    public List<PlatformTenant> Tenants { get; } = new();

    public List<PlatformTenantLifecycleEvent> Events { get; } = new();

    public List<PlatformTenantNote> Notes { get; } = new();

    public List<PlatformPortalSlug> Slugs { get; } = new();

    public List<PlatformTenantRegistration> Registrations { get; } = new();

    public List<PlatformRegistrationToken> Tokens { get; } = new();

    public List<PlatformOutboxMessage> Outbox { get; } = new();

    public List<(string Kind, string To, string Value)> Mail { get; } = new();

    public bool BotPasses { get; set; } = true;

    public IReadOnlyCollection<string> ReservedSlugs { get; set; } = new[] { "billing" };

    public string TermsVersion => "2026-10";

    public string PrivacyVersion => "2026-09";

    public string TenantBaseDomain => "techcrafters.example";

    public bool RegistrationInviteOnly { get; set; }

    public void Fill(Span<byte> buffer)
    {
        for (int index = 0; index < buffer.Length; index++)
        {
            buffer[index] = _next++;
        }
    }

    public Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken cancellationToken)
    {
        return Task.FromResult(BotPasses && token == "human");
    }

    public Task<PlatformTenant?> GetAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Tenants.FirstOrDefault(tenant => tenant.Id == platformTenantId));
    }

    public Task<bool> CodeExistsAsync(string tenantCode, CancellationToken cancellationToken)
    {
        return Task.FromResult(Tenants.Any(tenant => tenant.TenantCode == tenantCode));
    }

    public void Add(PlatformTenant tenant)
    {
        Tenants.Add(tenant);
    }

    public void AddEvents(IEnumerable<PlatformTenantLifecycleEvent> events)
    {
        Events.AddRange(events);
    }

    public void AddNote(PlatformTenantNote note)
    {
        Notes.Add(note);
    }

    public Task<PlatformPortalSlug?> FindAsync(string slug, CancellationToken cancellationToken)
    {
        return Task.FromResult(Slugs.FirstOrDefault(claim => claim.Slug == slug));
    }

    public Task<PlatformPortalSlug?> FindPermanentForTenantAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Slugs.FirstOrDefault(claim =>
            claim.State == PlatformPortalSlugState.Permanent && claim.PlatformTenantId == platformTenantId));
    }

    public void Add(PlatformPortalSlug claim)
    {
        Slugs.Add(claim);
    }

    public void Remove(PlatformPortalSlug claim)
    {
        Slugs.Remove(claim);
    }

    Task<PlatformTenantRegistration?> IPlatformTenantRegistrationRepository.GetAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Registrations.FirstOrDefault(registration => registration.Id == registrationId));
    }

    public Task<PlatformTenantRegistration?> FindByAccessHashAsync(string accessTokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Registrations.FirstOrDefault(registration => registration.AccessTokenHash == accessTokenHash));
    }

    public Task<PlatformTenantRegistration?> FindByTenantAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Registrations.FirstOrDefault(registration => registration.PlatformTenantId == platformTenantId));
    }

    public Task<PlatformRegistrationToken?> FindTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Tokens.FirstOrDefault(token => token.TokenHash == tokenHash));
    }

    public Task<IReadOnlyList<PlatformRegistrationToken>> ListUnusedTokensAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformRegistrationToken>>(Tokens
            .Where(token => token.PlatformTenantRegistrationId == registrationId && token.UsedUtc is null && token.RevokedUtc is null)
            .ToList());
    }

    public void Add(PlatformTenantRegistration registration)
    {
        Registrations.Add(registration);
    }

    public void AddToken(PlatformRegistrationToken token)
    {
        Tokens.Add(token);
    }

    public void Add(PlatformOutboxMessage message)
    {
        Outbox.Add(message);
    }

    public Task<IReadOnlyList<Guid>> LeaseDueAsync(
        int maxCount,
        DateTimeOffset utcNow,
        DateTimeOffset leasedUntilUtc,
        CancellationToken cancellationToken)
    {
        List<PlatformOutboxMessage> due = Outbox
            .Where(message => message.ProcessedUtc is null
                && message.NextAttemptUtc <= utcNow
                && (message.LeasedUntilUtc is null || message.LeasedUntilUtc <= utcNow))
            .OrderBy(message => message.CreatedUtc)
            .Take(maxCount)
            .ToList();

        foreach (PlatformOutboxMessage message in due)
        {
            message.Lease(leasedUntilUtc, utcNow);
        }

        return Task.FromResult<IReadOnlyList<Guid>>(due.Select(message => message.Id).ToList());
    }

    Task<PlatformOutboxMessage?> IPlatformOutboxRepository.GetAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Outbox.FirstOrDefault(message => message.Id == messageId));
    }

    public Task SendRegistrationLinkAsync(PlatformTenantRegistration registration, string token, CancellationToken cancellationToken)
    {
        Mail.Add(("link", registration.OwnerEmail.Value, token));
        return Task.CompletedTask;
    }

    public Task SendCompanyChangeSubmittedAsync(PlatformPermissionHolderItem recipient, PlatformTenant tenant, PlatformTenantChangeRequest request, CancellationToken cancellationToken)
    {
        Mail.Add(("change_submitted", recipient.Email, request.Id.ToString("D")));
        return Task.CompletedTask;
    }

    public Task SendCompanyChangeDecidedAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformTenantChangeRequest request, CancellationToken cancellationToken)
    {
        Mail.Add(("change_decided", recipient.Email.Value, request.Status.ToString()));
        return Task.CompletedTask;
    }

    public Task SendStatusNoticeAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformTenantStatus status,
        PlatformSuspensionCategory? category,
        CancellationToken cancellationToken)
    {
        Mail.Add(("notice", recipient.Email.Value, $"{status}:{category}"));
        return Task.CompletedTask;
    }

    public Task<PagedResult<PlatformTenantListItem>> ListAsync(
        PlatformTenantListFilter filter,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformTenantListItem>.Empty(page));
    }

    Task<PlatformTenantDetail?> IPlatformTenantQueries.GetAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformTenantDetail?>(null);
    }

    public Task<PagedResult<PlatformTenantEventItem>> ListEventsAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformTenantEventItem>.Empty(page));
    }

    public Task<PagedResult<PlatformTenantNoteItem>> ListNotesAsync(Guid platformTenantId, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformTenantNoteItem>.Empty(page));
    }

    public Task<IReadOnlyList<PlatformStaffOptionItem>> ListActiveStaffAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformStaffOptionItem>>(Array.Empty<PlatformStaffOptionItem>());
    }

    public Task<PagedResult<PlatformTenantRegistrationListItem>> ListRegistrationsAsync(
        PlatformTenantRegistrationStatus? status,
        string? search,
        PagedRequest page,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformTenantRegistrationListItem>.Empty(page));
    }

    public Task<PlatformTenantRegistrationDetail?> GetRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformTenantRegistrationDetail?>(null);
    }
}
