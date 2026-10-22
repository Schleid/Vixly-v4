using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;

// =========================================================
// FILE: UpdateOperationsSiteDraftUseCase.cs
//
// PURPOSE:
// Saves the Owner changes to the draft site: settings and the texts of one language (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Nothing changes on the public site until Publish
// - Colours must pass WCAG 2.2 AA (domain)
// =========================================================
public sealed class UpdateOperationsSiteDraftUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UpdateOperationsSiteDraftUseCase(
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

    public async Task<UpdateOperationsSiteDraftResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UpdateOperationsSiteDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return UpdateOperationsSiteDraftResponse.Failure(refused);
        }

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        if (brand is null)
        {
            return UpdateOperationsSiteDraftResponse.Failure(OperationsCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            brand.ChangeDraft(
                brand.Draft.With(request.DisplayName, request.Preset, request.PrimaryColour, request.AccentColour, request.Services, request.ShowSteps, request.ShowFaq,
                    (request.FacebookUrl, request.InstagramUrl, request.XUrl, request.LinkedInUrl, request.YouTubeUrl, request.TikTokUrl)),
                utcNow);

            foreach ((string key, string? value) in request.Texts)
            {
                brand.SetDraftText(request.Language, key, value, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return UpdateOperationsSiteDraftResponse.Failure(exception.Error);
        }

        _audit.Record(context.UserId, "ops.site.draft_changed", nameof(OperationsTenantBrand), brand.Id, arguments: new { language = request.Language, texts = request.Texts.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdateOperationsSiteDraftResponse.Success()
            : UpdateOperationsSiteDraftResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
