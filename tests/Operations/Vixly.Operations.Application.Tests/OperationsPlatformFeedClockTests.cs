using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Setup;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsPlatformFeedClockTests.cs
//
// PURPOSE:
// Proves the Platform event feed clock (Slice 0, D-F3).
//
// RESPONSIBILITIES:
// - Forget tenants whose status, address or subscription changed
// - Keep the position and record each run
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsPlatformFeedClockTests
{
    private readonly FakePlatformClient _platform = new();

    private readonly FakeStores _stores = new();

    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task ForgetsChangedTenants_AndMovesOn()
    {
        Guid suspended = Guid.NewGuid();
        Guid renamed = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        _platform.Pages.Enqueue(new OperationsPlatformEventPage(
            new[]
            {
                new OperationsPlatformEvent(1, OperationsPlatformEventTypes.TenantStatusChanged, suspended),
                new OperationsPlatformEvent(2, "platform.invoice.issued.v1", other)
            },
            2,
            true));
        _platform.Pages.Enqueue(new OperationsPlatformEventPage(
            new[] { new OperationsPlatformEvent(3, OperationsPlatformEventTypes.TenantSlugCorrected, renamed) },
            3,
            false));
        FakeTenantDirectory directory = new(_platform);

        int handled = await Clock(directory).RunAsync(CancellationToken.None);

        Assert.Equal(3, handled);
        Assert.Equal(new[] { suspended, renamed }, directory.Forgotten);
        Assert.Equal(new long[] { 0, 2 }, _platform.ReadFrom);
        Assert.Equal(3, Assert.Single(_stores.Positions).Position);
        Assert.Equal(3, Assert.Single(_stores.Runs).Handled);
        Assert.Equal(1, _stores.Saves);
    }

    [Fact]
    public async Task StartsFromTheSavedPosition()
    {
        await Clock(new FakeTenantDirectory(_platform)).RunAsync(CancellationToken.None);
        _platform.Pages.Enqueue(new OperationsPlatformEventPage(new[] { new OperationsPlatformEvent(8, "platform.x.v1", null) }, 8, false));
        await Clock(new FakeTenantDirectory(_platform)).RunAsync(CancellationToken.None);

        _platform.ReadFrom.Clear();
        await Clock(new FakeTenantDirectory(_platform)).RunAsync(CancellationToken.None);

        Assert.Equal(new long[] { 8 }, _platform.ReadFrom);
    }

    [Fact]
    public async Task PlatformDown_IsRecorded_NotThrown()
    {
        _platform.Down = true;

        await Clock(new FakeTenantDirectory(_platform)).RunAsync(CancellationToken.None);

        Assert.Equal("ops.common.platform_unavailable", Assert.Single(_stores.Runs).ErrorCode);
        Assert.Equal(1, _stores.Saves);
    }

    [Fact]
    public async Task AccountReady_AsksToInviteTheOwner_InTheTenant()
    {
        Guid tenant = Guid.NewGuid();
        _platform.Pages.Enqueue(new OperationsPlatformEventPage(
            new[] { new OperationsPlatformEvent(1, OperationsPlatformEventTypes.RegistrationAccountSetupReady, tenant) }, 1, false));

        await Clock(new FakeTenantDirectory(_platform)).RunAsync(CancellationToken.None);

        OperationsOutboxMessage message = Assert.Single(_stores.Outbox);
        Assert.Equal(OperationsSetupMessages.OwnerInvitationRequested, message.Type);
        Assert.Equal(tenant, message.TenantId);
        Assert.Equal(tenant, OperationsOutboxWriter.Read<OperationsOwnerInvitationRequested>(message).TenantId);
    }

    [Fact]
    public async Task RunsInTheSystemScope()
    {
        FakeTenantScope scope = new();

        await new OperationsPlatformFeedClock(_platform, new FakeTenantDirectory(_platform), _stores, _stores, scope, _stores, new OperationsOutboxWriter(_stores, _time), _time).RunAsync(CancellationToken.None);

        Assert.True(scope.IsSystem);
    }

    private OperationsPlatformFeedClock Clock(FakeTenantDirectory directory)
    {
        return new OperationsPlatformFeedClock(_platform, directory, _stores, _stores, new FakeTenantScope(), _stores, new OperationsOutboxWriter(_stores, _time), _time);
    }
}
