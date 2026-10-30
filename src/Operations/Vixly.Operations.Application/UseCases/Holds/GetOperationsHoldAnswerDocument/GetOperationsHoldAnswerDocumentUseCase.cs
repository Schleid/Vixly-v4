using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument;

// =========================================================
// FILE: GetOperationsHoldAnswerDocumentUseCase.cs
//
// PURPOSE:
// The document a customer sent with an answer, for staff to read (Slice 3E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.holds.manage for the parcel's warehouse; served as a download, never shown in the page
// =========================================================
public sealed class GetOperationsHoldAnswerDocumentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsHoldRepository _holds;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsFileStore _files;

    public GetOperationsHoldAnswerDocumentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsHoldRepository holds,
        IOperationsTrackingRepository tracking,
        IOperationsFileStore files)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsHoldAnswerDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsHoldAnswerDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsHoldAnswerDocumentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _holds.GetAnswerAsync(request.AnswerId, cancellationToken) is not { DocumentKey: { } key } answer
            || await _tracking.GetParcelAsync(answer.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId }
            || await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, warehouseId, cancellationToken) is null
            || await _files.ReadAsync(context.RequiredTenantId, key, cancellationToken) is not { } content)
        {
            return GetOperationsHoldAnswerDocumentResponse.Failure(OperationsHoldErrors.AnswerNotFound);
        }

        return GetOperationsHoldAnswerDocumentResponse.Success(content, answer.DocumentType ?? "application/octet-stream", answer.DocumentName ?? "document");
    }
}
