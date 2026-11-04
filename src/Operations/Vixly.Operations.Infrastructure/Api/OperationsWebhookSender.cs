using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Api;

namespace Vixly.Operations.Infrastructure.Api;

// =========================================================
// FILE: OperationsWebhookSender.cs
//
// PURPOSE:
// Posts signed webhook bodies to the company’s own addresses (Slice 10).
//
// RESPONSIBILITIES:
// - Post JSON with its headers; report the status, or why nothing came back
// - Connect only to public addresses, checked on the address actually connected to (DNS rebinding)
//
// IMPORTANT NOTES:
// - No redirects, no cookies, 10 seconds; the answer’s body is never read beyond its status
// - Never logs the body or the signature
// =========================================================
public sealed class OperationsWebhookSender : IOperationsWebhookSender
{
    public const string HttpClientName = "operations-webhooks";

    private readonly IHttpClientFactory _clients;

    private readonly ILogger<OperationsWebhookSender> _logger;

    public OperationsWebhookSender(IHttpClientFactory clients, ILogger<OperationsWebhookSender> logger)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // The handler of the named client: connects only to public addresses, follows no redirect.
    public static SocketsHttpHandler Handler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectCallback = ConnectPublicAsync
    };

    public async Task<OperationsWebhookResult> PostAsync(string url, IReadOnlyDictionary<string, string> headers, string body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, new Uri(OperationsWebhookEndpoint.CheckUrl(url)))
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            foreach ((string name, string value) in headers)
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }

            using HttpResponseMessage response = await _clients.CreateClient(HttpClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return new OperationsWebhookResult((int)response.StatusCode, response.IsSuccessStatusCode ? null : "status");
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException)
        {
            return new OperationsWebhookResult(null, "address_refused");
        }
        catch (HttpRequestException exception) when (exception.InnerException is NotPublicException)
        {
            return new OperationsWebhookResult(null, "address_refused");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogInformation("A webhook could not be delivered: {Reason}.", exception.HttpRequestError);
            return new OperationsWebhookResult(null, "unreachable");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new OperationsWebhookResult(null, "timeout");
        }
    }

    private static async ValueTask<Stream> ConnectPublicAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        IPAddress[] addresses = IPAddress.TryParse(context.DnsEndPoint.Host, out IPAddress? literal)
            ? new[] { literal }
            : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        IPAddress[] usable = addresses.Where(OperationsWebhookEndpoint.IsPublic).ToArray();

        // Any private answer refuses the host: a name that also points inside is not trusted.
        if (usable.Length == 0 || usable.Length != addresses.Length)
        {
            throw new NotPublicException();
        }

        Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(usable, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private sealed class NotPublicException : Exception
    {
        public NotPublicException()
            : base("The address is not public.")
        {
        }
    }
}
