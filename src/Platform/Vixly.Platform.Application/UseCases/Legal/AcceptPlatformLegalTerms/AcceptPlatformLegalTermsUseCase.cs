using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.AcceptPlatformLegalTerms;

// =========================================================
// FILE: AcceptPlatformLegalTermsUseCase.cs
//
// PURPOSE:
// Records the owner's acceptance of the current Terms, sent by Operations (D-Lg5, D-Lg11).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Owner only; the role, browser IP and user agent come from the service token (D-K8)
// =========================================================
public sealed class AcceptPlatformLegalTermsUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public AcceptPlatformLegalTermsUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<AcceptPlatformLegalTermsResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        AcceptPlatformLegalTermsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenant? tenant = await _legal.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);
        PlatformLegalDocument? terms = await _legal.Store.GetPublishedAsync(PlatformLegalDocumentKind.Terms, cancellationToken);

        if (tenant is null || !PlatformLegalGate.Asked(tenant) || terms is null)
        {
            return AcceptPlatformLegalTermsResponse.Failure(PlatformLegalErrors.NothingToAccept);
        }

        // The owner must have read the version that is current now, in the language and file shown.
        PlatformLegalDocumentFile? file = await _legal.Store.GetFileAsync(terms.Id, request.Language?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);

        if (terms.Id != request.DocumentId || file is null || !string.Equals(file.Sha256, request.Sha256?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return AcceptPlatformLegalTermsResponse.Failure(PlatformLegalErrors.VersionChanged);
        }

        if (await _legal.Store.HasAcceptedAsync(tenant.Id, terms.Id, cancellationToken))
        {
            return AcceptPlatformLegalTermsResponse.Success();
        }

        PlatformTenantTermsAcceptance acceptance;

        try
        {
            acceptance = PlatformTenantTermsAcceptance.ByOwner(tenant.Id, terms, file, context.ActorReference, context.ActorName, PlatformTenantAccess.RoleName(context.Role), context.ClientIp, context.UserAgent, _legal.UtcNow);
        }
        catch (DomainException exception)
        {
            return AcceptPlatformLegalTermsResponse.Failure(exception.Error);
        }

        _legal.Store.AddAcceptance(acceptance);
        _legal.Accepted(acceptance);
        _legal.Audit(null, "platform.legal.terms_accepted", null, nameof(PlatformTenant), tenant.Id, new { version = terms.Version, language = acceptance.Language, actor = acceptance.ActorReference });

        // A double click loses on the unique index; the first one counted.
        return await _legal.SaveAsync(cancellationToken) || await _legal.Store.HasAcceptedAsync(tenant.Id, terms.Id, cancellationToken)
            ? AcceptPlatformLegalTermsResponse.Success()
            : AcceptPlatformLegalTermsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
