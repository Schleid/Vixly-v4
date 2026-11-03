using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Branding;

// =========================================================
// FILE: OperationsEdgeBrandHandler.cs
//
// PURPOSE:
// Sends a company's brand to the Cloudflare Worker after its website is published.
//
// RESPONSIBILITIES:
// - Find the company's portal address name with Platform, then send the published brand
//
// IMPORTANT NOTES:
// - Runs in the company's tenant scope (the dispatcher enters it from the message)
// - A failure throws: the outbox tries again with back-off
// =========================================================
public sealed class OperationsEdgeBrandHandler : IOperationsOutboxHandler
{
    private readonly IOperationsPlatformClient _platform;

    private readonly OperationsEdgeBrands _brands;

    public OperationsEdgeBrandHandler(IOperationsPlatformClient platform, OperationsEdgeBrands brands)
    {
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
    }

    public string MessageType => OperationsEdgeBrandChanged.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.TenantId is not { } tenantId || await _platform.GetTenantAsync(tenantId, cancellationToken) is not { Slug.Length: > 0 } account)
        {
            return;
        }

        await _brands.SendAsync(account, cancellationToken);
    }
}
