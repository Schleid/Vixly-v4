using System.Globalization;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformLicensingErrors.cs
//
// PURPOSE:
// Error codes for licensing: seats, devices, proofs, links and releases.
//
// RESPONSIBILITIES:
// - Give each refusal a stable code with en, fr and es text
//
// IMPORTANT NOTES:
// - Codes are platform.licensing.*; never reword one that shipped
// - One answer for every unusable link (no hint whether it existed)
// =========================================================
public static class PlatformLicensingErrors
{
    public static Error NoFreeSeat(int inUse, int seats) => Error.Create("platform.licensing.no_free_seat",
        ("inUse", inUse.ToString(CultureInfo.InvariantCulture)), ("seats", seats.ToString(CultureInfo.InvariantCulture)));

    public static Error TooManyPending { get; } = Error.Create("platform.licensing.too_many_pending", ("max", PlatformLicensingLimits.MaxPending.ToString(CultureInfo.InvariantCulture)));

    public static Error DeviceNotFound { get; } = Error.Create("platform.licensing.device_not_found");

    public static Error NotAllowedInStatus { get; } = Error.Create("platform.licensing.not_allowed_in_status");

    public static Error NameInvalid { get; } = Error.Create("platform.licensing.name_invalid");

    public static Error KindInvalid { get; } = Error.Create("platform.licensing.kind_invalid");

    public static Error PublicKeyInvalid { get; } = Error.Create("platform.licensing.public_key_invalid");

    public static Error LinkInvalid { get; } = Error.Create("platform.licensing.link_invalid");

    public static Error TenantNotActive { get; } = Error.Create("platform.licensing.tenant_not_active");

    public static Error AccessNotFull { get; } = Error.Create("platform.licensing.access_not_full");

    public static Error ReasonRequired { get; } = Error.Create("platform.licensing.reason_required");

    public static Error ActorRequired { get; } = Error.Create("platform.licensing.actor_required");

    public static Error ProofInvalid { get; } = Error.Create("platform.licensing.proof_invalid");

    public static Error ReleaseNotPending { get; } = Error.Create("platform.licensing.release_not_pending");

    public static Error RegistrationNotReady { get; } = Error.Create("platform.licensing.registration_not_ready");

    public static Error SupportReasonRequired { get; } = Error.Create("platform.licensing.support_reason_required");

    public static Error IdentityCheckRequired { get; } = Error.Create("platform.licensing.identity_check_required");

    public static Error ApproverMustDiffer { get; } = Error.Create("platform.licensing.approver_must_differ");

    public static Error SupportNotPending { get; } = Error.Create("platform.licensing.support_not_pending");

    public static Error TooManyLinks { get; } = Error.Create("platform.licensing.too_many_links");
}
