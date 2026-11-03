using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformHelpTestPermissions.cs
//
// PURPOSE:
// Supplies mutable fresh permission snapshots for help tests.
//
// RESPONSIBILITIES:
// - Model revocation, inactive accounts and reader failures.
//
// IMPORTANT NOTES:
// - Uses the same port as the real access use case.
// =========================================================
internal sealed class PlatformHelpTestPermissions : IPlatformPermissionReader
{
    public HashSet<string> Codes { get; set; } = new(StringComparer.Ordinal);
    public bool SuperUser { get; set; }
    public bool Active { get; set; } = true;
    public bool Fails { get; set; }
    public Func<Task<PlatformPermissionSnapshot?>>? ReadOverride { get; set; }

    public Task<PlatformPermissionSnapshot?> ReadAsync(Guid platformUserId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if (ReadOverride is not null) return ReadOverride();
        if (Fails) throw new InvalidOperationException("Synthetic permission-store failure.");
        return Task.FromResult<PlatformPermissionSnapshot?>(new(Active,
            SuperUser ? PlatformUserAuthorityLevel.SuperUser : PlatformUserAuthorityLevel.Standard, Codes));
    }
}
