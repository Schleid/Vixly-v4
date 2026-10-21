using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Contracts.Operations.Common;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Packages;

namespace Vixly.Customer.Web.Operations;

// =========================================================
// FILE: CustomerOperationsApi.cs
//
// PURPOSE:
// Calls Operations.Api for Customer.Web (Stage 3, Slice 0, D-F4).
//
// RESPONSIBILITIES:
// - Ask which tenant a host name belongs to (cached 60 seconds)
//
// IMPORTANT NOTES:
// - Every call carries a fresh signed token
// - Operations unreachable returns null: the page says "try again", never "not found"
// =========================================================
public sealed class CustomerOperationsApi
{
    public const string HttpClientName = "customer-operations";

    public static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(60);

    private readonly IHttpClientFactory _http;

    private readonly CustomerCallerSigner _signer;

    private readonly IMemoryCache _cache;

    private readonly ILogger<CustomerOperationsApi> _logger;

    public CustomerOperationsApi(IHttpClientFactory http, CustomerCallerSigner signer, IMemoryCache cache, ILogger<CustomerOperationsApi> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // =====================================================
    // CUSTOMER PORTAL CALLS (Slice 2)
    // =====================================================
    public async Task<CustomerPortalBrandDto?> GetBrandAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        string key = "customer.brand:" + tenantId.ToString("N");

        if (_cache.TryGetValue(key, out CustomerPortalBrandDto? cached) && cached is not null)
        {
            return cached;
        }

        CustomerCall<CustomerPortalBrandDto> call = await SendAsync<CustomerPortalBrandDto>(HttpMethod.Get, "internal/v1/customers/brand", tenantId, null, null, null, cancellationToken);

        if (call.Value is { DisplayName.Length: > 0 } brand)
        {
            _cache.Set(key, brand, CacheTime);
            return brand;
        }

        return null;
    }

