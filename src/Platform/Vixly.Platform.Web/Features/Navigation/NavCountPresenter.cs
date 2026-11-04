using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.MyAccount.GetPlatformAttentionCount;

namespace Vixly.Platform.Web.Features.Navigation;

// =========================================================
// FILE: NavCountPresenter.cs
//
// PURPOSE:
// Behaviour of a menu entry's count (owner, 2026-11-04).
//
// RESPONSIBILITIES:
// - Load the count for one page, again at most every 30 seconds
//
// IMPORTANT NOTES:
// - No permission, no number; a failure shows no number rather than an error
// =========================================================
public sealed class NavCountPresenter : PlatformPresenter<NavCountState>
{
    private static readonly TimeSpan Fresh = TimeSpan.FromSeconds(30);

    private readonly GetPlatformAttentionCountUseCase _count;

    private readonly TimeProvider _timeProvider;

    public NavCountPresenter(GetPlatformAttentionCountUseCase count, TimeProvider timeProvider, PlatformUseCaseScope scope, ILogger<NavCountPresenter> logger)
        : base(scope, NavCountState.Initial, logger)
    {
        _count = count ?? throw new ArgumentNullException(nameof(count));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task LoadAsync(PlatformAttention attention)
    {
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (State.LoadedUtc is { } loaded && utcNow - loaded < Fresh)
        {
            return;
        }

        GetPlatformAttentionCountResponse? response = null;
        await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _count.ExecuteAsync(context, new GetPlatformAttentionCountRequest { Attention = attention }, cancellationToken);
            return response.Error;
        });
        SetState(new NavCountState(response?.Count, utcNow));
    }
}
