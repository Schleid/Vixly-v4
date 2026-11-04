namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPlatformEventTypes.cs
//
// PURPOSE:
// The Platform event types Operations acts on (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Name the event types read from the feed
//
// IMPORTANT NOTES:
// - Values equal Contracts.Platform PlatformEventTypes
// =========================================================
public static class OperationsPlatformEventTypes
{
    public const string TenantStatusChanged = "platform.tenant.status_changed.v1";

    public const string TenantSlugCorrected = "platform.tenant.slug_corrected.v1";

    public const string SubscriptionStatusChanged = "platform.subscription.status_changed.v1";

    public const string RegistrationAccountSetupReady = "platform.registration.account_setup_ready.v1";
}
