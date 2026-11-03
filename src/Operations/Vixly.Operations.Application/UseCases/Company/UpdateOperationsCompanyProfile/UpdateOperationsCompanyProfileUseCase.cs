using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.UpdateOperationsCompanyProfile;

// =========================================================
// FILE: UpdateOperationsCompanyProfileUseCase.cs
//
// PURPOSE:
// The owner saves the company profile directly: trading name, email, phone and address (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Require the same right as a change request (the owner, with a checked second factor)
// - Send it to Platform, which checks again and keeps the legal name
// - Record who did it on the company's side
//
// IMPORTANT NOTES:
// - No approval by Vixly; only the legal name still needs a request with proof
// - Run the rule and return a response; never throw for an expected refusal
// =========================================================
public sealed class UpdateOperationsCompanyProfileUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public UpdateOperationsCompanyProfileUseCase(
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

    public async Task<UpdateOperationsCompanyProfileResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UpdateOperationsCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyRequestChange, true, cancellationToken);

        if (refused is not null)
        {
            return UpdateOperationsCompanyProfileResponse.Failure(refused);
        }

        OperationsPlatformCall<bool> call = await _company.UpdateProfileAsync(_actors.For(context, access!.User, stepUp: true), request.Input, cancellationToken);

        if (!call.Succeeded)
        {
            return UpdateOperationsCompanyProfileResponse.Failure(call.Error!);
        }

        _audit.Record(access.User.Id, "ops.company.profile_changed", "OperationsCompany", null,
            after: new { tradingName = request.Input.TradingName, email = request.Input.Email, city = request.Input.City, country = request.Input.CountryCode });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UpdateOperationsCompanyProfileResponse.Success();
    }
}
