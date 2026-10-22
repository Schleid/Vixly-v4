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

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsPolicyDocument;

// =========================================================
// FILE: GetOperationsPolicyDocumentUseCase.cs
//
// PURPOSE:
// A document filed with an insurance policy, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.view; served as a sandboxed download
// =========================================================
public sealed class GetOperationsPolicyDocumentUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsFileStore _files;

    public GetOperationsPolicyDocumentUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsFileStore files)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsPolicyDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPolicyDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsPolicyDocumentResponse.Failure(refused);
        }

        if (await _insurance.GetPolicyAsync(request.PolicyId, cancellationToken) is not { } policy
            || policy.Documents.FirstOrDefault(item => item.Id == request.DocumentId) is not { } document
            || await _files.ReadAsync(context.RequiredTenantId, document.Key, cancellationToken) is not { } content)
        {
            return GetOperationsPolicyDocumentResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        string extension = document.ContentType switch { "application/pdf" => ".pdf", "image/png" => ".png", _ => ".jpg" };
        return GetOperationsPolicyDocumentResponse.Success(content, document.ContentType, Path.GetFileNameWithoutExtension(document.FileName) + extension);
    }
}
