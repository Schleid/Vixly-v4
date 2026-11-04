namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsSecretPurposes.cs
//
// PURPOSE:
// The purposes secrets are kept under, so one kind can never be read as another (Slice 9 review).
//
// RESPONSIBILITIES:
// - Name each purpose once
//
// IMPORTANT NOTES:
// - Never change a value: what was kept under it could no longer be read
// =========================================================
public static class OperationsSecretPurposes
{
    public const string TwilioToken = "Vixly.Operations.TwilioToken.v1";

    public const string WebhookSecret = "Vixly.Operations.WebhookSecret.v1";

    // The company's card provider keys (2026-11-03): each also bound to the company, so a value copied to another company's row cannot be read.
    public const string CardSecretKey = "Vixly.Operations.CardSecretKey.v1";

    public const string CardWebhookSecret = "Vixly.Operations.CardWebhookSecret.v1";

    public static string ForTenant(string purpose, Guid tenantId)
    {
        System.ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return purpose + ":" + tenantId.ToString("N", System.Globalization.CultureInfo.InvariantCulture);
    }
}
