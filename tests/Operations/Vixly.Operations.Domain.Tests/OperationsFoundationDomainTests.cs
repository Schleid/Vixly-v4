using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Jobs;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Platform;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsFoundationDomainTests.cs
//
// PURPOSE:
// Proves the Slice 0 domain rules.
//
// RESPONSIBILITIES:
// - Tenant entities need a tenant
// - Outbox messages validate and back off
// - Feed positions only move forward
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsFoundationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TenantEntity_RefusesAnEmptyTenant()
    {
        DomainException error = Assert.Throws<DomainException>(() => new ProbeEntity(Guid.Empty));

        Assert.Equal("ops.common.tenant_required", error.Error.Code);
    }

    [Fact]
    public void TenantEntity_KeepsItsTenant()
    {
        Guid tenant = Guid.NewGuid();

        Assert.Equal(tenant, new ProbeEntity(tenant).TenantId);
    }

    [Theory]
    [InlineData("platform.tenant.created.v1")]
    [InlineData("ops.Tenant.created.v1")]
    [InlineData("ops.tenant.created")]
    [InlineData("")]
    public void Outbox_RefusesABadType(string type)
    {
        DomainException error = Assert.Throws<DomainException>(() => OperationsOutboxMessage.Create(type, null, Guid.NewGuid(), "{}", Now));

        Assert.Equal("ops.messaging.type_invalid", error.Error.Code);
    }

    [Fact]
    public void Outbox_NeedsAStreamAndAPayload()
    {
        Assert.Equal("ops.messaging.stream_required", Assert.Throws<DomainException>(() => OperationsOutboxMessage.Create("ops.test.done.v1", null, Guid.Empty, "{}", Now)).Error.Code);
        Assert.Equal("ops.messaging.payload_required", Assert.Throws<DomainException>(() => OperationsOutboxMessage.Create("ops.test.done.v1", null, Guid.NewGuid(), " ", Now)).Error.Code);
    }

    [Fact]
    public void Outbox_BacksOffThenWaitsForAPerson()
    {
        OperationsOutboxMessage message = OperationsOutboxMessage.Create("ops.test.done.v1", Guid.NewGuid(), Guid.NewGuid(), "{}", Now);

        message.MarkFailed("", Now);
        Assert.Equal(Now.AddMinutes(1), message.NextAttemptUtc);
        Assert.Equal("ops.messaging.handler_failed", message.LastErrorCode);

        message.MarkFailed("ops.test.failed", Now);
        Assert.Equal(Now.AddMinutes(2), message.NextAttemptUtc);

        for (int attempt = message.Attempts; attempt < OperationsOutboxMessage.MaxAttempts; attempt++)
        {
            message.MarkFailed("ops.test.failed", Now);
        }

        Assert.True(message.IsSetAside);
    }

    [Fact]
    public void Outbox_Processed_ClearsTheRetry()
    {
        OperationsOutboxMessage message = OperationsOutboxMessage.Create("ops.test.done.v1", null, Guid.NewGuid(), "{}", Now);
        message.MarkFailed("ops.test.failed", Now);

        message.MarkProcessed(Now.AddMinutes(1));

        Assert.Equal(Now.AddMinutes(1), message.ProcessedUtc);
        Assert.Null(message.NextAttemptUtc);
        Assert.Null(message.LastErrorCode);
        Assert.False(message.IsSetAside);
    }

    [Fact]
    public void FeedPosition_OnlyMovesForward()
    {
        OperationsPlatformFeedPosition position = OperationsPlatformFeedPosition.Create(OperationsPlatformFeedPosition.PlatformEvents, Now);

        position.MoveTo(10, Now);
        position.MoveTo(4, Now);

        Assert.Equal(10, position.Position);
    }

    [Fact]
    public void JobRun_RecordsItsResult()
    {
        OperationsJobRun run = OperationsJobRun.Start("ops.test", Now);

        run.Finish(3, -1, "ops.common.platform_unavailable", Now.AddSeconds(5));

        Assert.Equal(3, run.Handled);
        Assert.Equal(0, run.Failed);
        Assert.Equal(Now.AddSeconds(5), run.FinishedUtc);
    }

    private sealed class ProbeEntity : OperationsEntity
    {
        public ProbeEntity(Guid tenantId)
            : base(tenantId, Now)
        {
        }
    }
}
