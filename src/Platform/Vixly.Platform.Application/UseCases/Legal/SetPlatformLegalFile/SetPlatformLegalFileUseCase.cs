using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.SetPlatformLegalFile;

// =========================================================
// FILE: SetPlatformLegalFileUseCase.cs
//
// PURPOSE:
// Uploads or replaces the PDF of a draft in one language, with its summary.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetPlatformLegalFileUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public SetPlatformLegalFileUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<SetPlatformLegalFileResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformLegalFileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return SetPlatformLegalFileResponse.Failure(refused);
        }

        PlatformLegalDocument? document = await _legal.Store.GetDocumentAsync(request.DocumentId, cancellationToken);

        if (document is null)
        {
            return SetPlatformLegalFileResponse.Failure(PlatformLegalErrors.DocumentNotFound);
        }

        if (!document.IsDraft)
        {
            return SetPlatformLegalFileResponse.Failure(PlatformLegalErrors.NotDraft);
        }

        PlatformLegalDocumentFile? file = Vixly.Shared.Kernel.Localization.LanguageCode.TryCreate(request.Language, out Vixly.Shared.Kernel.Localization.LanguageCode? language)
            ? await _legal.Store.GetFileAsync(document.Id, language!.Value, cancellationToken)
            : null;

        try
        {
            if (file is null)
            {
                file = PlatformLegalDocumentFile.Create(document.Id, request.Language, request.Content, request.Summary, _legal.UtcNow);
                _legal.Store.AddFile(file);
            }
            else
            {
                file.Replace(request.Content, request.Summary, _legal.UtcNow);
            }
        }
        catch (DomainException exception)
        {
            return SetPlatformLegalFileResponse.Failure(exception.Error);
        }

        _legal.Audit(context.PlatformUserId, "platform.legal.file_set", PlatformPermissions.LegalManage, nameof(PlatformLegalDocument), document.Id, new { language = file.Language, sha256 = file.Sha256, size = file.SizeBytes });

        return await _legal.SaveAsync(cancellationToken)
            ? SetPlatformLegalFileResponse.Success()
            : SetPlatformLegalFileResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
