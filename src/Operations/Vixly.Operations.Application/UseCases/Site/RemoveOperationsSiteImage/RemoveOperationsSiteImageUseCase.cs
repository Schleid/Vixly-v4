using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Site.RemoveOperationsSiteImage;

// =========================================================
// FILE: RemoveOperationsSiteImageUseCase.cs
//
// PURPOSE:
// Removes a logo, favicon or hero image from the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The image stays stored: a published site may still use it
// =========================================================
public sealed class RemoveOperationsSiteImageUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsSiteImageUseCase(
        IOperationsSiteRepository site,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsSiteImageResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsSiteImageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return RemoveOperationsSiteImageResponse.Failure(refused);
        }

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        if (brand is null)
        {
            return RemoveOperationsSiteImageResponse.Failure(OperationsCommonErrors.NotFound);
        }

        brand.ChangeDraft(brand.Draft.WithoutImage(request.Kind), _timeProvider.GetUtcNow());
        _audit.Record(context.UserId, "ops.site.image_removed", nameof(OperationsTenantBrand), brand.Id, arguments: new { kind = request.Kind.ToString() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsSiteImageResponse.Success()
            : RemoveOperationsSiteImageResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
