using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: PlatformPasswordResetRequestRepository.cs
//
// PURPOSE:
// EF implementation of IPlatformPasswordResetRequestRepository.
//
// RESPONSIBILITIES:
// - Count recent requests by email and by client IP; add
//
// IMPORTANT NOTES:
// - Uses the (Email, CreatedUtc) and (IpAddress, CreatedUtc)
//   indexes
// =========================================================
internal sealed class PlatformPasswordResetRequestRepository : IPlatformPasswordResetRequestRepository
{
    private readonly PlatformDbContext _context;

    public PlatformPasswordResetRequestRepository(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> CountSinceAsync(EmailAddress email, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        return _context.PasswordResetRequests.CountAsync(
            request => request.Email == email && request.CreatedUtc >= sinceUtc,
            cancellationToken);
    }

    public Task<int> CountFromIpSinceAsync(string ipAddress, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);

        return _context.PasswordResetRequests.CountAsync(
            request => request.IpAddress == ipAddress && request.CreatedUtc >= sinceUtc,
            cancellationToken);
    }

    public void Add(PlatformPasswordResetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _context.PasswordResetRequests.Add(request);
    }
}
