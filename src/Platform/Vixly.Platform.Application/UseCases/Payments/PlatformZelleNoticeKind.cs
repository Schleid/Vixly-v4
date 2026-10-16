namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleNoticeKind.cs
//
// PURPOSE:
// Which Zelle email the customer gets.
//
// RESPONSIBILITIES:
// - Received when reported; rejected with the reason
//
// IMPORTANT NOTES:
// - A confirmed report sends the usual receipt instead
// =========================================================
public enum PlatformZelleNoticeKind
{
    Received = 1,
    Rejected = 2
}
