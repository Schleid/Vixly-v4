using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.RecordPlatformTermsAcceptance;

// =========================================================
// FILE: RecordPlatformTermsAcceptanceUseCase.cs
//
// PURPOSE:
// Records a tenant's acceptance of the current Terms from a signed paper (D-Lg5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up; the signed paper is kept with the acceptance
// =========================================================
public sealed class RecordPlatformTermsAcceptanceUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public RecordPlatformTermsAcceptanceUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<RecordPlatformTermsAcceptanceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RecordPlatformTermsAcceptanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalRecordAcceptance, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RecordPlatformTermsAcceptanceResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _legal.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);
        PlatformLegalDocument? terms = await _legal.Store.GetPublishedAsync(PlatformLegalDocumentKind.Terms, cancellationToken);
        string language = Vixly.Shared.Kernel.Localization.LanguageCode.TryCreate(request.Language, out Vixly.Shared.Kernel.Localization.LanguageCode? code) ? code!.Value : tenant?.DefaultLanguage.Value ?? "en";

        if (tenant is null || !PlatformLegalGate.Asked(tenant) || terms is null || await _legal.Store.GetFileAsync(terms.Id, language, cancellationToken) is not { } file)
        {
            return RecordPlatformTermsAcceptanceResponse.Failure(PlatformLegalErrors.TenantNotAsked);
        }

        if (await _legal.Store.HasAcceptedAsync(tenant.Id, terms.Id, cancellationToken))
        {
            return RecordPlatformTermsAcceptanceResponse.Failure(PlatformLegalErrors.AlreadyAccepted);
        }

        PlatformTenantTermsAcceptance acceptance;

        try
        {
            acceptance = PlatformTenantTermsAcceptance.RecordedByStaff(tenant.Id, terms, file, request.OwnerName, request.Reason, request.Evidence, request.EvidenceContentType, context.PlatformUserId, _legal.UtcNow);
        }
        catch (DomainException exception)
        {
            return RecordPlatformTermsAcceptanceResponse.Failure(exception.Error);
        }

        _legal.Store.AddAcceptance(acceptance);
        _legal.Accepted(acceptance);
        _legal.Audit(context.PlatformUserId, "platform.legal.acceptance_recorded", PlatformPermissions.LegalRecordAcceptance, nameof(PlatformTenant), tenant.Id,
            new { version = terms.Version, language = acceptance.Language }, request.Reason);

        return await _legal.SaveAsync(cancellationToken)
            ? RecordPlatformTermsAcceptanceResponse.Success()
            : RecordPlatformTermsAcceptanceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
