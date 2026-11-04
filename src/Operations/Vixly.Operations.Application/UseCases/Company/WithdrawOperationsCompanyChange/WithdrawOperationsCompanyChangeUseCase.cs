using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.WithdrawOperationsCompanyChange;

// =========================================================
// FILE: WithdrawOperationsCompanyChangeUseCase.cs
//
// PURPOSE:
// Withdraws the pending request to change the legal details (B12, Owner only).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class WithdrawOperationsCompanyChangeUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public WithdrawOperationsCompanyChangeUseCase(
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

    public async Task<WithdrawOperationsCompanyChangeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        WithdrawOperationsCompanyChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyRequestChange, false, cancellationToken);

        if (refused is not null)
        {
            return WithdrawOperationsCompanyChangeResponse.Failure(refused);
        }

        OperationsPlatformCall<bool> call = await _company.WithdrawChangeAsync(_actors.For(context, access!.User), cancellationToken);

        if (!call.Succeeded)
        {
            return WithdrawOperationsCompanyChangeResponse.Failure(call.Error!);
        }

        _audit.Record(access.User.Id, "ops.company.change_withdrawn", "OperationsCompany", null);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WithdrawOperationsCompanyChangeResponse.Success();
    }
}
