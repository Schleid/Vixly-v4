using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalTerms;

// =========================================================
// FILE: GetPublicLegalTermsUseCase.cs
//
// PURPOSE:
// Reads the current Terms in a language for the registration page (D-Lg9).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: published documents are public
// =========================================================
public sealed class GetPublicLegalTermsUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPublicLegalTermsUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPublicLegalTermsResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetPublicLegalTermsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformLegalTerms? terms = await _legal.TermsAsync(request.Language, cancellationToken);
        return terms is null ? GetPublicLegalTermsResponse.Failure(PlatformLegalErrors.NothingToAccept) : GetPublicLegalTermsResponse.Success(terms);
    }
}
