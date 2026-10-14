namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserPasskeyItem.cs
//
// PURPOSE:
// One passkey of a staff user, as screens show it.
//
// RESPONSIBILITIES:
// - Carry the name and the dates
//
// IMPORTANT NOTES:
// - Never carries the credential id or the public key
// =========================================================
public sealed record PlatformUserPasskeyItem(Guid PasskeyId, string Name, DateTimeOffset CreatedUtc, DateTimeOffset? LastUsedUtc);
