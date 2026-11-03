using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Branding;

// =========================================================
// FILE: OperationsEdgeBrands.cs
//
// PURPOSE:
// Sends the current company's brand to the Cloudflare Worker (Worker design, section 3; owner 2026-10-28).
//
// RESPONSIBILITIES:
// - Build the brand from the published site: name, main colour, logo; and the company's default language
// - Leave the logo out when it is over 100 KB (the Worker keeps it with the record, because the servers may be down)
//
// IMPORTANT NOTES:
// - Runs inside the company's tenant scope
// - Sent when the website is published (outbox) and every night (OperationsEdgeBrandWorker)
// =========================================================
public sealed class OperationsEdgeBrands
{
    public const int MaxLogoBytes = 100 * 1024;

    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsEdgeClient _edge;

    public OperationsEdgeBrands(IOperationsSiteRepository site, IOperationsEdgeClient edge)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
    }

    public async Task<OperationsEdgeSendResult> SendAsync(OperationsTenantAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);
        OperationsSiteImage? logo = brand?.Published.LogoImageId is { } logoId ? await _site.GetImageAsync(logoId, cancellationToken) : null;
        bool sendLogo = logo is not null && logo.Content.Length <= MaxLogoBytes && OperationsSiteImage.ContentTypes.Contains(logo.ContentType);
        string language = account.DefaultLanguage?.Trim().ToLowerInvariant() is "fr" or "es" ? account.DefaultLanguage.Trim().ToLowerInvariant() : "en";

        return await _edge.SendBrandAsync(new OperationsEdgeBrand(
            account.Slug,
            string.IsNullOrWhiteSpace(brand?.DisplayName) ? account.DisplayName : brand.DisplayName,
            brand?.Published.PrimaryColour,
            language,
            sendLogo ? logo!.Content : null,
            sendLogo ? logo!.ContentType : null), cancellationToken);
    }
}
