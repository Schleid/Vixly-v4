using Microsoft.Extensions.Logging;
using Vixly.Contracts.Platform.Internal.Entitlements;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Infrastructure.Platform;

// =========================================================
// FILE: OperationsUsageReporter.cs
//
// PURPOSE:
// Sends a company's monthly shipments to Platform (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Post the month, the count and whether the month is over
//
// IMPORTANT NOTES:
// - False when Platform cannot be reached or refuses; the hourly job tries again
// =========================================================
public sealed class OperationsUsageReporter : IOperationsUsageReporter
{
    private readonly OperationsPlatformSender _sender;

    private readonly ILogger<OperationsUsageReporter> _logger;

    public OperationsUsageReporter(OperationsPlatformSender sender, ILogger<OperationsUsageReporter> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ReportShipmentsAsync(Guid tenantId, string month, int shipments, bool isFinal, CancellationToken cancellationToken)
    {
        try
        {
            (_, string? refused) = await _sender.SendAsync<object>(HttpMethod.Post, "internal/v1/tenant/usage/shipments", tenantId, null,
                new PlatformShipmentUsageBody(month, shipments, isFinal), cancellationToken);

            if (refused is not null)
            {
                _logger.LogWarning("Platform refused the shipment count of {Month}: {Code}.", month, refused);
            }

            return refused is null;
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            _logger.LogWarning(exception, "The shipment count could not be sent to Platform; it is sent again next hour.");
            return false;
        }
    }
}
