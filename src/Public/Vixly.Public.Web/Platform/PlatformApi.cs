using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Common;
using Vixly.Contracts.Platform.Leads;
using Vixly.Contracts.Platform.Legal;
using Vixly.Contracts.Platform.Payments;
using Vixly.Contracts.Platform.Registration;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PlatformApi.cs
//
// PURPOSE:
// The public site's typed client for Platform.Api (Stage 2).
//
// RESPONSIBILITIES:
// - Call every public Platform endpoint the site needs
// - Turn Platform errors and outages into one result shape
//
// IMPORTANT NOTES:
// - Posts are never retried
// - Tokens (registration, pay link) are never logged
// - Platform's error text already comes in the page language (ADR-0014)
// =========================================================
public sealed class PlatformApi
{
    public const string Unavailable = "public.platform_unavailable";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    private readonly ILogger<PlatformApi> _logger;

    public PlatformApi(HttpClient http, ILogger<PlatformApi> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // =====================================================
    // CATALOGUE, LEGAL, CONTACT
    // =====================================================
    public Task<PlatformApiResult<IReadOnlyList<PlatformPublicPlan>>> PlansAsync(CancellationToken cancellationToken)
        => SendAsync<IReadOnlyList<PlatformPublicPlan>>(new HttpRequestMessage(HttpMethod.Get, "registration/plans"), null, cancellationToken);

    public Task<PlatformApiResult<PlatformLegalTermsInfo>> TermsAsync(string language, CancellationToken cancellationToken)
        => SendAsync<PlatformLegalTermsInfo>(new HttpRequestMessage(HttpMethod.Get, "legal/terms?language=" + Uri.EscapeDataString(language)), null, cancellationToken);

    public Task<HttpResponseMessage> LegalFileAsync(Guid documentId, string language, CancellationToken cancellationToken)
        => _http.GetAsync($"legal/documents/{documentId:D}/{Uri.EscapeDataString(language)}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    public Task<PlatformApiResult<bool>> SubmitLeadAsync(PlatformSalesLeadBody body, CancellationToken cancellationToken)
        => SendAsync<bool>(Post("leads", body), null, cancellationToken);

    // =====================================================
    // PAY AND SAVE-CARD LINKS
    // =====================================================
    public Task<PlatformApiResult<PlatformPaymentLinkInfo>> PaymentLinkAsync(string token, CancellationToken cancellationToken)
        => SendAsync<PlatformPaymentLinkInfo>(new HttpRequestMessage(HttpMethod.Get, "payment-links/" + Uri.EscapeDataString(token)), null, cancellationToken);

    public Task<PlatformApiResult<PlatformCheckoutStarted>> PaymentLinkCheckoutAsync(string token, PlatformCheckoutBody body, CancellationToken cancellationToken)
        => SendAsync<PlatformCheckoutStarted>(Post("payment-links/" + Uri.EscapeDataString(token) + "/checkout", body), null, cancellationToken);

    public Task<PlatformApiResult<bool>> PaymentLinkZelleAsync(string token, PlatformZelleReportBody body, CancellationToken cancellationToken)
        => SendAsync<bool>(Post("payment-links/" + Uri.EscapeDataString(token) + "/zelle", body), null, cancellationToken);

    public Task<HttpResponseMessage> ZelleQrAsync(int version, CancellationToken cancellationToken)
        => _http.GetAsync("payments/zelle/qr?v=" + version.ToString(System.Globalization.CultureInfo.InvariantCulture), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    // =====================================================
    // REGISTRATION (access token as bearer, Slice 2)
    // =====================================================
    public Task<PlatformApiResult<PlatformSlugAvailability>> SlugAsync(string slug, CancellationToken cancellationToken)
        => SendAsync<PlatformSlugAvailability>(new HttpRequestMessage(HttpMethod.Get, "registration/slugs/" + Uri.EscapeDataString(slug)), null, cancellationToken);

    public Task<PlatformApiResult<PlatformRegistrationAccess>> StartAsync(StartPlatformRegistrationBody body, CancellationToken cancellationToken)
        => SendAsync<PlatformRegistrationAccess>(Post("registration", body), null, cancellationToken);

    public Task<PlatformApiResult<PlatformRegistrationAccess>> VerifyAsync(string token, CancellationToken cancellationToken)
        => SendAsync<PlatformRegistrationAccess>(Post("registration/verify", new VerifyPlatformRegistrationBody(token)), null, cancellationToken);

    public Task<PlatformApiResult<bool>> CorrectEmailAsync(string access, string email, CancellationToken cancellationToken)
        => SendAsync<bool>(Post("registration/email", new CorrectPlatformRegistrationEmailBody(email)), access, cancellationToken);

    public Task<PlatformApiResult<bool>> ResendAsync(string access, CancellationToken cancellationToken)
        => SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, "registration/resend"), access, cancellationToken);

    public Task<PlatformApiResult<PlatformRegistrationStatus>> StatusAsync(string access, CancellationToken cancellationToken)
        => SendAsync<PlatformRegistrationStatus>(new HttpRequestMessage(HttpMethod.Get, "registration/status"), access, cancellationToken);

    public Task<PlatformApiResult<PlatformRegistrationOffer>> OfferAsync(string token, CancellationToken cancellationToken)
        => SendAsync<PlatformRegistrationOffer>(Post("registration/offer", new PlatformOfferTokenBody { Token = token }), null, cancellationToken);

    public Task<PlatformApiResult<PlatformPromotionCheck>> PromotionAsync(string code, CancellationToken cancellationToken)
        => SendAsync<PlatformPromotionCheck>(new HttpRequestMessage(HttpMethod.Get, "registration/promotions/" + Uri.EscapeDataString(code)), null, cancellationToken);

    public Task<PlatformApiResult<PlatformPlanChoice>> SelectPlanAsync(string access, SelectPlatformRegistrationPlanBody body, CancellationToken cancellationToken)
        => SendAsync<PlatformPlanChoice>(Post("registration/plan", body), access, cancellationToken);

    public Task<PlatformApiResult<PlatformPaymentLinkInfo>> RegistrationInvoiceAsync(string access, CancellationToken cancellationToken)
        => SendAsync<PlatformPaymentLinkInfo>(new HttpRequestMessage(HttpMethod.Get, "registration/invoice"), access, cancellationToken);

    public Task<PlatformApiResult<PlatformCheckoutStarted>> RegistrationCheckoutAsync(string access, PlatformCheckoutBody body, CancellationToken cancellationToken)
        => SendAsync<PlatformCheckoutStarted>(Post("registration/checkout", body), access, cancellationToken);

    public Task<PlatformApiResult<bool>> RegistrationZelleAsync(string access, PlatformZelleReportBody body, CancellationToken cancellationToken)
        => SendAsync<bool>(Post("registration/zelle", body), access, cancellationToken);

    public async Task<bool> AliveAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _http.GetAsync("health/live", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    // =====================================================
    // METHOD: SEND
    // =====================================================
    private async Task<PlatformApiResult<T>> SendAsync<T>(HttpRequestMessage request, string? access, CancellationToken cancellationToken)
    {
        using (request)
        {
            if (access is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            }

            try
            {
                using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return new PlatformApiResult<T>((T)(object)true, null, (int)response.StatusCode);
                    }

                    T? value = response.StatusCode == HttpStatusCode.NoContent ? default : await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
                    return new PlatformApiResult<T>(value, null, (int)response.StatusCode);
                }

                PlatformApiError? error = null;

                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    error = await response.Content.ReadFromJsonAsync<PlatformApiError>(Json, cancellationToken);
                }

                return new PlatformApiResult<T>(default, error ?? new PlatformApiError(Unavailable, string.Empty), (int)response.StatusCode);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                // The path may hold a token: only the method is logged.
                _logger.LogWarning(exception, "Platform.Api call failed ({Method}).", request.Method);
                return new PlatformApiResult<T>(default, new PlatformApiError(Unavailable, string.Empty), 0);
            }
        }
    }

    private static HttpRequestMessage Post<TBody>(string path, TBody body)
    {
        return new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: Json) };
    }
}
