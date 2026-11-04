using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextErrors.cs
//
// PURPOSE:
// Error codes of text messages to customers by SMS and WhatsApp (Slice 9).
//
// RESPONSIBILITIES:
// - Name each refusal
//
// IMPORTANT NOTES:
// - Codes only; texts in OperationsText (en, fr, es)
// =========================================================
public static class OperationsTextErrors
{
    public static Error KeysInvalid { get; } = Error.Create("ops.texts.keys_invalid");

    public static Error SenderInvalid { get; } = Error.Create("ops.texts.sender_invalid");

    public static Error TemplateInvalid { get; } = Error.Create("ops.texts.template_invalid");

    public static Error NotConnected { get; } = Error.Create("ops.texts.not_connected");

    public static Error ProviderRefused { get; } = Error.Create("ops.texts.provider_refused");

    public static Error ProviderUnavailable { get; } = Error.Create("ops.texts.provider_unavailable");

    public static Error FeatureOff { get; } = Error.Create("ops.texts.feature_off");

    public static Error ChannelUnavailable { get; } = Error.Create("ops.texts.channel_unavailable");

    public static Error ConsentRequired { get; } = Error.Create("ops.texts.consent_required");
}
