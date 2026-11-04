namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformCheckoutSettings;

// =========================================================
// FILE: UpdatePlatformCheckoutSettingsRequest.cs
//
// PURPOSE:
// Changes the checkout page look and the bank statement wording.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain; empty values clear a setting
// =========================================================
public sealed class UpdatePlatformCheckoutSettingsRequest
{
    public string? DisplayName { get; init; }

    public string? LogoUrl { get; init; }

    public string? IconUrl { get; init; }

    public string? BackgroundColor { get; init; }

    public string? ButtonColor { get; init; }

    public string? StatementPrefix { get; init; }

    public string? StatementSuffix { get; init; }
}
