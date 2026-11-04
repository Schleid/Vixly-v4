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

namespace Vixly.Operations.Application.UseCases.Insurance.AttachOperationsPolicyDocument;

// =========================================================
// FILE: AttachOperationsPolicyDocumentUseCase.cs
//
// PURPOSE:
// Files a document with an insurance policy: the policy itself, an endorsement, a certificate (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// - The file is removed again when the policy is not saved
// =========================================================
public sealed class AttachOperationsPolicyDocumentUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsFileStore _files;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AttachOperationsPolicyDocumentUseCase(
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

    public async Task<AttachOperationsPolicyDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AttachOperationsPolicyDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return AttachOperationsPolicyDocumentResponse.Failure(refused);
        }

        if (await _insurance.GetPolicyAsync(request.PolicyId, cancellationToken) is not { } policy)
        {
            return AttachOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        // PDF, PNG or JPEG of up to 10 MB, checked by its first bytes, never its name (as the documents answering holds).
        if (request.Content.Length is 0 or > Vixly.Operations.Domain.Holds.OperationsHoldLimits.MaxDocumentBytes
            || Vixly.Operations.Application.Holds.OperationsDocumentInspector.TypeOf(request.Content) is not { } type)
        {
            return AttachOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.DocumentInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsPolicyDocument document;

        try
        {
            if (policy.Documents.Count >= OperationsInsurancePolicy.MaxDocuments)
            {
                return AttachOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.TooManyDocuments);
            }

            if (!OperationsPolicyDocument.Kinds.Contains(request.Kind, StringComparer.Ordinal))
            {
                return AttachOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.DocumentInvalid);
            }

            string key = await _files.SaveAsync(context.RequiredTenantId, "insurance", request.Content, type, cancellationToken);
            document = policy.AttachDocument(request.Kind, request.FileName, key, type, Vixly.Operations.Application.Holds.OperationsDocumentInspector.Sha256(request.Content), utcNow);
        }
        catch (DomainException exception)
        {
            return AttachOperationsPolicyDocumentResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.insurance.document_added", nameof(OperationsInsurancePolicy), policy.Id, after: new { kind = document.Kind, name = document.FileName });
        bool saved = false;

        try
        {
            saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!saved)
            {
                await _files.DeleteAsync(context.RequiredTenantId, document.Key, CancellationToken.None);
            }
        }

        return saved ? AttachOperationsPolicyDocumentResponse.Success() : AttachOperationsPolicyDocumentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
