using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Printing;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.FinishOperationsPrintJob;

// =========================================================
// FILE: FinishOperationsPrintJobUseCase.cs
//
// PURPOSE:
// Records what the dock print program did with a label (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A report for an unknown or already finished job changes nothing: the print program may report twice
// =========================================================
public sealed class FinishOperationsPrintJobUseCase
{
    private readonly OperationsPrintAgentKeys _keys;

    private readonly IOperationsPrintingRepository _printing;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public FinishOperationsPrintJobUseCase(
        OperationsPrintAgentKeys keys,
        IOperationsPrintingRepository printing,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<FinishOperationsPrintJobResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FinishOperationsPrintJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _keys.AuthenticateAsync(request.AgentKey, cancellationToken) is not { } agent)
        {
            return FinishOperationsPrintJobResponse.Failure(OperationsPrintingErrors.AgentNotFound);
        }

        if (await _printing.GetJobAsync(request.JobId, cancellationToken) is not { Status: OperationsPrintJobStatus.Taken } job || job.WarehouseId != agent.WarehouseId)
        {
            return FinishOperationsPrintJobResponse.Success(request.JobId);
        }

        string? error = request.ErrorCode is null ? null : new string(request.ErrorCode.Where(character => char.IsAsciiLetterOrDigit(character) || character == '_').Take(60).ToArray());
        job.Finish(request.Printed, error, _timeProvider.GetUtcNow());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? FinishOperationsPrintJobResponse.Success(job.Id)
            : FinishOperationsPrintJobResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
