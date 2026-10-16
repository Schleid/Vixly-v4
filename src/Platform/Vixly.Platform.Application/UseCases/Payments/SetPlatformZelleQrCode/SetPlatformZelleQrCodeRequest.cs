namespace Vixly.Platform.Application.UseCases.Payments.SetPlatformZelleQrCode;

// =========================================================
// FILE: SetPlatformZelleQrCodeRequest.cs
//
// PURPOSE:
// Replaces or removes the Zelle QR code image shown at checkout.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformZelleQrCodeRequest
{
    public byte[]? Content { get; init; }

    public string? ContentType { get; init; }
}
