using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Site.PublishOperationsSite;

// =========================================================
// FILE: PublishOperationsSiteUseCase.cs
//
// PURPOSE:
// Makes the draft the public site (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Step-up arrives with Slice 1C
// =========================================================
public sealed class PublishOperationsSiteUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PublishOperationsSiteUseCase(
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

    public async Task<PublishOperationsSiteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        PublishOperationsSiteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandPublish, cancellationToken) is { } refused)
        {
            return PublishOperationsSiteResponse.Failure(refused);
        }

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        if (brand is null)
        {
            return PublishOperationsSiteResponse.Failure(OperationsCommonErrors.NotFound);
        }

        brand.Publish(_timeProvider.GetUtcNow());
        _audit.Record(context.UserId, "ops.site.published", nameof(OperationsTenantBrand), brand.Id, after: new { version = brand.PublishedVersion });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? PublishOperationsSiteResponse.Success(brand.PublishedVersion)
            : PublishOperationsSiteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
