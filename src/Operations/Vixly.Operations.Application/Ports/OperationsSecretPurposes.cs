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
}
