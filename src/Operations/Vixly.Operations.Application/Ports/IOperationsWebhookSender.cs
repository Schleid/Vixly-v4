namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsWebhookSender.cs
//
// PURPOSE:
// Posts one signed webhook body to the company’s own address (Slice 10).
//
// RESPONSIBILITIES:
// - Post the body with its headers; say what the receiver answered, or why nothing came back
//
// IMPORTANT NOTES:
// - Never follows a redirect; connects only to public addresses (SSRF); 10 seconds at most; never throws for the receiver’s failures
// =========================================================
public interface IOperationsWebhookSender
{
    Task<OperationsWebhookResult> PostAsync(string url, IReadOnlyDictionary<string, string> headers, string body, CancellationToken cancellationToken);
}
