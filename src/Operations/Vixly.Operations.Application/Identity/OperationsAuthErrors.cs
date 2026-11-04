using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsAuthErrors.cs
//
// PURPOSE:
// Error codes of staff sign-in (Slice 1).
//
// RESPONSIBILITIES:
// - Declare each refusal as a code
//
// IMPORTANT NOTES:
// - Sign-in failures never say whether the email exists
// =========================================================
public static class OperationsAuthErrors
{
    public static Error SignInFailed { get; } = Error.Create("ops.auth.sign_in_failed");

    // Owner, 2026-10-21: a device signing in from another country than its own must confirm with a second factor; this account has none.
    // Owner, R-6: the device was used from another country than its own; only a company administrator unblocks it.
    public static Error DeviceBlocked { get; } = Error.Create("ops.auth.device_blocked");

    // Slice 7 step 4 (R-8): partner agents are not part of the company's plan now, or Platform could not say so.
    public static Error PartnerAgentsOff { get; } = Error.Create("ops.auth.partner_agents_off");

    // Owner, 2026-10-29 (S-5): an outside account signs in only during its working hours and until its end date.
    public static Error OutsideHours { get; } = Error.Create("ops.auth.outside_hours");

    public static Error CodeInvalid { get; } = Error.Create("ops.auth.code_invalid");

    // Slice 12A (owner H-8): a badge or PIN works only on a licensed device of the company.
    public static Error QuickDeviceRequired { get; } = Error.Create("ops.auth.quick_device_required");

    // The badge is unknown or revoked, or the PIN is wrong: one answer for both.
    public static Error QuickFailed { get; } = Error.Create("ops.auth.quick_failed");

    // Five wrong PINs: the PIN works again after a full sign-in.
    public static Error PinLocked { get; } = Error.Create("ops.auth.pin_locked");

    // The person must set up their authenticator with a full sign-in first.
    public static Error QuickFullSignInFirst { get; } = Error.Create("ops.auth.quick_full_sign_in_first");

    public static Error PasswordWrong { get; } = Error.Create("ops.auth.password_wrong");

    public static Error SignInExpired { get; } = Error.Create("ops.auth.sign_in_expired");

    public static Error LinkInvalid { get; } = Error.Create("ops.auth.link_invalid");

    public static Error PasswordTooShort { get; } = Error.Create("ops.auth.password_too_short", ("minLength", OperationsPasswordPolicy.MinLength));

    public static Error PasswordTooLong { get; } = Error.Create("ops.auth.password_too_long", ("maxLength", OperationsPasswordPolicy.MaxLength));

    public static Error PasswordTooCommon { get; } = Error.Create("ops.auth.password_too_common");

    public static Error PasswordContainsEmail { get; } = Error.Create("ops.auth.password_contains_email");
}
