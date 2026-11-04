namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsZelleQr;

// =========================================================
// FILE: GetMyOperationsZelleQrRequest.cs
//
// PURPOSE:
// The company's Zelle QR picture for a signed-in customer paying on the portal (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the session
//
// IMPORTANT NOTES:
// - Validated by the use case
// =========================================================
public sealed class GetMyOperationsZelleQrRequest
{
    public string SessionToken { get; init; } = string.Empty;
}
