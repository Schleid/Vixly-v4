using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument;

// =========================================================
// FILE: GetOperationsBillingDocumentUseCase.cs
//
// PURPOSE:
// Fetches one billing PDF for the Owner or an Administrator (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsBillingDocumentUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    public GetOperationsBillingDocumentUseCase(
        OperationsAccess access,
        OperationsActors actors,
        IOperationsCompanyClient company)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _company = company ?? throw new ArgumentNullException(nameof(company));
    }

    public async Task<GetOperationsBillingDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBillingDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsAccessView? access = await _access.LoadAsync(context, cancellationToken);

        if (access is null)
        {
            return GetOperationsBillingDocumentResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired);
        }

        // The billing page belongs to the Owner and Administrators (design section 3).
        if (!access.IsOwner && !access.IsAdministrator)
        {
            return GetOperationsBillingDocumentResponse.Failure(OperationsCompanyErrors.OwnerOrAdministrator);
        }

        OperationsPlatformActor actor = _actors.For(context, access.User);
        OperationsLegalFile? file = await _company.GetDocumentAsync(actor, request.Kind, request.DocumentId, cancellationToken);

        return file is null
            ? GetOperationsBillingDocumentResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.NotFound)
            : GetOperationsBillingDocumentResponse.Success(file);
    }
}
