using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsQuickSignInRepository.cs
//
// PURPOSE:
// Badges, PINs and the people of each licensed device (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Find a badge by its code's hash, a person's badge in use and their PIN
// - Find and list the people who signed in on a device
//
// IMPORTANT NOTES:
// - Scoped to the tenant by the data layer
// =========================================================
public interface IOperationsQuickSignInRepository
{
    void Add(OperationsStaffBadge badge);

    void Add(OperationsStaffPin pin);

    void Add(OperationsDevicePerson person);

    Task<OperationsStaffBadge?> FindBadgeAsync(string codeHash, CancellationToken cancellationToken);

    Task<OperationsStaffBadge?> FindActiveBadgeAsync(Guid userId, CancellationToken cancellationToken);

    Task<OperationsStaffPin?> FindPinAsync(Guid userId, CancellationToken cancellationToken);

    Task<OperationsDevicePerson?> FindDevicePersonAsync(Guid deviceId, Guid userId, CancellationToken cancellationToken);

    // The people who signed in on the device since the date, latest first.
    Task<IReadOnlyList<OperationsDevicePerson>> ListDevicePeopleAsync(Guid deviceId, DateTimeOffset sinceUtc, CancellationToken cancellationToken);
}
