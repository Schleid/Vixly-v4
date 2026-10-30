using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesLeadErrors.cs
//
// PURPOSE:
// Errors of contact requests (Stage 2).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; text lives in PlatformText (L-1)
// =========================================================
public static class PlatformSalesLeadErrors
{
    public static Error CompanyRequired { get; } = Error.Create("platform.leads.company_required");

    public static Error NameRequired { get; } = Error.Create("platform.leads.name_required");

    public static Error EmailInvalid { get; } = Error.Create("platform.leads.email_invalid");

    public static Error PhoneInvalid { get; } = Error.Create("platform.leads.phone_invalid");

    public static Error CountryInvalid { get; } = Error.Create("platform.leads.country_invalid");

    public static Error KindInvalid { get; } = Error.Create("platform.leads.kind_invalid");

    public static Error MessageTooLong { get; } = Error.Create("platform.leads.message_too_long", ("max", PlatformSalesLeadLimits.MessageMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error NoteTooLong { get; } = Error.Create("platform.leads.note_too_long", ("max", PlatformSalesLeadLimits.NoteMaxLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    public static Error StatusInvalid { get; } = Error.Create("platform.leads.status_invalid");

    public static Error LeadNotFound { get; } = Error.Create("platform.leads.lead_not_found");

    public static Error BotCheckFailed { get; } = Error.Create("platform.leads.bot_check_failed");
}
