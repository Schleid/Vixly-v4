using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.RemoveOperationsPolicyDocument;

// =========================================================
// FILE: RemoveOperationsPolicyDocumentUseCase.cs
//
// PURPOSE:
// Removes a document filed with an insurance policy (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// =========================================================
public sealed class RemoveOperationsPolicyDocumentUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsFileStore _files;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsPolicyDocumentUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsFileStore files,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsPolicyDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsPolicyDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsPolicyDocumentResponse.Failure(refused);
        }

        if (await _insurance.GetPolicyAsync(request.PolicyId, cancellationToken) is not { } policy)
        {
            return RemoveOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        // Already removed (a double click): nothing to do.
        if (policy.Documents.All(item => item.Id != request.DocumentId))
        {
            return RemoveOperationsPolicyDocumentResponse.Success();
        }

        OperationsPolicyDocument document = policy.RemoveDocument(request.DocumentId, _timeProvider.GetUtcNow());
        _audit.Record(access!.User.Id, "ops.insurance.document_removed", nameof(OperationsInsurancePolicy), policy.Id, before: new { kind = document.Kind, name = document.FileName });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemoveOperationsPolicyDocumentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // The file goes once the policy no longer points to it.
        await _files.DeleteAsync(context.RequiredTenantId, document.Key, CancellationToken.None);
        return RemoveOperationsPolicyDocumentResponse.Success();
    }
}
