using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Vixly.Platform.Application.Identity;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformRevalidatingAuthenticationStateProvider.cs
//
// PURPOSE:
// Re-checks the session of a live Blazor circuit every
// minute, so a locked account or an ended session drops the
// console within a minute even without a page load.
//
// RESPONSIBILITIES:
// - Ask the session validator whether the circuit's session
//   is still valid
//
// IMPORTANT NOTES:
// - Does not keep the session alive: only user activity
//   (requests and the keep-alive call) does, so the 5-minute
//   idle limit still applies to an open but unused tab
// =========================================================
public sealed class PlatformRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PlatformRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        string? sessionId = authenticationState.User.FindFirst(PlatformClaimTypes.SessionId)?.Value;

        if (!Guid.TryParse(sessionId, out Guid id))
        {
            return false;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

        return await scope.ServiceProvider
            .GetRequiredService<PlatformSessionValidator>()
            .IsStillValidAsync(id, cancellationToken);
    }
}
