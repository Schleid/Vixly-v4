namespace Vixly.Operations.Application.UseCases.Texts.ConnectOperationsTextProvider;

// =========================================================
// FILE: ConnectOperationsTextProviderRequest.cs
//
// PURPOSE:
// Connects the company’s own Twilio account, or changes its senders (Slice 9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConnectOperationsTextProviderRequest
{
    public string? AccountSid { get; init; }

    // Empty: keep the token already kept (only the senders change).
    public string? AuthToken { get; init; }

    public string? SmsFrom { get; init; }

    public string? WhatsAppFrom { get; init; }

    // Where Twilio reports what became of each message: the company's own address (set by the web layer).
    public string? CallbackUrl { get; init; }
}
