using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsCompany;

// =========================================================
// FILE: GetOperationsCompanyUseCase.cs
//
// PURPOSE:
// Reads the company's account at Vixly for the company page (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsCompanyUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    public GetOperationsCompanyUseCase(
        OperationsAccess access,
        OperationsActors actors,
        IOperationsCompanyClient company)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _company = company ?? throw new ArgumentNullException(nameof(company));
    }

    public async Task<GetOperationsCompanyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCompanyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCompanyResponse.Failure(refused);
        }

        OperationsPlatformActor actor = _actors.For(context, access!.User);
        OperationsPlatformCall<OperationsCompany> company = await _company.GetAsync(actor, cancellationToken);

        if (!company.Succeeded)
        {
            return GetOperationsCompanyResponse.Failure(company.Error!);
        }

        // Platform shows change requests to the Owner and Administrators only.
        OperationsCompanyChange? change = access.IsOwner || access.IsAdministrator ? await _company.GetChangeAsync(actor, cancellationToken) : null;

        return GetOperationsCompanyResponse.Success(new OperationsCompanyView(company.Value!, change,
            access.HasTenantWide(OperationsPermissions.CompanyEditContacts), access.HasTenantWide(OperationsPermissions.CompanyRequestChange)));
    }
}
