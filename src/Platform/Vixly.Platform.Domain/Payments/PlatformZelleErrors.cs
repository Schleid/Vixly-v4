using System.Globalization;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleErrors.cs
//
// PURPOSE:
// Error codes for Zelle settings and Zelle payment claims (Slice 5 update).
//
// RESPONSIBILITIES:
// - One error per broken Zelle rule, with the values the text needs
//
// IMPORTANT NOTES:
// - Codes only (L-1); text comes from PlatformText
// =========================================================
public static class PlatformZelleErrors
{
    public static Error NotAvailable { get; } = Error.Create("platform.payments.zelle_not_available");

    public static Error SettingsInvalid { get; } = Error.Create("platform.payments.zelle_settings_invalid");

    public static Error RecipientInvalid { get; } = Error.Create("platform.payments.zelle_recipient_invalid");

    public static Error EmailInvalid { get; } = Error.Create("platform.payments.zelle_email_invalid");

    public static Error TagInvalid { get; } = Error.Create("platform.payments.zelle_tag_invalid");

    public static Error PhoneInvalid { get; } = Error.Create("platform.payments.zelle_phone_invalid");

    public static Error QrCodeInvalid { get; } = Error.Create("platform.payments.zelle_qr_invalid", ("max", PlatformZelleLimits.MaxQrKilobytes.ToString(CultureInfo.InvariantCulture)));

    public static Error AmountInvalid { get; } = Error.Create("platform.payments.zelle_amount_invalid");

    public static Error DateInvalid { get; } = Error.Create("platform.payments.zelle_date_invalid", ("days", PlatformZelleLimits.MaxDaysBack.ToString(CultureInfo.InvariantCulture)));

    public static Error SenderInvalid { get; } = Error.Create("platform.payments.zelle_sender_invalid");

    public static Error ReferenceInvalid { get; } = Error.Create("platform.payments.zelle_reference_invalid");

    public static Error ClaimWaiting { get; } = Error.Create("platform.payments.zelle_claim_waiting");

    public static Error TooManyClaims { get; } = Error.Create("platform.payments.zelle_too_many_claims");

    public static Error ClaimNotFound { get; } = Error.Create("platform.payments.zelle_claim_not_found");

    public static Error ClaimDecided { get; } = Error.Create("platform.payments.zelle_claim_decided");

    public static Error ReasonRequired { get; } = Error.Create("platform.payments.zelle_reason_required");

    public static Error ActorRequired { get; } = Error.Create("platform.payments.zelle_actor_required");
}
