using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Web.Features;
using Vixly.Platform.Web.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PresenterTestKit.cs
//
// PURPOSE:
// Hand-written stand-ins that let presenters run without a
// Blazor host or a database (UI-9).
//
// RESPONSIBILITIES:
// - Sign a principal in, count unit-of-work resets, allow every
//   gate, and answer user facts and permission snapshots
//
// IMPORTANT NOTES:
// - Hand-written; no mocking library (Slice 0 convention)
// =========================================================
internal sealed class PresenterTestKit :
    IPlatformUnitOfWork,
    IPlatformAuthorizationGate,
    IPlatformUserQueries,
    IPlatformPermissionReader
{
    public static readonly Guid ActorId = Guid.CreateVersion7();

    public static readonly Guid TargetId = Guid.CreateVersion7();

    public int Discards { get; private set; }

    public PlatformUserAccountStatus TargetStatus { get; set; } = PlatformUserAccountStatus.Active;

    public PlatformUseCaseScope Scope()
    {
        PlatformInteractiveRequest circuit = new();
        circuit.Capture("203.0.113.7", "tests", "corr");

        PlatformInteractiveRequest request = new();
        request.CopyFrom(circuit, new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(PlatformClaimTypes.UserId, ActorId.ToString()),
                new Claim(PlatformClaimTypes.SessionId, Guid.CreateVersion7().ToString()),
                new Claim(PlatformClaimTypes.Authority, ((int)PlatformUserAuthorityLevel.SuperUser).ToString(System.Globalization.CultureInfo.InvariantCulture))
            },
            "tests")));

        return new PlatformUseCaseScope(new PlatformCurrentUser(request), this);
    }

    public GetPlatformUserActionsUseCase ActionsUseCase()
    {
        return new GetPlatformUserActionsUseCase(this, this, this, TimeProvider.System);
    }

    public static NullLogger<T> Logger<T>()
    {
        return NullLogger<T>.Instance;
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public void DiscardChanges()
    {
        Discards++;
    }

    public Task AuthorizeAsync(PlatformAuthorizationContext context, string requiredPermissionKey, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task AuthorizeAnyAsync(PlatformAuthorizationContext context, IReadOnlyCollection<string> requiredPermissionKeys, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<PagedResult<PlatformUserListItem>> ListAsync(PlatformUserListFilter filter, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(PagedResult<PlatformUserListItem>.Empty(page));
    }

    public Task<PlatformUserDetail?> GetAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformUserDetail?>(null);
    }

    public Task<PagedResult<PlatformSignInItem>> ListSignInsAsync(Guid? platformUserId, DateTimeOffset utcNow, PagedRequest page, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PagedResult<PlatformSignInItem>(Array.Empty<PlatformSignInItem>(), page, 0));
    }

    public Task<PlatformUserActionFacts?> GetActionFactsAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformUserActionFacts?>(
            new PlatformUserActionFacts(platformUserId, TargetStatus, PlatformUserAuthorityLevel.Standard, true, 1));
    }

    public Task<PlatformPermissionSnapshot?> ReadAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult<PlatformPermissionSnapshot?>(
            new PlatformPermissionSnapshot(true, PlatformUserAuthorityLevel.SuperUser, new HashSet<string>(StringComparer.Ordinal)));
    }
}
