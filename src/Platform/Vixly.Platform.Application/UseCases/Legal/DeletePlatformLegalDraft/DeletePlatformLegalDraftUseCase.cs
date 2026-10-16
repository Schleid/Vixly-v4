using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.DeletePlatformLegalDraft;

// =========================================================
// FILE: DeletePlatformLegalDraftUseCase.cs
//
// PURPOSE:
// Deletes a draft and its files; published versions are never deleted.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class DeletePlatformLegalDraftUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public DeletePlatformLegalDraftUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<DeletePlatformLegalDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DeletePlatformLegalDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return DeletePlatformLegalDraftResponse.Failure(refused);
        }

        PlatformLegalDocument? document = await _legal.Store.GetDocumentAsync(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return DeletePlatformLegalDraftResponse.Failure(PlatformLegalErrors.DocumentNotFound);
        }

        if (!document.IsDraft)
        {
            return DeletePlatformLegalDraftResponse.Failure(PlatformLegalErrors.NotDraft);
        }

        await _legal.Store.RemoveDraftAsync(document, cancellationToken);
        _legal.Audit(context.PlatformUserId, "platform.legal.draft_deleted", PlatformPermissions.LegalManage, nameof(PlatformLegalDocument), document.Id, new { kind = document.Kind.ToString(), version = document.Version });

        return await _legal.SaveAsync(cancellationToken)
            ? DeletePlatformLegalDraftResponse.Success()
            : DeletePlatformLegalDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
