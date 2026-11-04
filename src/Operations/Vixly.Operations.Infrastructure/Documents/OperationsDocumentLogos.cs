using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Documents;

// =========================================================
// FILE: OperationsDocumentLogos.cs
//
// PURPOSE:
// The company's document logo and its black and white label copy, for the documents drawn in this request (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Read them once per request, for the company of the request
//
// IMPORTANT NOTES:
// - Outside a company (system jobs) there is no logo: documents show the company name
// =========================================================
internal sealed class OperationsDocumentLogos
{
    private readonly OperationsDbContext _context;

    private readonly IOperationsTenantScope _scope;

    private (Guid TenantId, byte[]? Logo, byte[]? Label)? _read;

    public OperationsDocumentLogos(OperationsDbContext context, IOperationsTenantScope scope)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public byte[]? Logo => Read().Logo;

    public byte[]? Label => Read().Label;

    private (Guid TenantId, byte[]? Logo, byte[]? Label) Read()
    {
        if (_scope.TenantId is not { } tenantId)
        {
            return (Guid.Empty, null, null);
        }

        if (_read is { } read && read.TenantId == tenantId)
        {
            return read;
        }

        var ids = _context.TenantSettings.AsNoTracking().Where(item => item.TenantId == tenantId)
            .Select(item => new { item.DocumentLogoImageId, item.LabelLogoImageId }).FirstOrDefault();
        byte[]? Image(Guid? id) => id is { } imageId
            ? _context.SiteImages.AsNoTracking().Where(item => item.Id == imageId && item.TenantId == tenantId).Select(item => item.Content).FirstOrDefault()
            : null;

        _read = (tenantId, Image(ids?.DocumentLogoImageId), Image(ids?.LabelLogoImageId));
        return _read.Value;
    }
}
