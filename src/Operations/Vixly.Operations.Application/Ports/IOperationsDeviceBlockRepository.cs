using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsDeviceBlockRepository.cs
//
// PURPOSE:
// Devices blocked after a sign-in from another country (owner, R-6).
//
// RESPONSIBILITIES:
// - Add a block; find the open block of a device; list the company's open blocks
//
// IMPORTANT NOTES:
// - Scoped to the tenant by the data layer
// =========================================================
public interface IOperationsDeviceBlockRepository
{
    void Add(OperationsDeviceBlock block);

    Task<OperationsDeviceBlock?> FindOpenAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<OperationsDeviceBlock?> GetAsync(Guid blockId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsDeviceBlock>> ListOpenAsync(CancellationToken cancellationToken);
}
