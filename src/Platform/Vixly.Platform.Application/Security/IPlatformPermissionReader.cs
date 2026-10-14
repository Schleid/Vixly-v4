namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: IPlatformPermissionReader.cs
//
// PURPOSE:
// Port that reads a user's current access from persistence.
//
// RESPONSIBILITIES:
// - Return the snapshot used by the authorization service
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure with one query; only role
//   grants in force at utcNow and only active roles count
// - Returns null for an unknown user
// =========================================================
public interface IPlatformPermissionReader
{
    Task<PlatformPermissionSnapshot?> ReadAsync(
        Guid platformUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
