using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformOwnerSupportItem.cs
//
// PURPOSE:
// An open owner support request on the tenant page (D-O13).
//
// RESPONSIBILITIES:
// - Carry what the console lists: kind, status, who asked, when and why
//
// IMPORTANT NOTES:
// - Pending and approved-not-completed only
// =========================================================
public sealed record PlatformOwnerSupportItem(Guid RequestId, PlatformOwnerSupportKind Kind, PlatformOwnerSupportStatus Status, Guid RequestedBy, string? RequestedByName, DateTimeOffset RequestedUtc, string Reason, string IdentityCheck);
