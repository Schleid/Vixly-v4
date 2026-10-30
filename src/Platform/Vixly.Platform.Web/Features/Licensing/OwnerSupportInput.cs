namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: OwnerSupportInput.cs
//
// PURPOSE:
// What staff type when they start an owner support action (D-O13).
//
// RESPONSIBILITIES:
// - Carry the reason, the identity check, the device to replace and a decision note
//
// IMPORTANT NOTES:
// - Kept by the presenter while a step-up is asked, then sent once
// =========================================================
public sealed record OwnerSupportInput(string? Reason, string? IdentityCheck, Guid? ReplaceDeviceId, string? Note);
