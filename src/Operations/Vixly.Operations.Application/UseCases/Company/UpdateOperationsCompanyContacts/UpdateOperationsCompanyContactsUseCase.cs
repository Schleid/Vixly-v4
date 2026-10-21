using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyContacts;

// =========================================================
// FILE: UpdateOperationsCompanyContactsUseCase.cs
//
// PURPOSE:
// Saves the company's billing and technical contacts, language and time zone at Vixly (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdateOperationsCompanyContactsUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public UpdateOperationsCompanyContactsUseCase(
        OperationsAccess access,
        OperationsActors actors,
        IOperationsCompanyClient company,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _company = company ?? throw new ArgumentNullException(nameof(company));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateOperationsCompanyContactsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UpdateOperationsCompanyContactsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyEditContacts, true, cancellationToken);

        if (refused is not null)
        {
            return UpdateOperationsCompanyContactsResponse.Failure(refused);
        }

        if (request.Contacts.Any(contact => contact.Role is not ("billing" or "technical")))
        {
            return UpdateOperationsCompanyContactsResponse.Failure(OperationsCompanyErrors.ContactInvalid);
        }

        OperationsPlatformCall<bool> call = await _company.UpdateAsync(_actors.For(context, access!.User, stepUp: true), request.Contacts, request.Language, request.TimeZone.Trim(), cancellationToken);

        if (!call.Succeeded)
        {
            return UpdateOperationsCompanyContactsResponse.Failure(call.Error!);
        }

        // Platform keeps its own audit; this one records who did it from the tenant side.
        _audit.Record(access.User.Id, "ops.company.contacts_changed", "OperationsCompany", null, after: new { roles = request.Contacts.Select(contact => contact.Role).ToArray(), language = request.Language, timeZone = request.TimeZone });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UpdateOperationsCompanyContactsResponse.Success();
    }
}
