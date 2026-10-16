namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformServiceToken.cs
//
// PURPOSE:
// A checked service token: the calling plane and, for tenant calls, the tenant user (Slice 9, D-K1, D-K8).
//
// RESPONSIBILITIES:
// - Carry what the token proved
//
// IMPORTANT NOTES:
// - Built only by PlatformServiceTokenValidator
// =========================================================
public sealed record PlatformServiceToken(
    string Caller,
    IReadOnlyCollection<string> Scopes,
    Guid? TenantId,
    string? Subject,
    string? Name,
    string? Role,
    bool StepUp,
    string? ClientIp,
    string? UserAgent);
