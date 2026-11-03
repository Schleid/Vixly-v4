namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformCheckoutSettingsItem.cs
//
// PURPOSE:
// The checkout page look and the bank statement wording as shown on Payments settings.
//
// RESPONSIBILITIES:
// - Carry what the console shows and edits
//
// IMPORTANT NOTES:
// - Plain record; empty fields mean the provider dashboard's own branding
// - StatementWording is how a card charge reads, e.g. "TECHCRAFT* VIXLY"; null when prefix or suffix is missing
// =========================================================
public sealed record PlatformCheckoutSettingsItem(
    string? DisplayName,
    string? LogoUrl,
    string? IconUrl,
    string? BackgroundColor,
    string? ButtonColor,
    string? StatementPrefix,
    string? StatementSuffix,
    string? StatementWording);
