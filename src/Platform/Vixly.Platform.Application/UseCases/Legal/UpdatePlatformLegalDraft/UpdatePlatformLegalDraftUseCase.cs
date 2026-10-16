using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.UpdatePlatformLegalDraft;

// =========================================================
// FILE: UpdatePlatformLegalDraftUseCase.cs
//
// PURPOSE:
// Changes a draft: version label, acceptance rule, effective date and included versions.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformLegalDraftUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public UpdatePlatformLegalDraftUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<UpdatePlatformLegalDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformLegalDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return UpdatePlatformLegalDraftResponse.Failure(refused);
        }

        PlatformLegalDocument? document = await _legal.Store.GetDocumentAsync(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return UpdatePlatformLegalDraftResponse.Failure(PlatformLegalErrors.DocumentNotFound);
        }

        if (!document.IsDraft)
        {
            return UpdatePlatformLegalDraftResponse.Failure(PlatformLegalErrors.NotDraft);
        }

        try
        {
            document.UpdateDraft(request.Version, request.AcceptanceRequired, request.EffectiveUtc, _legal.UtcNow);

            if (document.Kind == PlatformLegalDocumentKind.Terms)
            {
                IReadOnlyList<PlatformLegalDocument> parts = await _legal.Store.GetDocumentsAsync(request.IncludedDocumentIds.Distinct().ToArray(), cancellationToken);

                if (parts.Count != request.IncludedDocumentIds.Distinct().Count())
                {
                    return UpdatePlatformLegalDraftResponse.Failure(PlatformLegalErrors.PartsInvalid);
                }

                document.SetParts(parts, _legal.UtcNow);
            }
        }
        catch (DomainException exception)
        {
            return UpdatePlatformLegalDraftResponse.Failure(exception.Error);
        }

        if (await _legal.Store.VersionExistsAsync(document.Kind, document.Version, document.Id, cancellationToken))
        {
            return UpdatePlatformLegalDraftResponse.Failure(PlatformLegalErrors.VersionTaken);
        }

        _legal.Audit(context.PlatformUserId, "platform.legal.draft_updated", PlatformPermissions.LegalManage, nameof(PlatformLegalDocument), document.Id,
            new { version = document.Version, acceptanceRequired = document.AcceptanceRequired, effective = document.EffectiveUtc, parts = document.Parts.Select(part => part.Kind + " " + part.Version).ToArray() });

        return await _legal.SaveAsync(cancellationToken)
            ? UpdatePlatformLegalDraftResponse.Success()
            : UpdatePlatformLegalDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
