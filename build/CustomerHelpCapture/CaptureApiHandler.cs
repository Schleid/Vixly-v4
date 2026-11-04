using System.Net;
using System.Net.Http.Json;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Packages;
using Vixly.Contracts.Operations.Internal;

namespace Vixly.CustomerHelpCapture;

// =========================================================
// FILE: CaptureApiHandler.cs
// PURPOSE:
// Supplies fictional contract responses to real Customer pages.
// RESPONSIBILITIES:
// - Refuse mutations and all unknown API calls.
// IMPORTANT NOTES:
// - No transport can reach a real Operations service.
// =========================================================
public sealed class CaptureApiHandler : HttpMessageHandler
{
    private bool _tourPending;
    private readonly bool _tourFixture;

    public CaptureApiHandler(bool tour = false)
    {
        _tourPending = tour;
        _tourFixture = tour;
    }
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Origin = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid Destination = Guid.Parse("33333333-3333-4333-8333-333333333333");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (_tourFixture && request.Method == HttpMethod.Post && path == "/internal/v1/customers/me/tour/dismiss")
        {
            _tourPending = false;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
        object? answer = request.Method != HttpMethod.Get ? null : path switch
        {
            var p when p.Contains("maintenance", StringComparison.Ordinal) => Array.Empty<OperationsMaintenanceWindowDto>(),
            var p when p.EndsWith("/bluewater-shipping", StringComparison.Ordinal) => new OperationsTenantHostDto(true, Tenant, "live", "full", "bluewater-shipping", "en"),
            "/internal/v1/customers/brand" => new CustomerPortalBrandDto("Bluewater Shipping", null, null, "#123456", "#0F766E"),
            "/internal/v1/customers/me" => new CustomerProfileDto(Tenant, "C00042", "Ana", "Pierre", null, "ana@bluewater-shipping.example", "2025550100", "en",
                new[] { new CustomerMailboxDto("Demo receiving warehouse", "Ana Pierre C00042", "10 Example Way", "Mailbox C00042", "Doral", "FL", "33100", "US") },
                new CustomerAddressDto("20 Example Road", null, "Les Cayes", null, null, "HT"), WelcomeTourPending: _tourPending),
            "/internal/v1/customers/me/shipments/choices" => new CustomerShipmentChoicesDto(
                new[] { new CustomerChoiceDto(Origin, "Demo receiving warehouse", "Doral, US") }, Array.Empty<CustomerChoiceDto>(), true, true, "Ana Pierre", "2025550100",
                new[] { "HT" }, new[] { new CustomerDestinationDto(Destination, "Demo destination office", "Les Cayes", "HT", true, true, Array.Empty<string>()) }, Services: new[] { "air_standard" }),
            "/internal/v1/customers/me/packages" => new CustomerPackagesDto(new[]
            {
                new CustomerPreAlertDto(Destination, "Demo carrier", "DEMO-TRACK-001", "Demo store", "Two cotton shirts", 35m, "USD", null, DateTimeOffset.Parse("2026-10-27T12:00:00Z"), WarehouseId: Origin, Warehouse: "Demo receiving warehouse")
            }, Array.Empty<CustomerParcelDto>(), "lb", "in"),
            _ => null
        };
        return Task.FromResult(answer is null ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(answer, answer.GetType()) });
    }
}
