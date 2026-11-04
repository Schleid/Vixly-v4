using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsQuickSignInRepository.cs
//
// PURPOSE:
// Badges, PINs and the people of each licensed device (Slice 12A).
//
// RESPONSIBILITIES:
// - Implement IOperationsQuickSignInRepository with EF Core
//
// IMPORTANT NOTES:
// - Tenant scoping comes from the context's query filters; a badge is found by its code's hash only within the tenant
// =========================================================
internal sealed class OperationsQuickSignInRepository : IOperationsQuickSignInRepository
{
    private readonly OperationsDbContext _context;

    public OperationsQuickSignInRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsStaffBadge badge) => _context.StaffBadges.Add(badge ?? throw new ArgumentNullException(nameof(badge)));

    public void Add(OperationsStaffPin pin) => _context.StaffPins.Add(pin ?? throw new ArgumentNullException(nameof(pin)));

    public void Add(OperationsDevicePerson person) => _context.DevicePeople.Add(person ?? throw new ArgumentNullException(nameof(person)));

    public Task<OperationsStaffBadge?> FindBadgeAsync(string codeHash, CancellationToken cancellationToken) =>
        _context.StaffBadges.FirstOrDefaultAsync(item => item.CodeHash == codeHash, cancellationToken);

    public Task<OperationsStaffBadge?> FindActiveBadgeAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.StaffBadges.FirstOrDefaultAsync(item => item.UserId == userId && item.RevokedUtc == null, cancellationToken);

    public Task<OperationsStaffPin?> FindPinAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.StaffPins.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

    public Task<OperationsDevicePerson?> FindDevicePersonAsync(Guid deviceId, Guid userId, CancellationToken cancellationToken) =>
        _context.DevicePeople.FirstOrDefaultAsync(item => item.DeviceId == deviceId && item.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<OperationsDevicePerson>> ListDevicePeopleAsync(Guid deviceId, DateTimeOffset sinceUtc, CancellationToken cancellationToken) =>
        await _context.DevicePeople.AsNoTracking()
            .Where(item => item.DeviceId == deviceId && item.LastSignInUtc >= sinceUtc)
            .OrderByDescending(item => item.LastSignInUtc)
            .Take(OperationsQuickSignIn.DevicePeopleMax)
            .ToListAsync(cancellationToken);
}
