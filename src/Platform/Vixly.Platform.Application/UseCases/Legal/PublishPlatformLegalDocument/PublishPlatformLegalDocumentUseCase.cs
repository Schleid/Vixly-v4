using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.PublishPlatformLegalDocument;

// =========================================================
// FILE: PublishPlatformLegalDocumentUseCase.cs
//
// PURPOSE:
// Publishes a draft: it becomes the current version of its kind, never changed again (D-Lg3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up; notices go out through the reminder clock
// =========================================================
public sealed class PublishPlatformLegalDocumentUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public PublishPlatformLegalDocumentUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<PublishPlatformLegalDocumentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PublishPlatformLegalDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalPublish, requireStepUp: true, cancellationToken) is { } refused)
        {
            return PublishPlatformLegalDocumentResponse.Failure(refused);
        }

        PlatformLegalDocument? document = await _legal.Store.GetDocumentAsync(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return PublishPlatformLegalDocumentResponse.Failure(PlatformLegalErrors.DocumentNotFound);
        }

        if (!document.IsDraft)
        {
            return PublishPlatformLegalDocumentResponse.Failure(PlatformLegalErrors.NotDraft);
        }

        PlatformLegalDocument? previous = await _legal.Store.GetPublishedAsync(document.Kind, cancellationToken);

        try
        {
            document.Publish(await _legal.Store.ListFileLanguagesAsync(document.Id, cancellationToken), await _legal.Store.ListPublishedKindsAsync(cancellationToken), context.PlatformUserId, _legal.UtcNow);
        }
        catch (DomainException exception)
        {
            return PublishPlatformLegalDocumentResponse.Failure(exception.Error);
        }

        previous?.Supersede(_legal.UtcNow);
        _legal.Published(document);
        _legal.Audit(context.PlatformUserId, "platform.legal.published", PlatformPermissions.LegalPublish, nameof(PlatformLegalDocument), document.Id,
            new { kind = document.Kind.ToString(), version = document.Version, acceptanceRequired = document.NeedsAcceptance, effective = document.EffectiveUtc, deadline = document.DeadlineUtc, superseded = previous?.Version });

        return await _legal.SaveAsync(cancellationToken)
            ? PublishPlatformLegalDocumentResponse.Success()
            : PublishPlatformLegalDocumentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
