using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Messaging;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsOutboxDispatcherTests.cs
//
// PURPOSE:
// Proves outbox delivery (Slice 0, D-F6).
//
// RESPONSIBILITIES:
// - Enter the tenant of each message
// - Back off and report failures
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsOutboxDispatcherTests
{
    private const string Type = "ops.test.done.v1";

    private readonly FakeStores _stores = new();

    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task TenantMessage_RunsInItsTenant()
    {
        Guid tenant = Guid.NewGuid();
        Guid id = Write(tenant);
        ProbeHandler handler = new(_ => Task.CompletedTask);
        FakeTenantScope scope = new();

        bool delivered = await Dispatcher(scope, handler).HandleAsync(id, null, CancellationToken.None);

        Assert.True(delivered);
        Assert.Equal(tenant, scope.TenantId);
        Assert.Equal(tenant, handler.SeenTenant);
        Assert.NotNull(_stores.Outbox.Single().ProcessedUtc);
    }

    [Fact]
    public async Task SystemMessage_RunsInTheSystemScope()
    {
        Guid id = Write(null);
        FakeTenantScope scope = new();

        await Dispatcher(scope, new ProbeHandler(_ => Task.CompletedTask)).HandleAsync(id, null, CancellationToken.None);

        Assert.True(scope.IsSystem);
    }

    [Fact]
    public async Task DomainFailure_KeepsItsCode()
    {
        Guid id = Write(Guid.NewGuid());
        List<OperationsOutboxFailure> failures = new();

        bool delivered = await Dispatcher(new FakeTenantScope(), new ProbeHandler(_ => throw new DomainException(OperationsCommonErrors.NotFound)))
            .HandleAsync(id, failures.Add, CancellationToken.None);

        Assert.False(delivered);
        Assert.Equal("ops.common.not_found", Assert.Single(failures).ErrorCode);
        Assert.Equal(1, _stores.Outbox.Single().Attempts);
        Assert.Equal(1, _stores.Discards);
        Assert.Equal(1, _stores.Saves);
    }

    [Fact]
    public async Task OtherFailure_IsAHandlerFailure()
    {
        Guid id = Write(Guid.NewGuid());
        List<OperationsOutboxFailure> failures = new();

        await Dispatcher(new FakeTenantScope(), new ProbeHandler(_ => throw new InvalidOperationException("boom")))
            .HandleAsync(id, failures.Add, CancellationToken.None);

        Assert.Equal("ops.messaging.handler_failed", Assert.Single(failures).ErrorCode);
        Assert.Equal(_time.UtcNow.AddMinutes(1), _stores.Outbox.Single().NextAttemptUtc);
    }

    [Fact]
    public async Task ProcessedMessage_IsNotHandledTwice()
    {
        Guid id = Write(null);
        int calls = 0;
        ProbeHandler handler = new(_ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        await Dispatcher(new FakeTenantScope(), handler).HandleAsync(id, null, CancellationToken.None);
        bool again = await Dispatcher(new FakeTenantScope(), handler).HandleAsync(id, null, CancellationToken.None);

        Assert.False(again);
        Assert.Equal(1, calls);
    }

    private Guid Write(Guid? tenant)
    {
        new OperationsOutboxWriter(_stores, _time).Write(Type, tenant, Guid.NewGuid(), new { Value = 1 });
        return _stores.Outbox[^1].Id;
    }

    private OperationsOutboxDispatcher Dispatcher(FakeTenantScope scope, ProbeHandler handler)
    {
        handler.Scope = scope;
        return new OperationsOutboxDispatcher(_stores, new[] { handler }, scope, _stores, _time);
    }

    private sealed class ProbeHandler : IOperationsOutboxHandler
    {
        private readonly Func<OperationsOutboxMessage, Task> _work;

        public ProbeHandler(Func<OperationsOutboxMessage, Task> work)
        {
            _work = work;
        }

        public string MessageType => Type;

        public FakeTenantScope? Scope { get; set; }

        public Guid? SeenTenant { get; private set; }

        public Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
        {
            SeenTenant = Scope?.TenantId;
            return _work(message);
        }
    }
}
