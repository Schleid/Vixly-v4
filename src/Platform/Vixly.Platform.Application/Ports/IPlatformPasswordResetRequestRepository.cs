using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPasswordResetRequestRepository.cs
//
// PURPOSE:
// The reset-request log used for throttling.
//
// RESPONSIBILITIES:
// - Count recent requests for an email and for a client IP
// - Add a request
//
// IMPORTANT NOTES:
// - Both counts run on indexed columns
// =========================================================
public interface IPlatformPasswordResetRequestRepository
{
    Task<int> CountSinceAsync(EmailAddress email, DateTimeOffset sinceUtc, CancellationToken cancellationToken);

    Task<int> CountFromIpSinceAsync(string ipAddress, DateTimeOffset sinceUtc, CancellationToken cancellationToken);

    void Add(PlatformPasswordResetRequest request);
}
