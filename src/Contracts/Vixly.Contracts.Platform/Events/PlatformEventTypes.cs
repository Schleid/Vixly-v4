namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformEventTypes.cs
//
// PURPOSE:
// The public Platform event types other planes may read from the feed (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Type names end with the major version
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public static class PlatformEventTypes
{
    public const string TenantStatusChanged = "platform.tenant.status_changed.v1";

    public const string TenantSlugCorrected = "platform.tenant.slug_corrected.v1";

    public const string SubscriptionStatusChanged = "platform.subscription.status_changed.v1";

    public const string PlanVersionPublished = "platform.plan.version_published.v1";

    public const string DeviceApproved = "platform.device.approved.v1";

    public const string DeviceReleased = "platform.device.released.v1";

    public const string DeviceRevoked = "platform.device.revoked.v1";

    public const string SeatsChanged = "platform.licensing.seats_changed.v1";

    public const string LegalVersionPublished = "platform.legal.version_published.v1";

    public const string TermsAccepted = "platform.legal.terms_accepted.v1";

    // Operations Slice 1: the owner may now set up the account; Operations invites the owner.
    public const string RegistrationAccountSetupReady = "platform.registration.account_setup_ready.v1";

    // Edge reads only what it needs to resolve and redirect portals.
    public static IReadOnlyList<string> ForEdge { get; } = new[] { TenantStatusChanged, TenantSlugCorrected, SubscriptionStatusChanged };

    public static IReadOnlyList<string> All { get; } = new[]
    {
        TenantStatusChanged, TenantSlugCorrected, SubscriptionStatusChanged, PlanVersionPublished,
        DeviceApproved, DeviceReleased, DeviceRevoked, SeatsChanged, LegalVersionPublished, TermsAccepted,
        RegistrationAccountSetupReady
    };
}
