using Vixly.Contracts.Platform.Payments;
using Vixly.Public.Web.Platform;

namespace Vixly.Public.Web.Features.Pay;

// =========================================================
// FILE: PaymentTarget.cs
//
// PURPOSE:
// Who a payment page pays for: an emailed pay or save-card link, or the registration invoice.
//
// RESPONSIBILITIES:
// - Load the invoice summary
// - Start the card checkout or report a Zelle payment for that target
// - Know the page to come back to
//
// IMPORTANT NOTES:
// - The token or access token is never shown or logged
// =========================================================
public sealed class PaymentTarget
{
    private PaymentTarget(string? linkToken, string? access, string returnPath)
    {
        LinkToken = linkToken;
        Access = access;
        ReturnPath = returnPath;
    }

    public string? LinkToken { get; }

    public string? Access { get; }

    public string ReturnPath { get; }

    public static PaymentTarget Link(string token, bool saveCard) => new(token, null, (saveCard ? "/cards/" : "/pay/") + Uri.EscapeDataString(token));

    public static PaymentTarget Registration(string access) => new(null, access, "/register/payment");

    public Task<PlatformApiResult<PlatformPaymentLinkInfo>> LoadAsync(PlatformApi api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        return LinkToken is { } token ? api.PaymentLinkAsync(token, cancellationToken) : api.RegistrationInvoiceAsync(Access!, cancellationToken);
    }

    public Task<PlatformApiResult<PlatformCheckoutStarted>> CheckoutAsync(PlatformApi api, PlatformCheckoutBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        return LinkToken is { } token ? api.PaymentLinkCheckoutAsync(token, body, cancellationToken) : api.RegistrationCheckoutAsync(Access!, body, cancellationToken);
    }

    public Task<PlatformApiResult<bool>> ZelleAsync(PlatformApi api, PlatformZelleReportBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        return LinkToken is { } token ? api.PaymentLinkZelleAsync(token, body, cancellationToken) : api.RegistrationZelleAsync(Access!, body, cancellationToken);
    }
}
