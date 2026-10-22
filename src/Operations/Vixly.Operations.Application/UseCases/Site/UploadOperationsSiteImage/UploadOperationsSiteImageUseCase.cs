using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;

// =========================================================
// FILE: UploadOperationsSiteImageUseCase.cs
//
// PURPOSE:
// Adds a logo, favicon or hero image to the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The file type comes from its bytes: SVG and anything unknown are refused
// =========================================================
public sealed class UploadOperationsSiteImageUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsImageInspector _inspector;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UploadOperationsSiteImageUseCase(
        IOperationsSiteRepository site,
        IOperationsImageInspector inspector,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UploadOperationsSiteImageResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UploadOperationsSiteImageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return UploadOperationsSiteImageResponse.Failure(refused);
        }

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        if (brand is null)
        {
            return UploadOperationsSiteImageResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (request.Content.Length > OperationsSiteImage.MaxBytes)
        {
            return UploadOperationsSiteImageResponse.Failure(OperationsBrandingErrors.ImageTooLarge);
        }

        OperationsImageInfo? info = _inspector.Inspect(request.Content);

        if (info is null)
        {
            return UploadOperationsSiteImageResponse.Failure(OperationsBrandingErrors.ImageTypeNotAllowed);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSiteImage image;

        try
        {
            image = OperationsSiteImage.Create(context.RequiredTenantId, request.Kind, info.ContentType, request.Content, info.Width, info.Height, info.Sha256, utcNow);
        }
        catch (DomainException exception)
        {
            return UploadOperationsSiteImageResponse.Failure(exception.Error);
        }

        _site.AddImage(image);
        brand.ChangeDraft(brand.Draft.WithImage(request.Kind, image.Id), utcNow);
        _audit.Record(context.UserId, "ops.site.image_uploaded", nameof(OperationsSiteImage), image.Id, arguments: new { kind = request.Kind.ToString(), bytes = request.Content.Length });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UploadOperationsSiteImageResponse.Success(image.Id)
            : UploadOperationsSiteImageResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
