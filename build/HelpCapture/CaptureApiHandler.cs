using System.Net;
using System.Net.Http.Json;
using Vixly.Contracts.Platform.Legal;
using Vixly.Contracts.Platform.Payments;
using Vixly.Contracts.Platform.Registration;

namespace Vixly.HelpCapture;

// =========================================================
// FILE: CaptureApiHandler.cs
//
// PURPOSE:
// Supplies fictional contract data to real Public components.
//
// RESPONSIBILITIES:
// - Model each visible sign-up and payment screen without services.
//
// IMPORTANT NOTES:
// - No network fallback, real money, email or persisted accounts.
// - Unknown requests fail instead of reaching another service.
// =========================================================
public sealed class CaptureApiHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath.Trim('/');
        object? answer = path switch
        {
            "maintenance" => Array.Empty<object>(),
            "registration/plans" => Plans(),
            "legal/terms" => Terms(request.RequestUri.Query),
            "registration/status" => Status(request.Headers.Authorization?.Parameter),
            "registration/invoice" => Invoice(),
            "registration/offer" => new PlatformRegistrationOffer("Bluewater Shipping", "a**@bluewater-shipping.example",
                "basic", "Basic", "USD", "monthly", 2, 24m, null, 0m, [], true, 14,
                new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero)),
            "payment-links/help-demo" => Invoice(),
            "payment-links/help-save-card" => new PlatformPaymentLinkInfo("save_card", "Bluewater Shipping", null, null, null, null, false, true, "2026-10"),
            _ => null
        };
        return Task.FromResult(answer is null ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(answer) });
    }

    // =====================================================
    // METHOD: BUILD FICTIONAL PLANS
    // =====================================================
    private static PlatformPublicPlan[] Plans() =>
    [
        new("basic", "Basic", null, "USD", 12m, 120m, 0m, 2, 10, 1, ["data_export"], 14),
        new("growth", "Growth", null, "USD", 25m, 250m, 99m, 2, 50, 5, ["data_export", "report_scheduling"], 30)
    ];

    // =====================================================
    // METHOD: BUILD FICTIONAL TERMS
    // =====================================================
    private static PlatformLegalTermsInfo Terms(string query)
    {
        string language = query.Contains("fr", StringComparison.Ordinal) ? "fr" : query.Contains("es", StringComparison.Ordinal) ? "es" : "en";
        return new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "2026-10", language,
            "demo", null, new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), "/legal/demo.pdf",
            [new PlatformLegalPartInfo(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), "privacy", "2026-10", "demo", null, "/legal/privacy-demo.pdf")]);
    }

    // =====================================================
    // METHOD: BUILD FICTIONAL REGISTRATION STATUS
    // =====================================================
    private static PlatformRegistrationStatus Status(string? token) => new(token switch
    {
        "help-plan" => "pending_plan_selection", "help-pay" => "pending_payment",
        "help-done" => "pending_account_setup", _ => "pending_email_verification"
    }, "a**@bluewater-shipping.example", "bluewater-shipping", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    // =====================================================
    // METHOD: BUILD FICTIONAL INVOICE
    // =====================================================
    private static PlatformPaymentLinkInfo Invoice() => new("pay_invoice", "Bluewater Shipping", "DEMO-2026-001",
        125.50m, "USD", new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), false, true, "2026-10",
        new PlatformZelleInfo("Bluewater Shipping", "billing@bluewater-shipping.example", null, null, null, null, true));
}
