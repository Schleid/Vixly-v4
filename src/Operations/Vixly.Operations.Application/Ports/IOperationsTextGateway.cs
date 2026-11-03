namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTextGateway.cs
//
// PURPOSE:
// Sends SMS and WhatsApp messages through the company's own Twilio account (Slice 9).
//
// RESPONSIBILITIES:
// - Check an account; send an SMS; send a WhatsApp template
//
// IMPORTANT NOTES:
// - A refusal says Twilio's code only (OperationsTextRefusedException); an outage OperationsTextUnavailableException
// =========================================================
public interface IOperationsTextGateway
{
    // The account's friendly name.
    Task<string> CheckAsync(string accountSid, string authToken, CancellationToken cancellationToken);

    // Twilio's message id (SM...). From: a number or a messaging service (MG...).
    // statusCallback: where Twilio reports what became of the message (none: not asked).
    Task<string> SendSmsAsync(string accountSid, string authToken, string from, string to, string body, string? statusCallback, CancellationToken cancellationToken);

    Task<string> SendWhatsAppAsync(string accountSid, string authToken, string from, string to, string contentSid, IReadOnlyList<string> variables, string? statusCallback,
        CancellationToken cancellationToken);

    // Twilio's X-Twilio-Signature over the address and the posted fields, with the account's token.
    bool SignatureValid(string authToken, string url, IReadOnlyList<KeyValuePair<string, string>> form, string? signature);
}
