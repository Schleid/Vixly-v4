using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeDeviceBlocks.cs
//
// PURPOSE:
// In-memory devices blocked after a sign-in from another country (owner, R-6).
//
// RESPONSIBILITIES:
// - Keep the blocks a test adds
//
// IMPORTANT NOTES:
// - No tenant filter: one tenant per test
// =========================================================
internal sealed class FakeDeviceBlocks : IOperationsDeviceBlockRepository
{
    public List<OperationsDeviceBlock> Blocks { get; } = new();

    public void Add(OperationsDeviceBlock block) => Blocks.Add(block);

    public Task<OperationsDeviceBlock?> FindOpenAsync(Guid deviceId, CancellationToken cancellationToken) =>
        Task.FromResult(Blocks.FirstOrDefault(block => block.DeviceId == deviceId && block.IsOpen));

    public Task<OperationsDeviceBlock?> GetAsync(Guid blockId, CancellationToken cancellationToken) =>
        Task.FromResult(Blocks.FirstOrDefault(block => block.Id == blockId));

    public Task<IReadOnlyList<OperationsDeviceBlock>> ListOpenAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsDeviceBlock>>(Blocks.Where(block => block.IsOpen).ToList());
}
