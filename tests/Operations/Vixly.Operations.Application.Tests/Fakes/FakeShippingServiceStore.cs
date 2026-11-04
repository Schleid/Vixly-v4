using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeShippingServiceStore.cs
//
// PURPOSE:
// An in-memory store of shipping-service setups (Slice 1E).
//
// RESPONSIBILITIES:
// - Hold the setups a test adds
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeShippingServiceStore : IOperationsShippingServiceRepository
{
    public List<OperationsShippingServiceSetup> Setups { get; } = new();

    public void Add(OperationsShippingServiceSetup setup) => Setups.Add(setup);

    public Task<IReadOnlyList<OperationsShippingServiceSetup>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsShippingServiceSetup>>(Setups.ToList());
}
