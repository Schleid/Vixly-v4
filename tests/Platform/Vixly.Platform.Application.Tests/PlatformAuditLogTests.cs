using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Audit.GetAuditEntry;
using Vixly.Platform.Application.UseCases.Audit.ListAuditEntries;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformAuditLogTests.cs
//
// PURPOSE:
// Proves the audit log use cases.
//
// RESPONSIBILITIES:
// - Permission: platform.audit.view, refused without it
// - Filters: unknown action codes and backwards windows refused,
//   good filters passed through
// - Unknown entry answers NotFound
//
// IMPORTANT NOTES:
// - Uses the real gate and authorization service over the
//   in-memory store
// =========================================================
public sealed class PlatformAuditLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task List_NeedsTheAuditPermission()
    {
        PlatformAuthorizationContext standard = User(PlatformUserAuthorityLevel.Standard);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            List().ExecuteAsync(standard, new ListAuditEntriesRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task List_RefusesUnknownActions_AndBackwardsWindows()
    {
        PlatformAuthorizationContext context = User(PlatformUserAuthorityLevel.SuperUser);

        ListAuditEntriesResponse unknown = await List().ExecuteAsync(context, new ListAuditEntriesRequest { ActionCode = "platform.nothing.done" }, CancellationToken.None);
        ListAuditEntriesResponse backwards = await List().ExecuteAsync(context, new ListAuditEntriesRequest { FromUtc = Now, ToUtc = Now.AddDays(-1) }, CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.RequestInvalid, unknown.Error);
        Assert.Equal(PlatformCommonErrors.RequestInvalid, backwards.Error);
    }

    [Fact]
    public async Task List_PassesTheFiltersThrough()
    {
        PlatformAuthorizationContext context = User(PlatformUserAuthorityLevel.SuperUser);
        Guid target = Guid.NewGuid();

        ListAuditEntriesResponse response = await List().ExecuteAsync(
            context,
            new ListAuditEntriesRequest { TargetId = target, ActionCode = "platform.users.locked", FromUtc = Now.AddDays(-1), ToUtc = Now },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(target, _store.LastAuditFilter!.TargetId);
        Assert.Equal("platform.users.locked", _store.LastAuditFilter.ActionCode);
    }

    [Fact]
    public async Task Get_UnknownEntry_IsNotFound()
    {
        PlatformAuthorizationContext context = User(PlatformUserAuthorityLevel.SuperUser);

        GetAuditEntryResponse response = await new GetAuditEntryUseCase(Gate(), _store).ExecuteAsync(
            context,
            new GetAuditEntryRequest { AuditEntryId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.NotFound, response.Error);
    }

    private ListAuditEntriesUseCase List()
    {
        return new ListAuditEntriesUseCase(Gate(), _store);
    }

    private PlatformAuthorizationGate Gate()
    {
        return new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock));
    }

    private PlatformAuthorizationContext User(PlatformUserAuthorityLevel level)
    {
        PlatformUser user = PlatformUser.Invite("Audit", "Reader", EmailAddress.Create($"a{_store.Users.Count}@example.com"), LanguageCode.En, level, null, null, Now);
        user.AcceptInvitation("H:password", Now);
        _store.Users.Add(user);

        return new PlatformAuthorizationContext(user.Id, level);
    }
}
