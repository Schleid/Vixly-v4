using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Legal;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Legal.AcceptOperationsLegalTerms;

// =========================================================
// FILE: AcceptOperationsLegalTermsUseCase.cs
//
// PURPOSE:
// Records the Owner's acceptance of the Terms version shown (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only (Platform D-Lg5); Platform checks the version is current and the file hash matches what was shown
// =========================================================
public sealed class AcceptOperationsLegalTermsUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsActors _actors;

    private readonly IOperationsLegalClient _legal;

    private readonly IOperationsTenantDirectory _directory;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public AcceptOperationsLegalTermsUseCase(
        IOperationsUserRepository users,
        OperationsActors actors,
        IOperationsLegalClient legal,
        IOperationsTenantDirectory directory,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AcceptOperationsLegalTermsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AcceptOperationsLegalTermsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return AcceptOperationsLegalTermsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!user.IsOwner)
        {
            return AcceptOperationsLegalTermsResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        OperationsPlatformCall<bool> call = await _legal.AcceptAsync(_actors.For(context, user), request.DocumentId, request.Sha256, request.Language, cancellationToken);

        if (!call.Succeeded)
        {
            return AcceptOperationsLegalTermsResponse.Failure(call.Error!);
        }

        // Read-only for overdue Terms ends at once, not in 60 seconds.
        _directory.Forget(context.RequiredTenantId);
        _audit.Record(user.Id, "ops.legal.terms_accepted", "LegalDocument", request.DocumentId, arguments: new { language = request.Language });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AcceptOperationsLegalTermsResponse.Success();
    }
}