    public Task<CustomerCall<object>> SignUpAsync(Guid tenantId, CustomerSignUpBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/sign-up", tenantId, null, body, http, cancellationToken);

    public Task<CustomerCall<CustomerSessionDto>> VerifyAsync(Guid tenantId, string token, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerSessionDto>(HttpMethod.Post, "internal/v1/customers/verify", tenantId, null, new CustomerTokenBody(token), http, cancellationToken);

    public Task<CustomerCall<object>> ResendVerificationAsync(Guid tenantId, CustomerEmailBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/verification", tenantId, null, body, http, cancellationToken);

    public Task<CustomerCall<CustomerSessionDto>> SignInAsync(Guid tenantId, CustomerSignInBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerSessionDto>(HttpMethod.Post, "internal/v1/customers/sessions", tenantId, null, body, http, cancellationToken);

    public Task<CustomerCall<object>> SignOutAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, "internal/v1/customers/sessions/current", tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<object>> RequestPasswordResetAsync(Guid tenantId, CustomerEmailBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/password-reset", tenantId, null, body, http, cancellationToken);

    public Task<CustomerCall<CustomerSessionDto>> ResetPasswordAsync(Guid tenantId, CustomerPasswordResetBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerSessionDto>(HttpMethod.Post, "internal/v1/customers/password-reset/complete", tenantId, null, body, http, cancellationToken);

    public Task<CustomerCall<CustomerProfileDto>> GetMeAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerProfileDto>(HttpMethod.Get, "internal/v1/customers/me", tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<CustomerProfileDto>> UpdateMeAsync(Guid tenantId, string session, CustomerProfileBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerProfileDto>(HttpMethod.Put, "internal/v1/customers/me", tenantId, session, body, http, cancellationToken);

    public Task<CustomerCall<object>> ChangePasswordAsync(Guid tenantId, string session, CustomerPasswordChangeBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/me/password", tenantId, session, body, http, cancellationToken);

    // =====================================================
    // PACKAGES (Slice 3B)
    // =====================================================
    public Task<CustomerCall<CustomerPackagesDto>> ListPackagesAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerPackagesDto>(HttpMethod.Get, "internal/v1/customers/me/packages", tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<CustomerParcelDetailDto>> GetPackageAsync(Guid tenantId, string session, Guid parcelId, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerParcelDetailDto>(HttpMethod.Get, "internal/v1/customers/me/packages/" + parcelId, tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<CustomerPreAlertCreatedDto>> CreatePreAlertAsync(Guid tenantId, string session, CustomerPreAlertBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerPreAlertCreatedDto>(HttpMethod.Post, "internal/v1/customers/me/pre-alerts", tenantId, session, body, http, cancellationToken);

    // Slice 4D: a package into one of the customer's shipments, moved, or taken out.
    public Task<CustomerCall<object>> MovePackageAsync(Guid tenantId, string session, CustomerPackageMoveBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/me/packages/move", tenantId, session, body, http, cancellationToken);

    public Task<CustomerCall<object>> CancelPreAlertAsync(Guid tenantId, string session, Guid preAlertId, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, "internal/v1/customers/me/pre-alerts/" + preAlertId, tenantId, session, null, http, cancellationToken);

    // Slice 3E: the customer answers a hold; a document travels in the body.
    public Task<CustomerCall<object>> AnswerHoldAsync(Guid tenantId, string session, Guid parcelId, CustomerHoldAnswerBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/me/packages/" + parcelId + "/answers", tenantId, session, body, http, cancellationToken);

    // =====================================================
    // SHIPMENTS (owner, 2026-10-18)
    // =====================================================
    public Task<CustomerCall<CustomerShipmentChoicesDto>> GetShipmentChoicesAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerShipmentChoicesDto>(HttpMethod.Get, "internal/v1/customers/me/shipments/choices", tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<CustomerShipmentCreatedDto>> CreateShipmentAsync(Guid tenantId, string session, CustomerShipmentBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerShipmentCreatedDto>(HttpMethod.Post, "internal/v1/customers/me/shipments", tenantId, session, body, http, cancellationToken);

    public Task<CustomerCall<CustomerShipmentDetailDto>> GetShipmentAsync(Guid tenantId, string session, Guid shipmentId, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<CustomerShipmentDetailDto>(HttpMethod.Get, "internal/v1/customers/me/shipments/" + shipmentId, tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<object>> ChangeShipmentAsync(Guid tenantId, string session, Guid shipmentId, CustomerShipmentInstructionsBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Put, "internal/v1/customers/me/shipments/" + shipmentId, tenantId, session, body, http, cancellationToken);

    public Task<CustomerCall<object>> CancelShipmentAsync(Guid tenantId, string session, Guid shipmentId, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, "internal/v1/customers/me/shipments/" + shipmentId, tenantId, session, null, http, cancellationToken);

    // =====================================================
    // TICKETS (Slice 4D step 4)
    // =====================================================
    public Task<CustomerCall<Vixly.Contracts.Operations.Tickets.CustomerTicketsDto>> ListTicketsAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Tickets.CustomerTicketsDto>(HttpMethod.Get, "internal/v1/customers/me/tickets", tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<Vixly.Contracts.Operations.Tickets.CustomerTicketCreatedDto>> CreateTicketAsync(Guid tenantId, string session, Vixly.Contracts.Operations.Tickets.CustomerTicketBody body,
        HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Tickets.CustomerTicketCreatedDto>(HttpMethod.Post, "internal/v1/customers/me/tickets", tenantId, session, body, http, cancellationToken);

    public Task<CustomerCall<Vixly.Contracts.Operations.Tickets.CustomerTicketDto>> GetTicketAsync(Guid tenantId, string session, Guid ticketId, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Tickets.CustomerTicketDto>(HttpMethod.Get, "internal/v1/customers/me/tickets/" + ticketId, tenantId, session, null, http, cancellationToken);

    public Task<CustomerCall<object>> ReplyTicketAsync(Guid tenantId, string session, Guid ticketId, Vixly.Contracts.Operations.Tickets.CustomerTicketReplyBody body, HttpContext http,
        CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "internal/v1/customers/me/tickets/" + ticketId + "/messages", tenantId, session, body, http, cancellationToken);

    // =====================================================
    // RATES (Slice 4)
    // =====================================================
    public Task<CustomerCall<Vixly.Contracts.Operations.Rates.CustomerRatesDto>> GetRatesAsync(Guid tenantId, string session, Vixly.Contracts.Operations.Rates.CustomerRatesBody body, HttpContext http,
        CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Rates.CustomerRatesDto>(HttpMethod.Post, "internal/v1/customers/me/rates", tenantId, session, body, http, cancellationToken);

    // Slice 5D: the shipment estimate while the customer builds or changes a shipment, and on their shipment. Read only.
    public Task<CustomerCall<Vixly.Contracts.Operations.Rates.CustomerShipmentEstimateDto>> EstimateShipmentAsync(Guid tenantId, string session,
        Vixly.Contracts.Operations.Rates.CustomerShipmentEstimateBody body, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Rates.CustomerShipmentEstimateDto>(HttpMethod.Post, "internal/v1/customers/me/shipments/estimate", tenantId, session, body, http, cancellationToken);

    // =====================================================
    // INVOICES (Slice 5F)
    // =====================================================
    public Task<CustomerCall<Vixly.Contracts.Operations.Billing.CustomerInvoicesDto>> ListInvoicesAsync(Guid tenantId, string session, HttpContext http, CancellationToken cancellationToken) =>
        SendAsync<Vixly.Contracts.Operations.Billing.CustomerInvoicesDto>(HttpMethod.Get, "internal/v1/customers/me/invoices", tenantId, session, null, http, cancellationToken);

    // The PDF of the customer's own invoice or approved credit note; Operations checks the ownership (S-3).
    public Task<(byte[] Content, string FileName)?> GetInvoiceDocumentAsync(Guid tenantId, string session, Guid invoiceId, Guid? creditNoteId, HttpContext http,
        CancellationToken cancellationToken) =>
        PdfAsync(tenantId, session, "internal/v1/customers/me/invoices/" + invoiceId + "/pdf" + (creditNoteId is { } credit ? "?credit=" + credit : string.Empty), "invoice", http,
            cancellationToken);

    // Slice 5A: a document of the customer's own shipment (PDF); Operations checks the ownership (S-3).
    // The certificate has its own address (Slice 5B); the other documents go by kind.
    public Task<(byte[] Content, string FileName)?> GetShipmentDocumentAsync(Guid tenantId, string session, Guid shipmentId, string kind, HttpContext http,
        CancellationToken cancellationToken) =>
        PdfAsync(tenantId, session, "internal/v1/customers/me/shipments/" + shipmentId + (kind == "certificate" ? "/certificate" : "/documents/" + Uri.EscapeDataString(kind)), kind, http,
            cancellationToken);

    private async Task<(byte[] Content, string FileName)?> PdfAsync(Guid tenantId, string session, string path, string kind, HttpContext http, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId, null));
            request.Headers.TryAddWithoutValidation(OperationsCallerClaims.SessionHeader, session);

            if (http.Connection.RemoteIpAddress is { } address)
            {
                request.Headers.Add(OperationsCallerClaims.ClientIpHeader, address.ToString());
            }

            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/pdf"
                ? (await response.Content.ReadAsByteArrayAsync(cancellationToken), response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? kind + ".pdf")
                : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Operations could not be reached for a document.");
            return null;
        }
    }

    // A photo of the customer's own package; Operations checks the ownership (S-3).
    public async Task<(byte[] Content, string ContentType)?> GetPhotoAsync(Guid tenantId, string session, Guid parcelId, Guid photoId, HttpContext http, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "internal/v1/customers/me/packages/" + parcelId + "/photos/" + photoId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId, null));
            request.Headers.TryAddWithoutValidation(OperationsCallerClaims.SessionHeader, session);

            if (http.Connection.RemoteIpAddress is { } address)
            {
                request.Headers.Add(OperationsCallerClaims.ClientIpHeader, address.ToString());
            }

            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            string? type = response.Content.Headers.ContentType?.MediaType;

            return response.IsSuccessStatusCode && type is "image/jpeg" or "image/png" or "image/webp" or "image/gif"
                ? (await response.Content.ReadAsByteArrayAsync(cancellationToken), type)
                : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Operations could not be reached for a package photo.");
            return null;
        }
    }

    // One signed call: the tenant in the token, the session and the visitor's address in headers.
    private async Task<CustomerCall<T>> SendAsync<T>(HttpMethod method, string path, Guid tenantId, string? session, object? body, HttpContext? http, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using HttpRequestMessage request = new(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(tenantId, null));

            if (!string.IsNullOrEmpty(session))
            {
                request.Headers.TryAddWithoutValidation(OperationsCallerClaims.SessionHeader, session);
            }

            if (http?.Connection.RemoteIpAddress is { } address)
            {
                request.Headers.Add(OperationsCallerClaims.ClientIpHeader, address.ToString());
            }

            if (http?.Request.Headers.UserAgent.ToString() is { Length: > 0 } agent)
            {
                request.Headers.TryAddWithoutValidation(OperationsCallerClaims.UserAgentHeader, agent.Length > 512 ? agent[..512] : agent);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body, body.GetType());
            }

            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object)
                    ? new CustomerCall<T>(null, null)
                    : new CustomerCall<T>(await response.Content.ReadFromJsonAsync<T>(cancellationToken), null);
            }

            if ((int)response.StatusCode is >= 400 and < 500 && response.Content.Headers.ContentType?.MediaType == "application/json"
                && await response.Content.ReadFromJsonAsync<OperationsApiErrorDto>(cancellationToken) is { Code.Length: > 0 } problem)
            {
                return new CustomerCall<T>(null, problem.Code, false, problem.Arguments);
            }

            _logger.LogWarning("Operations answered {Status} to {Path}.", (int)response.StatusCode, path);
            return new CustomerCall<T>(null, null, true);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException or NotSupportedException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Operations could not be reached for {Path}.", path);
            return new CustomerCall<T>(null, null, true);
        }
    }

    public async Task<OperationsTenantHostDto?> FindTenantAsync(string slug, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        string key = "customer.tenant:" + slug;

        if (_cache.TryGetValue(key, out OperationsTenantHostDto? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "internal/v1/tenants/by-host/" + Uri.EscapeDataString(slug));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _signer.Sign(null, null));
            using HttpResponseMessage response = await _http.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("Operations answered {Status} to the tenant lookup.", (int)response.StatusCode);
                return null;
            }

            OperationsTenantHostDto? host = await response.Content.ReadFromJsonAsync<OperationsTenantHostDto>(cancellationToken);

            if (host is not null)
            {
                _cache.Set(key, host, CacheTime);
            }

            return host;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Operations could not be reached for the tenant lookup.");
            return null;
        }
    }
}
