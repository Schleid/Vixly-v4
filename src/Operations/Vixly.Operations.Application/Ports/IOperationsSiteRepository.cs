using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSiteRepository.cs
//
// PURPOSE:
// The brand, texts and images of the current tenant site (Slice 1B).
//
// RESPONSIBILITIES:
// - Load the brand with its texts
// - Add and load images
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsSiteRepository
{
    Task<OperationsTenantBrand?> FindBrandAsync(CancellationToken cancellationToken);

    void AddImage(OperationsSiteImage image);

    Task<OperationsSiteImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken);
}
