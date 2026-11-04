using Vixly.Platform.Application.Jobs;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Jobs.ListPlatformJobRuns;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformJobRunsUseCaseTests.cs
//
// PURPOSE:
// Proves the Jobs page use case (owner, 2026-10-23).
//
// RESPONSIBILITIES:
// - Permission: platform.jobs.view, refused without it
// - Every known job is listed, even one that never ran
// - An unknown job filter is refused; a known one is passed through
//
// IMPORTANT NOTES:
// - Uses the real gate and authorization service over the in-memory store
// =========================================================
public sealed class PlatformJobRunsUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly FakePlatformStore _store = new();

    private readonly FakePlatformJobQueries _queries = new();

    private readonly FixedTimeProvider _clock = new(Now);

    [Fact]
    public async Task Jobs_NeedTheJobsPermission()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            List().ExecuteAsync(User(PlatformUserAuthorityLevel.Standard), new ListPlatformJobRunsRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task EveryJob_IsListed_EvenOneThatNeverRan()
    {
        PlatformJobRunItem failed = new(Guid.NewGuid(), PlatformJobNames.Billing, Now.AddHours(-1), Now.AddHours(-1), 0, 1, "TimeoutException");
        _queries.Latest.Add(new PlatformJobSummaryItem(PlatformJobNames.Billing, failed, Now.AddHours(-2), 1));

        ListPlatformJobRunsResponse response = await List().ExecuteAsync(User(PlatformUserAuthorityLevel.SuperUser), new ListPlatformJobRunsRequest(), CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal(PlatformJobNames.All, response.Jobs.Select(item => item.Job).ToArray());
        Assert.Equal(1, response.Jobs.Single(item => item.Job == PlatformJobNames.Billing).FailedRecently);
        Assert.Null(response.Jobs.Single(item => item.Job == PlatformJobNames.Legal).Latest);
    }

    [Fact]
    public async Task AnUnknownJob_IsRefused_AKnownOneIsPassedThrough()
    {
        PlatformAuthorizationContext context = User(PlatformUserAuthorityLevel.SuperUser);

        ListPlatformJobRunsResponse unknown = await List().ExecuteAsync(context, new ListPlatformJobRunsRequest { Job = "nothing" }, CancellationToken.None);
        ListPlatformJobRunsResponse known = await List().ExecuteAsync(context, new ListPlatformJobRunsRequest { Job = PlatformJobNames.Payments, FailedOnly = true }, CancellationToken.None);

        Assert.Equal(PlatformCommonErrors.RequestInvalid, unknown.Error);
        Assert.True(known.Succeeded);
        Assert.Equal((PlatformJobNames.Payments, true), _queries.LastFilter);
    }

    private ListPlatformJobRunsUseCase List()
    {
        return new ListPlatformJobRunsUseCase(new PlatformAuthorizationGate(new PlatformAuthorizationService(_store, _clock)), _queries, _clock);
    }

    private PlatformAuthorizationContext User(PlatformUserAuthorityLevel level)
    {
        PlatformUser user = PlatformUser.Invite("Jobs", "Reader", EmailAddress.Create($"j{_store.Users.Count}@example.com"), LanguageCode.En, level, null, null, Now);
        user.AcceptInvitation("H:password", Now);
        _store.Users.Add(user);

        return new PlatformAuthorizationContext(user.Id, level);
    }
}
