using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange;

// =========================================================
// FILE: SubmitOperationsCompanyChangeUseCase.cs
//
// PURPOSE:
// Asks Vixly to change the company's legal details, with proof (B12, Owner only).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SubmitOperationsCompanyChangeUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SubmitOperationsCompanyChangeUseCase(
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

    public async Task<SubmitOperationsCompanyChangeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SubmitOperationsCompanyChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyRequestChange, true, cancellationToken);

        if (refused is not null)
        {
            return SubmitOperationsCompanyChangeResponse.Failure(refused);
        }

        if (string.IsNullOrWhiteSpace(request.Input.Reason))
        {
            return SubmitOperationsCompanyChangeResponse.Failure(OperationsCompanyErrors.ReasonRequired);
        }

        OperationsPlatformCall<bool> call = await _company.SubmitChangeAsync(_actors.For(context, access!.User, stepUp: true), request.Input, cancellationToken);

        if (!call.Succeeded)
        {
            return SubmitOperationsCompanyChangeResponse.Failure(call.Error!);
        }

        _audit.Record(access.User.Id, "ops.company.change_requested", "OperationsCompany", null, after: new { legalName = request.Input.LegalName, country = request.Input.CountryCode }, reason: request.Input.Reason.Trim());
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SubmitOperationsCompanyChangeResponse.Success();
    }
}
