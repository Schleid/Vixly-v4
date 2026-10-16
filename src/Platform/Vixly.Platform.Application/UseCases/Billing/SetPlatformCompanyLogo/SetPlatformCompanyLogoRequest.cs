namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformCompanyLogo;

// =========================================================
// FILE: SetPlatformCompanyLogoRequest.cs
//
// PURPOSE:
// Sets or removes the logo printed on invoices (PNG or JPEG, D-B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformCompanyLogoRequest
{
    public byte[]? Content { get; init; }

    public string? ContentType { get; init; }
}
