using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;

// =========================================================
// FILE: CreatePlatformLegalDraftUseCase.cs
//
// PURPOSE:
// Starts a new version of a legal document as a draft.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CreatePlatformLegalDraftUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public CreatePlatformLegalDraftUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<CreatePlatformLegalDraftResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformLegalDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return CreatePlatformLegalDraftResponse.Failure(refused);
        }

        PlatformLegalDocument document;

        try
        {
            document = PlatformLegalDocument.CreateDraft(request.Kind, request.Version, context.PlatformUserId, _legal.UtcNow);
        }
        catch (DomainException exception)
        {
            return CreatePlatformLegalDraftResponse.Failure(exception.Error);
        }

        if (await _legal.Store.VersionExistsAsync(document.Kind, document.Version, null, cancellationToken))
        {
            return CreatePlatformLegalDraftResponse.Failure(PlatformLegalErrors.VersionTaken);
        }

        // A new Terms draft starts with the published versions of the other documents.
        if (document.Kind == PlatformLegalDocumentKind.Terms)
        {
            List<PlatformLegalDocument> parts = new();

            foreach (PlatformLegalDocumentKind kind in await _legal.Store.ListPublishedKindsAsync(cancellationToken))
            {
                if (kind != PlatformLegalDocumentKind.Terms && await _legal.Store.GetPublishedAsync(kind, cancellationToken) is { } part)
                {
                    parts.Add(part);
                }
            }

            document.SetParts(parts, _legal.UtcNow);
        }

        _legal.Store.AddDocument(document);
        _legal.Audit(context.PlatformUserId, "platform.legal.draft_created", PlatformPermissions.LegalManage, nameof(PlatformLegalDocument), document.Id, new { kind = document.Kind.ToString(), version = document.Version });

        return await _legal.SaveAsync(cancellationToken)
            ? CreatePlatformLegalDraftResponse.Success(document.Id)
            : CreatePlatformLegalDraftResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
