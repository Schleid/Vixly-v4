using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Infrastructure.Mail;

// =========================================================
// FILE: OperationsMailBrandReader.cs
//
// PURPOSE:
// Finds the company's own look for its emails: the published logo and main colour (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Read the published website settings of the company in scope
// - Return the logo when it is a PNG or JPEG small enough for an email, and the main colour
//
// IMPORTANT NOTES:
// - The company comes from the tenant scope (a request, or the outbox and workers, which enter it per company)
// - No company in scope, or nothing published: null, and the email keeps its plain header
// - Read only; a failure never stops an email
// =========================================================
public sealed class OperationsMailBrandReader
{
    public const int MaxLogoBytes = 300 * 1024;

    private static readonly string[] EmailTypes = { "image/png", "image/jpeg" };

    private readonly IOperationsTenantScope _scope;

    private readonly OperationsDbContext _db;

    public OperationsMailBrandReader(IOperationsTenantScope scope, OperationsDbContext db)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<(string? Colour, VixlyEmailLogo? Logo)?> ReadAsync(CancellationToken cancellationToken)
    {
        if (_scope.TenantId is not Guid tenantId)
        {
            return null;
        }

        var brand = await _db.TenantBrands.AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => new { item.Published.PrimaryColour, item.Published.LogoImageId })
            .FirstOrDefaultAsync(cancellationToken);
        if (brand is null)
        {
            return null;
        }

        VixlyEmailLogo? logo = null;
        if (brand.LogoImageId is Guid imageId)
        {
            var image = await _db.SiteImages.AsNoTracking()
                .Where(item => item.Id == imageId && item.TenantId == tenantId && EmailTypes.Contains(item.ContentType) && item.Content.Length <= MaxLogoBytes)
                .Select(item => new { item.ContentType, item.Content, item.Width, item.Height })
                .FirstOrDefaultAsync(cancellationToken);
            logo = image is null ? null : new VixlyEmailLogo(image.Content, image.ContentType, image.Width, image.Height);
        }

        return (brand.PrimaryColour, logo);
    }
}
