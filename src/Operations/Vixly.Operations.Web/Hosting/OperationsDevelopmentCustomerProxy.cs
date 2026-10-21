using System.Net.Security;
using System.Net;
using Microsoft.Extensions.Options;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDevelopmentCustomerProxy.cs
//
// PURPOSE:
// Passes /customer/** to Customer.Web on a developer computer (Slice 2, design section 4).
//
// RESPONSIBILITIES:
// - Forward the request with its Host header, so Customer.Web finds the same tenant
// - Return the answer as it is: status, headers, cookies and body
// - A request the browser abandoned (the first click of a double click) ends quietly; a portal that is not running gives a page saying so
//
// IMPORTANT NOTES:
// - Development only, and only when OperationsDevelopment:CustomerPortalUrl is set; Edge does this in production
// - Never follows redirects and never keeps cookies: the browser does
// - Accepts the developer certificate of a loopback address only
// =========================================================
public static class OperationsDevelopmentCustomerProxy
{
    public const string HttpClientName = "ops-dev-customer-proxy";

    private static readonly HashSet<string> HopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Connection", "Transfer-Encoding", "Upgrade", "TE", "Trailer", "Host"
    };

    public static IServiceCollection AddOperationsDevelopmentCustomerProxy(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<OperationsDevelopmentOptions>().Bind(configuration.GetSection(OperationsDevelopmentOptions.SectionName));
        services.AddHttpClient(HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                Uri target = new(provider.GetRequiredService<IOptions<OperationsDevelopmentOptions>>().Value.CustomerPortalUrl);
                return new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        // The ASP.NET Core developer certificate, on this computer only.
                        RemoteCertificateValidationCallback = (_, _, _, errors) => errors == SslPolicyErrors.None || target.IsLoopback
                    }
                };
            });
        return services;
    }

    public static WebApplication UseOperationsDevelopmentCustomerProxy(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment()
            || !Uri.TryCreate(app.Configuration[OperationsDevelopmentOptions.SectionName + ":CustomerPortalUrl"], UriKind.Absolute, out _))
        {
            return app;
        }

        app.UseWhen(http => http.Request.Path.StartsWithSegments("/customer", StringComparison.OrdinalIgnoreCase), branch => branch.Run(ForwardAsync));
        return app;
    }

    private static async Task ForwardAsync(HttpContext http)
    {
        Uri target = new(http.RequestServices.GetRequiredService<IOptions<OperationsDevelopmentOptions>>().Value.CustomerPortalUrl);
        using HttpRequestMessage request = new(new HttpMethod(http.Request.Method), new Uri(target, http.Request.Path + http.Request.QueryString));

        if (http.Request.ContentLength > 0 || http.Request.Headers.TransferEncoding.Count > 0)
        {
            request.Content = new StreamContent(http.Request.Body);
        }

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in http.Request.Headers.Where(header => !HopHeaders.Contains(header.Key)))
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // The tenant comes from the host name, as behind Edge.
        request.Headers.Host = http.Request.Host.Value;

        try
        {
            using HttpResponseMessage response = await http.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, http.RequestAborted);
            http.Response.StatusCode = (int)response.StatusCode;

            foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers.Concat(response.Content.Headers).Where(header => !HopHeaders.Contains(header.Key)))
            {
                http.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(http.Response.Body, http.RequestAborted);
        }
        catch (Exception exception) when (http.RequestAborted.IsCancellationRequested && exception is OperationCanceledException or IOException or HttpRequestException)
        {
            // The browser left (the first click of a double click, a closed tab): no one is waiting for an answer (ADR-0019).
            if (!http.Response.HasStarted)
            {
                http.Response.StatusCode = VixlyClientDisconnects.ClientClosedRequest;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or IOException)
        {
            // Customer.Web is not running on this computer, or did not answer in time.
            http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(OperationsDevelopmentCustomerProxy).FullName!)
                .LogWarning("The customer portal at {Target} did not answer: {Reason}", target, exception.Message);

            if (http.Response.HasStarted)
            {
                http.Abort();
                return;
            }

            http.Response.StatusCode = StatusCodes.Status502BadGateway;
            http.Response.ContentType = "text/plain; charset=utf-8";
            await http.Response.WriteAsync("The customer portal (Customer.Web) did not answer at " + target + ". Start it on this computer, then reload this page.");
        }
    }
}
