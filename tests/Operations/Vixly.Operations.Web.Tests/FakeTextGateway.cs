using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeTextGateway.cs
//
// PURPOSE:
// Stands in for Twilio (Slice 9): accounts, SMS and WhatsApp messages kept in memory.
//
// RESPONSIBILITIES:
// - Accept the account a test sets; keep what was sent; answer STOP when a test asks
//
// IMPORTANT NOTES:
// - Tests only; never reaches the network
// =========================================================
internal sealed class FakeTextGateway : IOperationsTextGateway
{
    public string AcceptedToken { get; set; } = "0123456789abcdef0123456789abcdef";

    public List<(string From, string To, string Body)> Sms { get; } = new();

    public List<(string From, string To, string Template, IReadOnlyList<string> Variables)> WhatsApp { get; } = new();

    // The next message is refused with this Twilio code (21610: the customer replied STOP).
    public string? RefuseNextWith { get; set; }

    public Task<string> CheckAsync(string accountSid, string authToken, CancellationToken cancellationToken) =>
        authToken == AcceptedToken ? Task.FromResult("Text Co Twilio") : throw new OperationsTextRefusedException("Twilio refused the request: 20003.") { Code = "20003" };

    // Where each accepted message asked Twilio to report.
    public List<string?> Callbacks { get; } = new();

    // Twilio's own signature rule, as the real gateway checks it.
    public bool SignatureValid(string authToken, string url, IReadOnlyList<KeyValuePair<string, string>> form, string? signature) =>
        new Vixly.Operations.Infrastructure.Texts.OperationsTwilioGateway(new NoClients(), Microsoft.Extensions.Logging.Abstractions.NullLogger<Vixly.Operations.Infrastructure.Texts.OperationsTwilioGateway>.Instance)
            .SignatureValid(authToken, url, form, signature);

    public Task<string> SendSmsAsync(string accountSid, string authToken, string from, string to, string body, string? statusCallback, CancellationToken cancellationToken)
    {
        Refuse();
        Callbacks.Add(statusCallback);
        Sms.Add((from, to, body));
        return Task.FromResult("SM" + Sms.Count.ToString("D32", System.Globalization.CultureInfo.InvariantCulture));
    }

    public Task<string> SendWhatsAppAsync(string accountSid, string authToken, string from, string to, string contentSid, IReadOnlyList<string> variables,
        string? statusCallback, CancellationToken cancellationToken)
    {
        Refuse();
        Callbacks.Add(statusCallback);
        WhatsApp.Add((from, to, contentSid, variables));
        return Task.FromResult("MM" + WhatsApp.Count.ToString("D32", System.Globalization.CultureInfo.InvariantCulture));
    }

    private sealed class NoClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No calls to Twilio in tests.");
    }

    private void Refuse()
    {
        if (RefuseNextWith is { } code)
        {
            RefuseNextWith = null;
            throw new OperationsTextRefusedException("Twilio refused the request: " + code + ".") { Code = code };
        }
    }
}
