using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsInspectionPhotos.cs
//
// PURPOSE:
// Checks and stores the photos of the contents taken during an inspection (Slice 3C).
//
// RESPONSIBILITIES:
// - Refuse anything that is not a JPEG, PNG or WebP image within the size limits
// - Store the files and add their records; remove the files again when the save fails
//
// IMPORTANT NOTES:
// - The same limits as the inspection station (Slice 3B): six photos of up to 10 MB
// - Each photo is a "contents" photo of the parcel
// =========================================================
public sealed class OperationsInspectionPhotos
{
    private readonly IOperationsImageInspector _images;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsReceivingRepository _receiving;

    public OperationsInspectionPhotos(IOperationsImageInspector images, IOperationsFileStore files, IOperationsReceivingRepository receiving)
    {
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
    }

    public (IReadOnlyList<(OperationsParcelPhotoUpload Upload, OperationsImageInfo Info)>? Photos, Error? Refused) Check(IReadOnlyList<OperationsParcelPhotoUpload> uploads)
    {
        ArgumentNullException.ThrowIfNull(uploads);

        if (uploads.Count > OperationsReceivingLimits.MaxPhotosPerReceipt)
        {
            return (null, OperationsReceivingErrors.PhotoInvalid);
        }

        List<(OperationsParcelPhotoUpload, OperationsImageInfo)> photos = new();

        foreach (OperationsParcelPhotoUpload upload in uploads)
        {
            if (upload.Content.Length is 0 or > OperationsReceivingLimits.MaxPhotoBytes || _images.Inspect(upload.Content) is not { } info)
            {
                return (null, OperationsReceivingErrors.PhotoInvalid);
            }

            photos.Add((upload with { Kind = "contents" }, info));
        }

        return (photos, null);
    }

    public async Task<IReadOnlyList<string>> StoreAsync(Guid tenantId, Guid parcelId, IReadOnlyList<(OperationsParcelPhotoUpload Upload, OperationsImageInfo Info)> photos, Guid userId,
        DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(photos);
        List<string> stored = new();

        foreach ((OperationsParcelPhotoUpload upload, OperationsImageInfo info) in photos)
        {
            string key = await _files.SaveAsync(tenantId, "parcels", upload.Content, info.ContentType, cancellationToken);
            stored.Add(key);
            _receiving.Add(OperationsParcelPhoto.Add(tenantId, parcelId, upload.Kind, key, info.ContentType, upload.Content.Length, info.Sha256, userId, utcNow));
        }

        return stored;
    }

    public async Task DeleteAsync(Guid tenantId, IReadOnlyList<string> stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        foreach (string key in stored)
        {
            await _files.DeleteAsync(tenantId, key, CancellationToken.None);
        }
    }
}
